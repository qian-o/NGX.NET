// One lane owns one primary path. Diffuse and specular paths run independently;
// the 2x2 sample strata are visited across frames for temporal reconstruction.
static const uint primarySampleCount = 1; // RenderLayout.PrimarySamples
#if HAS_RAY_QUERY
Surface hitSurface(Hit hit, float3 direction)
{
    Vertex a = vertices[hit.triangle * 3], b = vertices[hit.triangle * 3 + 1], c = vertices[hit.triangle * 3 + 2];
    float3 weights = float3(1 - hit.bary.x - hit.bary.y, hit.bary);
    float2 uv = a.uv.xy * weights.x + b.uv.xy * weights.y + c.uv.xy * weights.z;
    float3 n = normalize(a.normal.xyz * weights.x + b.normal.xyz * weights.y + c.normal.xyz * weights.z);
    float4 tangent = a.tangent * weights.x + b.tangent * weights.y + c.tangent * weights.z;
    float footprint = hit.distance * 2 * g.camera.w / g.size.y;
    float uvScale = max(length(b.uv.xy - a.uv.xy) / max(length(b.position.xyz - a.position.xyz), g.scene.z),
                        length(c.uv.xy - a.uv.xy) / max(length(c.position.xyz - a.position.xyz), g.scene.z));
    Material material = materials[(uint)a.normal.w];
    float4 lod;
    for (uint i = 0; i < 4; i++)
    {
        int index = (int)material.textures[i];
        lod[i] = index < 0 ? 0 : max(log2(max(footprint * uvScale * max(textureInfo[index].y, textureInfo[index].z), 1)) + g.jitter.w, 0);
    }
    Surface s = surface(material, uv, n, tangent, lod);
    if (dot(s.normal, direction) > 0) s.normal = -s.normal;
    float3 geometricNormal = normalize(cross(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz));
    s.geometricNormal = dot(geometricNormal, direction) > 0 ? -geometricNormal : geometricNormal;
    return s;
}
float3 secondaryRadiance(float3 origin, float3 direction, float escapeWeight, float3 throughput, bool diffusePath, inout uint rng, out float distance)
{
    const uint surfaceBounces = 8;
    float3 radiance = 0;
    distance = skyHitDistance;
    // Resolve emission/environment after the last scattering event as well.
    // Stopping before tracing that ray would discard valid terminal skylight.
    [loop]
    for (uint bounce = 0; bounce <= surfaceBounces; bounce++)
    {
        Hit hit = trace(origin, direction, g.scene.w * 4);
        if (bounce == 0 && hit.triangle != 0xffffffff) distance = hit.distance;
        if (hit.triangle == 0xffffffff)
        {
            radiance += throughput * sky(direction) * escapeWeight;
            break;
        }
        Surface s = hitSurface(hit, direction);
        radiance += throughput * s.emissive;
        if (bounce == surfaceBounces) break;
        float3 p = origin + direction * hit.distance;
        float3 view = -direction;
        radiance += throughput * (directLight(s, p, view, sampleSun(rng))
            + skyLighting(s, p, view, float2(random(rng), random(rng)), false));
        float probability = specularProbability(s);
        float3 halfVector;
        float2 u = float2(random(rng), random(rng));
        if (random(rng) < probability)
        {
            direction = sampleSpecular(s, view, u, halfVector);
        }
        else
        {
            float r = sqrt(u.x), phi = u.y * 6.2831853;
            direction = basisDirection(s.normal, float3(r * cos(phi), r * sin(phi), sqrt(1 - u.x)));
        }
        if (!aboveSurface(s, direction)) break;
        float2 pdfs = bsdfPdfs(s, view, direction);
        float pdf = lerp(pdfs.x, pdfs.y, probability);
        if (pdf <= 0) break;
        throughput *= brdf(s, view, direction) / pdf;
        escapeWeight = powerHeuristic(pdf, skyPortalPdf(p, direction));
        float continuation = max(throughput.r, max(throughput.g, throughput.b));
        if (continuation <= 0) break;
        // Allocate continuation from the complete path weight, including the
        // primary BSDF. Keep an extra event for an unlit diffuse first hit so
        // occluded regions can collect bounce light before roulette begins.
        if (bounce > 0 || !diffusePath || any(radiance > 0))
        {
            // Square-root survival is more conservative than linear throughput:
            // dim paths survive more often, reducing rare, large sample weights.
            float survival = sqrt(saturate(continuation));
            if (random(rng) >= survival) break;
            throughput /= survival;
        }
        origin = rayOrigin(p, s);
    }
    return radiance;
}
#endif
[shader("compute")]
[numthreads(8, 8, 1)]
void TraceLighting(uint3 id : SV_DispatchThreadID)
{
#if HAS_RAY_QUERY
    uint2 pixel = id.xy;
    if (any(pixel >= uint2(g.size.xy)) || id.z >= primarySampleCount * 2) return;
    float depth = depthImage[pixel];
    // Lighting resolves sky and motion directly without reading these samples.
    if (depth <= 0) return;
    float3 p = worldPosition(int2(pixel), depth);
    float3 view = normalize(g.camera.xyz - p);
    Surface s = primarySurface(pixel, view);
    uint pixelSeed = hash(pixel.x + pixel.y * (uint)g.size.x);
    uint seed = hash(pixelSeed + (uint)g.scene.y * 0x9e3779b9);
    uint rng = hash(seed ^ (id.z * 0x9e3779b9));
    uint sample = id.z % primarySampleCount;
    // A stable pixel rotation covers all four strata over four consecutive frames.
    uint stratumIndex = ((uint)g.scene.y * primarySampleCount + sample + (pixelSeed & 3)) & 3;
    float2 stratum = float2(stratumIndex & 1, stratumIndex >> 1);
    float3 f0 = lerp(0.04, s.albedo, s.metallic);
    float3 color = 0;
    if (id.z < primarySampleCount)
    {
        float2 lightU = (stratum + float2(random(rng), random(rng))) / 2;
        color = directLight(s, p, view, sampleSun(rng)) + skyLighting(s, p, view, lightU, true);
        float2 u = (stratum + float2(random(rng), random(rng))) / 2;
        float phi = u.y * 6.2831853;
        float3 direction = basisDirection(s.normal, float3(sqrt(u.x) * cos(phi), sqrt(u.x) * sin(phi), sqrt(1 - u.x)));
        float3 h = normalize(view + direction);
        float3 weight = s.albedo * (1 - s.metallic) * (1 - fresnel(f0, dot(view, h)));
        if (any(weight > 0) && aboveSurface(s, direction))
        {
            float pdf = max(dot(s.normal, direction), 0) / 3.14159265;
            float unused;
            color += secondaryRadiance(rayOrigin(p, s), direction,
                powerHeuristic(pdf, skyPortalPdf(p, direction)), weight, true, rng, unused);
        }
    }
    else
    {
        float2 u = (stratum + float2(random(rng), random(rng))) / 2;
        float3 halfVector;
        float3 direction = sampleSpecular(s, view, u, halfVector);
        float distance = skyHitDistance;
        float nl = dot(s.normal, direction);
        if (aboveSurface(s, direction))
        {
            float nv = max(dot(s.normal, view), 1e-4), vh = saturate(dot(view, halfVector));
            float3 weight = specularWeight(f0, nv, nl, vh, s.roughness * s.roughness);
            float pdf = bsdfPdfs(s, view, direction).y;
            color = secondaryRadiance(rayOrigin(p, s), direction,
                powerHeuristic(pdf, skyPortalPdf(p, direction)), weight, false, rng, distance);
        }
        // Preserve one real first-specular-sample distance, including the sky
        // sentinel. Other paths neither average nor race to write this RR guide.
        if (sample == 0 && g.jitter.z > 0) hitOutput[pixel] = distance;
    }
    lightingSamplesOutput[id] = float4(color, 0);
#endif
}
