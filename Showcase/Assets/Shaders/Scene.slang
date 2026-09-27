#ifndef HAS_RAY_QUERY
#define HAS_RAY_QUERY 1
#endif

#include "ToneMapping.slang"

struct Vertex { float4 position; float4 normal; float4 tangent; float4 uv; };
struct Material { float4 baseColor; float4 emissiveMetallic; float4 parameters; float4 textures; };
struct ObjectData { float4 offset; float4 previousOffset; uint4 geometry; };
struct Globals
{
    row_major float4x4 viewProjection;
    row_major float4x4 currentViewProjection;
    row_major float4x4 previousViewProjection;
    row_major float4x4 inverseViewProjection;
    float4 camera; // world position.xyz, tan(vertical FOV / 2).w
    float4 size; // render width, height, output width, height
    float4 sun;
    float4 scene; // object count, frame number, ray epsilon, scale
    float4 parameters; // ray tracing, exposure compensation, input image, reserved
    float4 jitter; // pixel jitter.xy; generate RR guides when z is set
    float4 center;
    row_major float4x4 sunViewProjection;
    float4 lighting; // sun irradiance, sky radiance, reserved, raster AO radius
    float4 exposure; // automatic metering enabled, delta seconds, reset history, reserved
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
[[vk::binding(21, 0)]] Texture2D<float> shadowImage : register(t20);
[[vk::binding(22, 0)]] Texture2D<float> exposureImage : register(t21);
[[vk::binding(23, 0)]] Texture2D<float> previousExposureImage : register(t22);
[[vk::binding(24, 0)]] Texture2D<float4> fontImage : register(t23);
[[vk::binding(32, 0)]] RWTexture2D<float4> sceneOutput : register(u0);
[[vk::binding(33, 0)]] RWTexture2D<float4> specularOutput : register(u1);
[[vk::binding(34, 0)]] RWTexture2D<float> hitOutput : register(u2);
[[vk::binding(35, 0)]] RWTexture2D<float4> reconstructedOutput : register(u3);
[[vk::binding(36, 0)]] RWTexture2D<float4> displayInputOutput : register(u4);
[[vk::binding(37, 0)]] RWTexture2D<float4> hudlessOutput : register(u5);
[[vk::binding(38, 0)]] RWTexture2D<float4> finalOutput : register(u6);
[[vk::binding(39, 0)]] RWTexture2D<float2> motionOutput : register(u7);
[[vk::binding(40, 0)]] RWTexture2D<float4> diffuseOutput : register(u8);
[[vk::binding(41, 0)]] RWTexture2D<float> exposureOutput : register(u9);
[[vk::binding(48, 0)]] SamplerState linearSampler : register(s0);

float3 srgbToLinear(float3 c) { return select(c <= 0.04045, c / 12.92, pow((c + 0.055) / 1.055, 2.4)); }
float3 linearToSrgb(float3 c) { return select(c <= 0.0031308, c * 12.92, 1.055 * pow(max(c, 0), 1.0 / 2.4) - 0.055); }
float4 unpackColor(uint p) { return float4(p & 255, (p >> 8) & 255, (p >> 16) & 255, p >> 24) / 255.0; }
float4 fetch(uint offset, int2 p, uint2 size)
{
    p = (p % int2(size) + int2(size)) % int2(size);
    return unpackColor(texels[offset + p.y * size.x + p.x]);
}
float4 sampleMip(uint offset, uint2 size, float2 uv, bool srgb)
{
    float2 p = frac(uv) * size - 0.5;
    int2 pixel = int2(floor(p));
    float2 f = frac(p);
    float4 a = fetch(offset, pixel, size), b = fetch(offset, pixel + int2(1, 0), size);
    float4 c = fetch(offset, pixel + int2(0, 1), size), d = fetch(offset, pixel + 1, size);
    if (srgb) { a.rgb = srgbToLinear(a.rgb); b.rgb = srgbToLinear(b.rgb); c.rgb = srgbToLinear(c.rgb); d.rgb = srgbToLinear(d.rgb); }
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
float4 sampleTexture(int index, float2 uv, float lod, bool srgb)
{
    if (index < 0) return 1;
    uint4 info = textureInfo[index];
    float mip = clamp(lod, 0, float(info.w - 1));
    uint level = (uint)mip;
    uint2 size = info.yz;
    uint offset = info.x;
    for (uint i = 0; i < level; i++) { offset += size.x * size.y; size = max(size / 2, 1); }
    float4 color = sampleMip(offset, size, uv, srgb);
    if (level + 1 >= info.w || frac(mip) == 0) return color;
    return lerp(color, sampleMip(offset + size.x * size.y, max(size / 2, 1), uv, srgb), frac(mip));
}
float textureLod(int index, float2 dx, float2 dy)
{
    if (index < 0) return 0;
    float2 size = textureInfo[index].yz;
    return max(0.5 * log2(max(dot(dx * size, dx * size), dot(dy * size, dy * size))), 0);
}
struct Surface { float3 albedo; float3 normal; float3 emissive; float metallic; float roughness; float alpha; };
Surface surface(Material m, float2 uv, float3 n, float4 tangent, float4 lod)
{
    Surface s;
    float4 color = m.baseColor * sampleTexture((int)m.textures.x, uv, lod.x, true);
    float4 mr = sampleTexture((int)m.textures.z, uv, lod.z, false);
    s.albedo = color.rgb;
    s.alpha = color.a;
    s.metallic = saturate(m.emissiveMetallic.w * mr.b);
    s.roughness = clamp(m.parameters.x * mr.g, 0.045, 1);
    s.emissive = m.emissiveMetallic.rgb * sampleTexture((int)m.textures.w, uv, lod.w, true).rgb;
    s.normal = normalize(n);
    if (m.textures.y >= 0)
    {
        float3 t = tangent.xyz - n * dot(n, tangent.xyz);
        // Some imported faces have a tangent parallel to their normal. Match
        // the loader's missing-tangent fallback instead of normalizing zero and
        // contaminating the indirect path and RR input with NaNs.
        float tangentLengthSquared = dot(t, t);
        t = tangentLengthSquared > 0 ? t * rsqrt(tangentLengthSquared) : tangentAxis(n);
        float3 b = cross(n, t) * tangent.w;
        float3 mapped = sampleTexture((int)m.textures.y, uv, lod.y, false).xyz * 2 - 1;
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
    float2 uvDx = ddx(input.uv), uvDy = ddy(input.uv);
    float4 lod;
    for (uint i = 0; i < 4; i++) lod[i] = textureLod((int)m.textures[i], uvDx, uvDy);
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
// Analytic daylight environment, shared by camera rays and indirect illumination.
// The sun is sampled separately as a finite directional emitter.
float3 sky(float3 direction)
{
    float elevation = saturate(direction.y);
    float3 horizon = float3(0.78, 0.84, 0.92);
    float3 zenith = float3(0.28, 0.48, 0.78);
    float3 atmosphere = lerp(horizon, zenith, sqrt(elevation));
    float towardSun = saturate(dot(direction, g.sun.xyz));
    atmosphere += float3(0.2, 0.17, 0.12) * pow(towardSun, 16) * elevation;
    float3 ground = float3(0.16, 0.15, 0.13);
    return g.lighting.y * lerp(ground, atmosphere, smoothstep(-0.08, 0.04, direction.y));
}
uint hash(uint x) { x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; return x ^ (x >> 16); }
float random(inout uint state) { state = hash(state); return float(state >> 8) / 16777216.0; }
float3 tangentAxis(float3 n) { return normalize(cross(abs(n.y) < 0.99 ? float3(0, 1, 0) : float3(1, 0, 0), n)); }
float3 basisDirection(float3 n, float3 local)
{
    float3 t = tangentAxis(n);
    return normalize(t * local.x + cross(n, t) * local.y + n * local.z);
}
float3 sampleSun(inout uint state)
{
    float r = sqrt(random(state)) * tan(g.sun.w), phi = random(state) * 6.2831853;
    return basisDirection(g.sun.xyz, float3(r * cos(phi), r * sin(phi), 1));
}
float3 fresnel(float3 f0, float cosine) { return f0 + (1 - f0) * pow(1 - saturate(cosine), 5); }
float ggxDistribution(float nh, float alpha)
{
    float alpha2 = alpha * alpha;
    float denominator = nh * nh * (alpha2 - 1) + 1;
    return alpha2 / max(3.14159265 * denominator * denominator, 1e-8);
}
// Height-correlated Smith visibility. The direct BRDF, path weights and RR
// specular guide use this same GGX model (see Filament's standard material model).
float smithVisibility(float nv, float nl, float alpha)
{
    float alpha2 = alpha * alpha;
    float v = nl * sqrt(nv * nv * (1 - alpha2) + alpha2);
    float l = nv * sqrt(nl * nl * (1 - alpha2) + alpha2);
    return 0.5 / max(v + l, 1e-6);
}
float smithG1(float nv, float alpha)
{
    return 2 * nv / max(nv + sqrt(alpha * alpha + (1 - alpha * alpha) * nv * nv), 1e-6);
}
float3 brdf(Surface s, float3 v, float3 l)
{
    float nv = max(dot(s.normal, v), 1e-4), nl = max(dot(s.normal, l), 0);
    if (nl == 0) return 0;
    float3 h = normalize(v + l);
    float nh = saturate(dot(s.normal, h)), vh = saturate(dot(v, h));
    float alpha = s.roughness * s.roughness;
    float3 f = fresnel(lerp(0.04, s.albedo, s.metallic), vh);
    float3 diffuse = (1 - f) * (1 - s.metallic) * s.albedo / 3.14159265;
    return (diffuse + f * ggxDistribution(nh, alpha) * smithVisibility(nv, nl, alpha)) * nl;
}
// Heitz 2018, Sampling the GGX Distribution of Visible Normals, JCGT 7(4).
// Visible-normal sampling avoids the very large grazing-angle weights of NDF sampling.
float3 sampleVisibleGgx(float3 view, float alpha, float2 u)
{
    float3 stretched = normalize(float3(alpha * view.xy, view.z));
    float lengthSquared = dot(stretched.xy, stretched.xy);
    float3 t1 = lengthSquared > 0 ? float3(-stretched.y, stretched.x, 0) * rsqrt(lengthSquared) : float3(1, 0, 0);
    float3 t2 = cross(stretched, t1);
    float radius = sqrt(u.x), phi = 6.2831853 * u.y;
    float x = radius * cos(phi), y = radius * sin(phi);
    float blend = 0.5 * (1 + stretched.z);
    y = (1 - blend) * sqrt(max(1 - x * x, 0)) + blend * y;
    float3 normal = x * t1 + y * t2 + sqrt(max(1 - x * x - y * y, 0)) * stretched;
    return normalize(float3(alpha * normal.xy, max(normal.z, 0)));
}
float3 sampleSpecular(Surface s, float3 view, float2 u, out float3 halfVector)
{
    float3 t = tangentAxis(s.normal), b = cross(s.normal, t);
    float3 localView = float3(dot(view, t), dot(view, b), max(dot(view, s.normal), 1e-4));
    float3 localHalf = sampleVisibleGgx(normalize(localView), s.roughness * s.roughness, u);
    halfVector = normalize(t * localHalf.x + b * localHalf.y + s.normal * localHalf.z);
    return reflect(-view, halfVector);
}
float3 specularWeight(float3 f0, float nv, float nl, float vh, float alpha)
{
    float geometry = 4 * nv * nl * smithVisibility(nv, nl, alpha);
    return fresnel(f0, vh) * geometry / max(smithG1(nv, alpha), 1e-6);
}
float3 directLight(Surface s, float3 p, float3 view, float3 sunDirection)
{
    float visibility;
#if HAS_RAY_QUERY
    if (g.parameters.x > 0) visibility = trace(p + s.normal * g.scene.z, sunDirection, g.scene.w * 4, true).triangle == 0xffffffff ? 1 : 0;
    else
#endif
    visibility = shadowVisibility(p, s.normal);
    float3 color = brdf(s, view, sunDirection) * float3(1, 0.96, 0.88) * g.lighting.x * visibility;
    return color;
}
float3 integratedSpecularAlbedo(float3 f0, float roughness, float nv)
{
    const uint sampleCount = 32;
    nv = max(nv, 1e-4);
    float3 view = float3(sqrt(max(1 - nv * nv, 0)), 0, nv);
    float3 result = 0;
    float alpha = roughness * roughness;
    for (uint i = 0; i < sampleCount; i++)
    {
        float2 u = float2((i + 0.5) / sampleCount, reversebits(i) * 2.3283064365386963e-10);
        float3 h = sampleVisibleGgx(view, alpha, u);
        float3 l = reflect(-view, h);
        if (l.z > 0) result += specularWeight(f0, nv, l.z, saturate(dot(view, h)), alpha);
    }
    return saturate(result / sampleCount);
}
float3 worldPosition(int2 pixel, float depth)
{
    float2 uv = (pixel + 0.5) / g.size.xy;
    float4 world = mul(float4(uv * float2(2, -2) + float2(-1, 1), depth, 1), g.inverseViewProjection);
    return world.xyz / world.w;
}
// Screen-space horizon occlusion affects the raster approximation's indirect light
// only. Traced illumination already measures visibility and must not be darkened twice.
float ambientVisibility(int2 pixel, float3 p, float3 n)
{
    float radius = g.lighting.w;
    if (radius <= 0) return 1;
    float viewDepth = mul(float4(p, 1), g.viewProjection).w;
    float radiusPixels = radius * g.size.y / max(2 * g.camera.w * viewDepth, g.scene.z);
    const uint directions = 8;
    const uint steps = 3;
    float occlusion = 0;
    for (uint i = 0; i < directions; i++)
    {
        float phi = (i + 0.5) * 6.2831853 / directions;
        float2 direction = float2(cos(phi), sin(phi));
        float horizon = 0;
        for (uint j = 1; j <= steps; j++)
        {
            int2 samplePixel = pixel + int2(round(direction * radiusPixels * (float(j) / steps)));
            if (any(samplePixel < 0) || any(samplePixel >= int2(g.size.xy)) || all(samplePixel == pixel)) continue;
            float depth = depthImage[samplePixel];
            if (depth >= 1) continue;
            float3 difference = worldPosition(samplePixel, depth) - p;
            float distanceSquared = dot(difference, difference);
            if (distanceSquared <= g.scene.z * g.scene.z) continue;
            float distance = sqrt(distanceSquared);
            float falloff = saturate(1 - distanceSquared / (radius * radius));
            float angle = max(dot(n, difference / distance) - g.scene.z / distance, 0);
            horizon = max(horizon, angle * falloff * falloff);
        }
        occlusion += horizon;
    }
    return saturate(1 - occlusion / directions);
}
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
        lod[i] = index < 0 ? 0 : max(log2(max(footprint * uvScale * max(textureInfo[index].y, textureInfo[index].z), 1)), 0);
    }
    Surface s = surface(material, uv, n, tangent, lod);
    if (dot(s.normal, direction) > 0) s.normal = -s.normal;
    return s;
}
float3 secondaryRadiance(float3 origin, float3 direction, inout uint rng, out float distance)
{
    const uint surfaceBounces = 2;
    float3 radiance = 0, throughput = 1;
    distance = g.scene.w * 4;
    // Resolve emission/environment after the last scattering event as well.
    // Stopping before tracing that ray would discard valid terminal skylight.
    for (uint bounce = 0; bounce <= surfaceBounces; bounce++)
    {
        Hit hit = trace(origin, direction, g.scene.w * 4, false);
        if (bounce == 0) distance = hit.distance;
        if (hit.triangle == 0xffffffff)
        {
            radiance += throughput * sky(direction);
            break;
        }
        Surface s = hitSurface(hit, direction);
        radiance += throughput * s.emissive;
        if (bounce == surfaceBounces) break;
        float3 p = origin + direction * hit.distance;
        float3 view = -direction;
        radiance += throughput * directLight(s, p, view, sampleSun(rng));
        float3 f0 = lerp(0.04, s.albedo, s.metallic);
        float specularProbability = clamp(max(f0.r, max(f0.g, f0.b)), 0.05, 0.95);
        float3 halfVector;
        float2 u = float2(random(rng), random(rng));
        if (random(rng) < specularProbability)
        {
            direction = sampleSpecular(s, view, u, halfVector);
        }
        else
        {
            float r = sqrt(u.x), phi = u.y * 6.2831853;
            direction = basisDirection(s.normal, float3(r * cos(phi), r * sin(phi), sqrt(1 - u.x)));
            halfVector = normalize(view + direction);
        }
        float nv = max(dot(s.normal, view), 1e-4), nl = dot(s.normal, direction);
        if (nl <= 0) break;
        float alpha = s.roughness * s.roughness;
        float specularPdf = ggxDistribution(saturate(dot(s.normal, halfVector)), alpha) * smithG1(nv, alpha) / (4 * nv);
        float pdf = lerp(nl / 3.14159265, specularPdf, specularProbability);
        throughput *= brdf(s, view, direction) / max(pdf, 1e-6);
        origin = p + s.normal * g.scene.z;
    }
    return radiance;
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
    Surface s; s.albedo = albedoImage[pixel].rgb; s.normal = normalize(normal.xyz); s.roughness = normal.w; s.emissive = emissive.rgb; s.metallic = emissive.w; s.alpha = 1;
    if (dot(s.normal, view) < 0) s.normal = -s.normal;
    uint rng = hash(pixel.x + pixel.y * (uint)g.size.x + (uint)g.scene.y * 0x9e3779b9);
    float3 sunDirection = g.sun.xyz;
    if (g.parameters.x > 0)
    {
        sunDirection = sampleSun(rng);
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
        float3 diffuseHalf = normalize(view + diffuseDirection);
        color += s.albedo * (1 - s.metallic) * (1 - fresnel(f0, dot(view, diffuseHalf))) * secondaryRadiance(p + s.normal * g.scene.z, diffuseDirection, rng, unused);
        float3 halfVector;
        float3 reflected = sampleSpecular(s, view, float2(random(rng), random(rng)), halfVector);
        if (dot(reflected, s.normal) > 0)
        {
            float nl = dot(s.normal, reflected), vh = saturate(dot(view, halfVector));
            float3 weight = specularWeight(f0, max(nv, 1e-4), nl, vh, s.roughness * s.roughness);
            color += secondaryRadiance(p + s.normal * g.scene.z, reflected, rng, specularDistance) * weight;
        }
    }
    else
#endif
    {
        float visibility = ambientVisibility(int2(pixel), p, s.normal);
        float3 diffuseEnvironment = lerp(sky(float3(0, 1, 0)), sky(s.normal), 0.5);
        color += s.albedo * (1 - s.metallic) * (1 - specularAlbedo) * diffuseEnvironment * visibility;
        // Lagarde's specular-occlusion approximation for the raster environment.
        float specularVisibility = saturate(pow(nv + visibility, exp2(-16 * s.roughness - 1)) - 1 + visibility);
        color += lerp(sky(reflect(-view, s.normal)), diffuseEnvironment, s.roughness * s.roughness) * specularAlbedo * specularVisibility;
    }
    sceneOutput[pixel] = float4(max(color, 0), 1);
    if (g.jitter.z > 0)
    {
        specularOutput[pixel] = float4(specularAlbedo, 1);
        diffuseOutput[pixel] = float4(s.albedo * (1 - s.metallic), 1);
        hitOutput[pixel] = specularDistance;
    }
}
float3 sampleHdr(float2 uv)
{
    return g.parameters.z > 0 ? reconstructedImage.SampleLevel(linearSampler, uv, 0).rgb : sceneImage.SampleLevel(linearSampler, uv, 0).rgb;
}
// Average scene luminance in linear light before converting to exposure stops.
// Averaging logs of sparse RT samples would let zeros dominate the meter and
// overexpose the lit floor and sky. Metering runs before UI composition.
groupshared float meterLuminance[64];
[shader("compute")]
[numthreads(8, 8, 1)]
void MeterExposure(uint3 id : SV_GroupThreadID, uint index : SV_GroupIndex)
{
    if (g.exposure.x == 0)
    {
        if (index == 0) exposureOutput[uint2(0, 0)] = 0;
        return;
    }
    const uint samplesPerTileAxis = 16;
    const float minimumLuminance = 1.0 / 65536.0;
    const float middleGray = 0.18;
    const float maximumExposureStops = 8;
    float tileLuminance = 0;
    for (uint y = 0; y < samplesPerTileAxis; y++)
        for (uint x = 0; x < samplesPerTileAxis; x++)
        {
            float2 uv = (id.xy + (float2(x, y) + 0.5) / samplesPerTileAxis) / 8.0;
            tileLuminance += dot(max(sampleHdr(uv), 0), float3(0.2126, 0.7152, 0.0722));
        }
    meterLuminance[index] = tileLuminance / (samplesPerTileAxis * samplesPerTileAxis);
    GroupMemoryBarrierWithGroupSync();
    for (uint stride = 32; stride > 0; stride >>= 1)
    {
        if (index < stride) meterLuminance[index] += meterLuminance[index + stride];
        GroupMemoryBarrierWithGroupSync();
    }
    if (index == 0)
    {
        float averageLuminance = max(meterLuminance[0] / 64.0, minimumLuminance);
        float target = clamp(log2(middleGray / averageLuminance), -maximumExposureStops, maximumExposureStops);
        float adapted = target;
        // Reset/resize never reads an uninitialized history image. Otherwise use
        // a time-based half-life, independent of FPS and the three frame slots.
        if (g.exposure.z == 0)
        {
            float previous = previousExposureImage[uint2(0, 0)];
            const float brightenHalfLifeSeconds = 1.0;
            const float darkenHalfLifeSeconds = 0.25;
            float halfLife = target > previous ? brightenHalfLifeSeconds : darkenHalfLifeSeconds;
            adapted = lerp(previous, target, 1 - exp2(-max(g.exposure.y, 0) / halfLife));
        }
        exposureOutput[uint2(0, 0)] = adapted;
    }
}
float3 sampleDisplay(float2 uv, bool toneMap)
{
    return toneMap ? linearToSrgb(agxToneMap(sampleHdr(uv) * exp2(exposureImage[uint2(0, 0)] + g.parameters.y))) : displayInputImage.SampleLevel(linearSampler, uv, 0).rgb;
}
float luminance(float3 c) { return dot(c, float3(0.299, 0.587, 0.114)); }
// FXAA edge filter for the native raster path.
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
    float3 color = sampleDisplay(uv, true);
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
