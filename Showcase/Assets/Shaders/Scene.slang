#ifndef HAS_RAY_QUERY
#define HAS_RAY_QUERY 1
#endif

struct Vertex { float4 position; float4 normal; float4 tangent; float4 uv; };
struct Material { float4 baseColor; float4 emissiveMetallic; float4 parameters; float4 textures; };
struct ObjectData { float4 offset; float4 previousOffset; uint4 geometry; };
struct Globals
{
    row_major float4x4 viewProjection;
    row_major float4x4 currentViewProjection;
    row_major float4x4 previousViewProjection;
    row_major float4x4 inverseViewProjection;
    float4 camera;
    float4 size; // render width, height, output width, height
    float4 sun;
    float4 scene; // object count, frame number, ray epsilon, scale
    float4 parameters; // ray tracing, exposure, input image, FXAA enabled
    float4 jitter; // pixel jitter.xy; generate RR guides when z is set
    float4 center;
    row_major float4x4 sunViewProjection;
};
[[vk::binding(0, 0)]] ConstantBuffer<Globals> g : register(b0);
[[vk::binding(1, 0)]] StructuredBuffer<Vertex> vertices : register(t0);
[[vk::binding(2, 0)]] StructuredBuffer<Material> materials : register(t1);
[[vk::binding(3, 0)]] StructuredBuffer<uint> texels : register(t2);
[[vk::binding(4, 0)]] StructuredBuffer<uint4> textureInfo : register(t3);
[[vk::binding(5, 0)]] StructuredBuffer<ObjectData> objects : register(t4);
#if HAS_RAY_QUERY
[[vk::binding(6, 0)]] RaytracingAccelerationStructure sceneAcceleration : register(t5);
#endif
[[vk::binding(7, 0)]] Texture2D<float4> albedoImage : register(t6);
[[vk::binding(8, 0)]] Texture2D<float4> normalImage : register(t7);
[[vk::binding(9, 0)]] Texture2D<float4> emissiveImage : register(t8);
[[vk::binding(10, 0)]] Texture2D<float2> motionImage : register(t9);
[[vk::binding(11, 0)]] Texture2D<float> depthImage : register(t10);
[[vk::binding(12, 0)]] Texture2D<float4> sceneImage : register(t11);
[[vk::binding(13, 0)]] Texture2D<float4> specularImage : register(t12);
[[vk::binding(14, 0)]] Texture2D<float> hitImage : register(t13);
[[vk::binding(15, 0)]] Texture2D<float4> reconstructedImage : register(t14);
[[vk::binding(16, 0)]] Texture2D<float4> displayInputImage : register(t15);
[[vk::binding(17, 0)]] Texture2D<float4> hudlessImage : register(t16);
[[vk::binding(18, 0)]] Texture2D<float4> uiImage : register(t17);
[[vk::binding(19, 0)]] Texture2D<float4> finalImage : register(t18);
[[vk::binding(20, 0)]] Texture2D<float4> diffuseImage : register(t19);
[[vk::binding(22, 0)]] Texture2D<float> shadowImage : register(t21);
[[vk::binding(23, 0)]] Texture2D<float4> fontImage : register(t22);
[[vk::binding(32, 0)]] RWTexture2D<float4> sceneOutput : register(u0);
[[vk::binding(33, 0)]] RWTexture2D<float4> specularOutput : register(u1);
[[vk::binding(34, 0)]] RWTexture2D<float> hitOutput : register(u2);
[[vk::binding(35, 0)]] RWTexture2D<float4> reconstructedOutput : register(u3);
[[vk::binding(36, 0)]] RWTexture2D<float4> displayInputOutput : register(u4);
[[vk::binding(37, 0)]] RWTexture2D<float4> hudlessOutput : register(u5);
[[vk::binding(38, 0)]] RWTexture2D<float4> finalOutput : register(u6);
[[vk::binding(39, 0)]] RWTexture2D<float2> motionOutput : register(u7);
[[vk::binding(40, 0)]] RWTexture2D<float4> diffuseOutput : register(u8);
[[vk::binding(48, 0)]] SamplerState linearSampler : register(s0);

float3 srgbToLinear(float3 c) { return select(c <= 0.04045, c / 12.92, pow((c + 0.055) / 1.055, 2.4)); }
float3 linearToSrgb(float3 c) { return select(c <= 0.0031308, c * 12.92, 1.055 * pow(max(c, 0), 1.0 / 2.4) - 0.055); }
float4 unpackColor(uint p) { return float4(p & 255, (p >> 8) & 255, (p >> 16) & 255, p >> 24) / 255.0; }
float4 fetch(uint offset, int2 p, uint2 size)
{
    p = (p % int2(size) + int2(size)) % int2(size);
    return unpackColor(texels[offset + p.y * size.x + p.x]);
}
float4 sampleTexture(int index, float2 uv, float lod, bool srgb)
{
    if (index < 0) return 1;
    uint4 info = textureInfo[index];
    uint level = min((uint)max(lod, 0), info.w - 1);
    uint2 size = info.yz;
    uint offset = info.x;
    for (uint i = 0; i < level; i++) { offset += size.x * size.y; size = max(size / 2, 1); }
    float2 p = frac(uv) * size - 0.5;
    int2 pixel = int2(floor(p));
    float2 f = frac(p);
    float4 a = fetch(offset, pixel, size), b = fetch(offset, pixel + int2(1, 0), size);
    float4 c = fetch(offset, pixel + int2(0, 1), size), d = fetch(offset, pixel + 1, size);
    if (srgb) { a.rgb = srgbToLinear(a.rgb); b.rgb = srgbToLinear(b.rgb); c.rgb = srgbToLinear(c.rgb); d.rgb = srgbToLinear(d.rgb); }
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
float textureLod(int index, float2 dx, float2 dy)
{
    if (index < 0) return 0;
    float2 size = textureInfo[index].yz;
    return max(0.5 * log2(max(dot(dx * size, dx * size), dot(dy * size, dy * size))), 0);
}
struct Surface { float3 albedo; float3 normal; float3 emissive; float metallic; float roughness; float alpha; };
Surface surface(Material m, float2 uv, float3 n, float4 tangent, float lod)
{
    Surface s;
    float4 color = m.baseColor * sampleTexture((int)m.textures.x, uv, lod, true);
    float4 mr = sampleTexture((int)m.textures.z, uv, lod, false);
    s.albedo = color.rgb;
    s.alpha = color.a;
    s.metallic = saturate(m.emissiveMetallic.w * mr.b);
    s.roughness = clamp(m.parameters.x * mr.g, 0.045, 1);
    s.emissive = m.emissiveMetallic.rgb * sampleTexture((int)m.textures.w, uv, lod, true).rgb;
    s.normal = normalize(n);
    if (m.textures.y >= 0)
    {
        float3 t = normalize(tangent.xyz - n * dot(n, tangent.xyz));
        float3 b = cross(n, t) * tangent.w;
        float3 mapped = sampleTexture((int)m.textures.y, uv, lod, false).xyz * 2 - 1;
        mapped.xy *= m.parameters.y;
        s.normal = normalize(t * mapped.x + b * mapped.y + n * mapped.z);
    }
    return s;
}
struct RasterVertex
{
    float4 position : SV_Position;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 uv : TEXCOORD0;
    float4 current : TEXCOORD1;
    float4 previous : TEXCOORD2;
    nointerpolation uint material : TEXCOORD3;
    float3 world : TEXCOORD4;
};
[shader("vertex")]
RasterVertex SceneVS(uint id : SV_VertexID)
{
    Vertex v = vertices[id];
    ObjectData object = objects[(uint)v.position.w];
    float4 world = float4(v.position.xyz + object.offset.xyz, 1);
    RasterVertex result;
    result.position = mul(world, g.viewProjection);
    result.world = world.xyz;
    result.current = mul(world, g.currentViewProjection);
    result.previous = mul(float4(v.position.xyz + object.previousOffset.xyz, 1), g.previousViewProjection);
    result.normal = v.normal.xyz;
    result.tangent = v.tangent;
    result.uv = v.uv.xy;
    result.material = (uint)v.normal.w;
    return result;
}
struct GBuffer { float4 albedo : SV_Target0; float4 normal : SV_Target1; float4 emissive : SV_Target2; float2 motion : SV_Target3; };
[shader("fragment")]
GBuffer ScenePS(RasterVertex input, bool front : SV_IsFrontFace)
{
    Material m = materials[input.material];
    if (!front && m.parameters.w == 0) discard;
    float lod = textureLod((int)m.textures.x, ddx(input.uv), ddy(input.uv));
    Surface s = surface(m, input.uv, normalize(input.normal), input.tangent, lod);
    clip(s.alpha - m.parameters.z);
    if (dot(s.normal, g.camera.xyz - input.world) < 0) s.normal = -s.normal;
    GBuffer output;
    output.albedo = float4(s.albedo, 1);
    output.normal = float4(s.normal, s.roughness);
    output.emissive = float4(s.emissive, s.metallic);
    float2 motion = input.previous.xy / input.previous.w - input.current.xy / input.current.w;
    output.motion = motion * float2(0.5, -0.5) * g.size.xy;
    return output;
}

struct ShadowVertex
{
    float4 position : SV_Position;
    float2 uv : TEXCOORD0;
    nointerpolation uint material : TEXCOORD1;
};
[shader("vertex")]
ShadowVertex ShadowVS(uint id : SV_VertexID)
{
    Vertex v = vertices[id];
    ShadowVertex result;
    result.position = mul(float4(v.position.xyz + objects[(uint)v.position.w].offset.xyz, 1), g.sunViewProjection);
    result.uv = v.uv.xy;
    result.material = (uint)v.normal.w;
    return result;
}
[shader("fragment")]
void ShadowPS(ShadowVertex input, bool front : SV_IsFrontFace)
{
    Material m = materials[input.material];
    if (!front && m.parameters.w == 0) discard;
    if (m.parameters.z >= 0)
    {
        float lod = textureLod((int)m.textures.x, ddx(input.uv), ddy(input.uv));
        clip(m.baseColor.a * sampleTexture((int)m.textures.x, input.uv, lod, true).a - m.parameters.z);
    }
}
float shadowVisibility(float3 p, float3 normal)
{
    uint width, height;
    shadowImage.GetDimensions(width, height);
    // One shadow texel in world space provides a resolution-scaled normal offset.
    float4 clip = mul(float4(p + normal * (g.scene.w / width), 1), g.sunViewProjection);
    float3 projected = clip.xyz / clip.w;
    float2 uv = projected.xy * float2(0.5, -0.5) + 0.5;
    if (any(uv < 0) || any(uv > 1) || projected.z < 0 || projected.z > 1) return 1;
    int2 pixel = int2(uv * float2(width, height));
    float visibility = 0;
    for (int y = -1; y <= 1; y++)
        for (int x = -1; x <= 1; x++)
            visibility += projected.z <= shadowImage[clamp(pixel + int2(x, y), int2(0), int2(width, height) - 1)] ? 1 : 0;
    return visibility / 9;
}

#if HAS_RAY_QUERY
struct Hit { float distance; uint triangle; uint object; float2 bary; };
Hit trace(float3 origin, float3 direction, float limit, bool anyHit)
{
    Hit hit;
    hit.distance = limit; hit.triangle = 0xffffffff; hit.object = 0; hit.bary = 0;
    if (limit <= g.scene.z) return hit;
    RayDesc ray;
    ray.Origin = origin;
    ray.Direction = direction;
    ray.TMin = g.scene.z;
    ray.TMax = limit;
    RayQuery<RAY_FLAG_NONE> query;
    query.TraceRayInline(sceneAcceleration, anyHit ? RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH : RAY_FLAG_NONE, 0xff, ray);
    while (query.Proceed())
    {
        if (query.CandidateType() != CANDIDATE_NON_OPAQUE_TRIANGLE) continue;
        uint objectIndex = query.CandidateInstanceID();
        // Each BLAS contains one non-indexed triangle geometry.
        uint firstVertex = objects[objectIndex].geometry.x + query.CandidatePrimitiveIndex() * 3;
        Vertex a = vertices[firstVertex], b = vertices[firstVertex + 1], c = vertices[firstVertex + 2];
        Material material = materials[(uint)a.normal.w];
        // All instances translate rigid geometry. Geometric winding keeps the
        // material's one-/two-sided rule identical on DXR and Vulkan.
        if (material.parameters.w == 0 && dot(direction, cross(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz)) >= 0) continue;
        if (material.parameters.z >= 0)
        {
            float2 bary = query.CandidateTriangleBarycentrics();
            float2 uv = a.uv.xy * (1 - bary.x - bary.y) + b.uv.xy * bary.x + c.uv.xy * bary.y;
            if (material.baseColor.a * sampleTexture((int)material.textures.x, uv, 0, true).a < material.parameters.z) continue;
        }
        query.CommitNonOpaqueTriangleHit();
    }
    if (query.CommittedStatus() == COMMITTED_TRIANGLE_HIT)
    {
        hit.object = query.CommittedInstanceID();
        hit.triangle = objects[hit.object].geometry.x / 3 + query.CommittedPrimitiveIndex();
        hit.distance = query.CommittedRayT();
        hit.bary = query.CommittedTriangleBarycentrics();
    }
    return hit;
}
#endif
float3 sky(float3 direction) { return lerp(float3(0.08, 0.09, 0.12), float3(0.4, 0.6, 1.0), saturate(direction.y * 0.5 + 0.5)); }
uint hash(uint x) { x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; return x ^ (x >> 16); }
float random(inout uint state) { state = hash(state); return float(state >> 8) / 16777216.0; }
float3 basisDirection(float3 n, float3 local)
{
    float3 t = normalize(cross(abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0), n));
    return normalize(t * local.x + cross(n, t) * local.y + n * local.z);
}
float3 fresnel(float3 f0, float cosine) { return f0 + (1 - f0) * pow(1 - saturate(cosine), 5); }
float3 brdf(Surface s, float3 v, float3 l)
{
    float3 h = normalize(v + l);
    float nv = max(dot(s.normal, v), 0.001), nl = max(dot(s.normal, l), 0);
    float nh = max(dot(s.normal, h), 0), vh = max(dot(v, h), 0);
    float a2 = pow(s.roughness, 4);
    float d = a2 / max(3.14159265 * pow(nh * nh * (a2 - 1) + 1, 2), 1e-6);
    float k = pow(s.roughness + 1, 2) / 8;
    float geometry = nv / (nv * (1 - k) + k) * nl / max(nl * (1 - k) + k, 1e-5);
    float3 f = fresnel(lerp(0.04, s.albedo, s.metallic), vh);
    return ((1 - f) * (1 - s.metallic) * s.albedo / 3.14159265 + f * d * geometry / max(4 * nv * nl, 1e-5)) * nl;
}
float3 directLight(Surface s, float3 p, float3 view, float3 sunDirection)
{
    float visibility;
#if HAS_RAY_QUERY
    if (g.parameters.x > 0) visibility = trace(p + s.normal * g.scene.z, sunDirection, g.scene.w * 4, true).triangle == 0xffffffff ? 1 : 0;
    else
#endif
    visibility = shadowVisibility(p, s.normal);
    float3 color = brdf(s, view, sunDirection) * float3(6, 5.6, 4.8) * visibility;
    for (uint i = 0; i < 2; i++)
    {
        float3 lightPosition = g.center.xyz + float3((i == 0 ? -1 : 1) * g.scene.w * 0.15, g.scene.w * 0.045, 0);
        float3 delta = lightPosition - p;
        float distanceSquared = dot(delta, delta);
        if (distanceSquared <= g.scene.z * g.scene.z) continue;
        float3 l = normalize(delta);
        float unoccluded = 1;
#if HAS_RAY_QUERY
        if (g.parameters.x > 0) unoccluded = trace(p + s.normal * g.scene.z, l, sqrt(distanceSquared), true).triangle == 0xffffffff ? 1 : 0;
#endif
        color += brdf(s, view, l) * (i == 0 ? float3(1, 0.35, 0.08) : float3(0.15, 0.4, 1)) * g.scene.w * g.scene.w * 0.002 / max(distanceSquared, g.scene.z) * unoccluded;
    }
    return color;
}
// Deterministically integrate the GGX specular reflectance for the RR guide buffer.
// Sampling uses the same NDF and masking model as the secondary specular rays.
float3 integratedSpecularAlbedo(float3 f0, float roughness, float nv)
{
    const uint sampleCount = 32;
    float3 view = float3(sqrt(max(1 - nv * nv, 0)), 0, nv);
    float3 result = 0;
    float alpha2 = pow(roughness, 4), k = roughness * roughness / 2;
    for (uint i = 0; i < sampleCount; i++)
    {
        float u = (i + 0.5) / sampleCount;
        float phi = (reversebits(i) * 2.3283064365386963e-10) * 6.2831853;
        float cosTheta = sqrt((1 - u) / (1 + (alpha2 - 1) * u));
        float sinTheta = sqrt(max(1 - cosTheta * cosTheta, 0));
        float3 h = float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta);
        float3 l = reflect(-view, h);
        if (l.z > 0)
        {
            float vh = max(dot(view, h), 0);
            float geometry = nv / max(nv * (1 - k) + k, 1e-5) * l.z / (l.z * (1 - k) + k);
            result += fresnel(f0, vh) * geometry * vh / max(nv * h.z, 1e-5);
        }
    }
    return saturate(result / sampleCount);
}
#if HAS_RAY_QUERY
float3 secondaryRadiance(float3 origin, float3 direction, out float distance)
{
    Hit hit = trace(origin, direction, g.scene.w * 4, false);
    distance = hit.distance;
    if (hit.triangle == 0xffffffff) return sky(direction);
    Vertex a = vertices[hit.triangle * 3], b = vertices[hit.triangle * 3 + 1], c = vertices[hit.triangle * 3 + 2];
    float3 weights = float3(1 - hit.bary.x - hit.bary.y, hit.bary);
    float2 uv = a.uv.xy * weights.x + b.uv.xy * weights.y + c.uv.xy * weights.z;
    float3 n = normalize(a.normal.xyz * weights.x + b.normal.xyz * weights.y + c.normal.xyz * weights.z);
    float4 tangent = a.tangent * weights.x + b.tangent * weights.y + c.tangent * weights.z;
    Surface s = surface(materials[(uint)a.normal.w], uv, n, tangent, 1);
    if (dot(s.normal, direction) > 0) s.normal = -s.normal;
    float3 p = origin + direction * distance;
    return s.emissive + directLight(s, p, -direction, g.sun.xyz) + s.albedo * (1 - s.metallic) * sky(s.normal) * 0.08;
}
#endif
[shader("compute")]
[numthreads(8, 8, 1)]
void Lighting(uint3 id : SV_DispatchThreadID)
{
    uint2 pixel = id.xy;
    if (any(pixel >= (uint2)g.size.xy)) return;
    float2 uv = (pixel + 0.5) / g.size.xy;
    float depth = depthImage[pixel];
    float4 world = mul(float4(uv * float2(2, -2) + float2(-1, 1), depth, 1), g.inverseViewProjection);
    float3 p = world.xyz / world.w;
    float3 view = normalize(g.camera.xyz - p);
    if (depth >= 1)
    {
        sceneOutput[pixel] = float4(sky(-view), 1);
        if (g.jitter.z > 0)
        {
            specularOutput[pixel] = float4(0, 0, 0, 1);
            diffuseOutput[pixel] = 0;
            hitOutput[pixel] = g.scene.w * 4;
        }
        float4 current = mul(float4(-view, 0), g.currentViewProjection);
        float4 previous = mul(float4(-view, 0), g.previousViewProjection);
        motionOutput[pixel] = (previous.xy / previous.w - current.xy / current.w) * float2(0.5, -0.5) * g.size.xy;
        return;
    }
    float4 normal = normalImage[pixel], emissive = emissiveImage[pixel];
    Surface s; s.albedo = albedoImage[pixel].rgb; s.normal = normal.xyz; s.roughness = normal.w; s.emissive = emissive.rgb; s.metallic = emissive.w; s.alpha = 1;
    if (dot(s.normal, view) < 0) s.normal = -s.normal;
    uint rng = hash(pixel.x + pixel.y * (uint)g.size.x + (uint)g.scene.y * 0x9e3779b9);
    float3 sunDirection = g.sun.xyz;
    if (g.parameters.x > 0)
    {
        float r = sqrt(random(rng)) * 0.02, phi = random(rng) * 6.2831853;
        sunDirection = basisDirection(sunDirection, float3(r * cos(phi), r * sin(phi), 1));
    }
    float3 color = s.emissive + directLight(s, p, view, sunDirection);
    float3 f0 = lerp(0.04, s.albedo, s.metallic);
    float nv = saturate(dot(s.normal, view));
    float3 specularAlbedo = integratedSpecularAlbedo(f0, s.roughness, nv);
    float specularDistance = g.scene.w * 4;
#if HAS_RAY_QUERY
    if (g.parameters.x > 0)
    {
        float u = random(rng), phi = random(rng) * 6.2831853;
        float3 diffuseDirection = basisDirection(s.normal, float3(sqrt(u) * cos(phi), sqrt(u) * sin(phi), sqrt(1 - u)));
        float unused;
        color += s.albedo * (1 - s.metallic) * (1 - fresnel(f0, nv)) * secondaryRadiance(p + s.normal * g.scene.z, diffuseDirection, unused);
        u = random(rng); phi = random(rng) * 6.2831853;
        float alpha2 = pow(s.roughness, 4);
        float cosTheta = sqrt((1 - u) / (1 + (alpha2 - 1) * u));
        float sinTheta = sqrt(max(1 - cosTheta * cosTheta, 0));
        float3 halfVector = basisDirection(s.normal, float3(sinTheta * cos(phi), sinTheta * sin(phi), cosTheta));
        float3 reflected = reflect(-view, halfVector);
        if (dot(reflected, s.normal) > 0)
        {
            float nl = dot(s.normal, reflected), nh = max(dot(s.normal, halfVector), 1e-5), vh = max(dot(view, halfVector), 0);
            float k = s.roughness * s.roughness / 2;
            float masking = nv / max(nv * (1 - k) + k, 1e-5) * nl / (nl * (1 - k) + k);
            float3 weight = fresnel(f0, vh) * masking * vh / max(nv * nh, 1e-5);
            color += secondaryRadiance(p + s.normal * g.scene.z, reflected, specularDistance) * weight;
        }
    }
    else
#endif
    {
        color += s.albedo * (1 - s.metallic) * sky(s.normal) * 0.25;
        color += sky(reflect(-view, s.normal)) * specularAlbedo;
    }
    sceneOutput[pixel] = float4(max(color, 0), 1);
    if (g.jitter.z > 0)
    {
        specularOutput[pixel] = float4(specularAlbedo, 1);
        diffuseOutput[pixel] = float4(s.albedo * (1 - s.metallic), 1);
        hitOutput[pixel] = specularDistance;
    }
}
float3 aces(float3 x) { return saturate((x * (2.51 * x + 0.03)) / (x * (2.43 * x + 0.59) + 0.14)); }
float3 sampleHdr(float2 uv)
{
    return g.parameters.z > 0 ? reconstructedImage.SampleLevel(linearSampler, uv, 0).rgb : sceneImage.SampleLevel(linearSampler, uv, 0).rgb;
}
float3 sampleDisplay(float2 uv, bool toneMap)
{
    return toneMap ? linearToSrgb(aces(sampleHdr(uv) * exp2(g.parameters.y))) : displayInputImage.SampleLevel(linearSampler, uv, 0).rgb;
}
float luminance(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }
// FXAA edge filter, evaluated at the source resolution before spatial NIS scaling.
float3 antialias(float2 uv, float2 step, bool toneMap)
{
    float3 center = sampleDisplay(uv, toneMap);
    float3 nw = sampleDisplay(uv - step, toneMap);
    float3 ne = sampleDisplay(uv + step * float2(1, -1), toneMap);
    float3 sw = sampleDisplay(uv + step * float2(-1, 1), toneMap);
    float3 se = sampleDisplay(uv + step, toneMap);
    float l = luminance(center), lnw = luminance(nw), lne = luminance(ne), lsw = luminance(sw), lse = luminance(se);
    float lo = min(l, min(min(lnw, lne), min(lsw, lse))), hi = max(l, max(max(lnw, lne), max(lsw, lse)));
    float2 direction = float2(-((lnw + lne) - (lsw + lse)), (lnw + lsw) - (lne + lse));
    float reduce = max((lnw + lne + lsw + lse) * (0.25 / 8), 1.0 / 128);
    direction = clamp(direction / (min(abs(direction.x), abs(direction.y)) + reduce), -8, 8) * step;
    float3 a = 0.5 * (sampleDisplay(uv + direction * (1.0 / 3 - 0.5), toneMap) + sampleDisplay(uv + direction * (2.0 / 3 - 0.5), toneMap));
    float3 b = a * 0.5 + 0.25 * (sampleDisplay(uv - direction * 0.5, toneMap) + sampleDisplay(uv + direction * 0.5, toneMap));
    float lb = luminance(b);
    return hi - lo < max(1.0 / 32, hi / 8) ? center : (lb < lo || lb > hi ? a : b);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void ToneMap(uint3 id : SV_DispatchThreadID)
{
    uint width, height; displayInputOutput.GetDimensions(width, height);
    if (any(id.xy >= uint2(width, height))) return;
    float2 uv = (id.xy + 0.5) / float2(width, height);
    float3 color = g.parameters.w == 2 ? antialias(uv, 1 / float2(width, height), true) : sampleDisplay(uv, true);
    displayInputOutput[id.xy] = float4(color, 1);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void NativeResolve(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= (uint2)g.size.zw)) return;
    float2 uv = (id.xy + 0.5) / g.size.zw;
    hudlessOutput[id.xy] = float4(antialias(uv, 1 / g.size.zw, false), 1);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void CopyDisplay(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= (uint2)g.size.zw)) return;
    hudlessOutput[id.xy] = displayInputImage[id.xy];
}
[shader("compute")]
[numthreads(8, 8, 1)]
void Composite(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= (uint2)g.size.zw)) return;
    float4 ui = uiImage[id.xy];
    finalOutput[id.xy] = float4(hudlessImage[id.xy].rgb * (1 - ui.a) + ui.rgb, 1);
}
struct UiInput { float2 position : POSITION; float2 uv : TEXCOORD; float4 color : COLOR; };
struct UiVertex { float4 position : SV_Position; float2 uv : TEXCOORD; float4 color : COLOR; };
[shader("vertex")]
UiVertex UiVS(UiInput input)
{
    UiVertex output; output.position = float4(input.position / g.size.zw * float2(2, -2) + float2(-1, 1), 0, 1); output.uv = input.uv; output.color = input.color; return output;
}
[shader("fragment")]
float4 UiPS(UiVertex input) : SV_Target
{
    float4 value = input.color * fontImage.Sample(linearSampler, input.uv);
    return float4(value.rgb * value.a, value.a);
}
