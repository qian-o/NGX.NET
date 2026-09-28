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
    float4 parameters; // ray tracing, exposure compensation, input image, reverse-order triangle count (0 = forward)
    float4 jitter; // pixel jitter.xy; RR guides enabled.z; texture mip bias.w
    float4 center;
    row_major float4x4 sunViewProjection;
    float4 lighting; // sun irradiance, sky radiance, reserved, raster AO radius
    float4 exposure; // automatic metering enabled, delta seconds, reset history, reserved
    float4 environmentMinimum;
    float4 environmentMaximum;
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
[[vk::binding(22, 0)]] Texture2D<float4> exposureImage : register(t21);
[[vk::binding(23, 0)]] Texture2D<float4> luminanceImage : register(t22);
[[vk::binding(24, 0)]] Texture2D<float> filteredLuminanceImage : register(t23);
[[vk::binding(25, 0)]] Texture2D<float2> geometricNormalImage : register(t24);
[[vk::binding(26, 0)]] Texture2D<float4> previousExposureImage : register(t25);
[[vk::binding(27, 0)]] Texture2D<float4> fontImage : register(t26);
[[vk::binding(28, 0)]] Texture2DArray<float4> lightingSamplesImage : register(t27);
[[vk::binding(32, 0)]] RWTexture2D<float4> sceneOutput : register(u0);
[[vk::binding(33, 0)]] RWTexture2D<float4> specularOutput : register(u1);
[[vk::binding(34, 0)]] RWTexture2D<float> hitOutput : register(u2);
[[vk::binding(35, 0)]] RWTexture2D<float4> reconstructedOutput : register(u3);
[[vk::binding(36, 0)]] RWTexture2D<float4> displayInputOutput : register(u4);
[[vk::binding(37, 0)]] RWTexture2D<float4> hudlessOutput : register(u5);
[[vk::binding(38, 0)]] RWTexture2D<float4> finalOutput : register(u6);
[[vk::binding(39, 0)]] RWTexture2D<float2> motionOutput : register(u7);
[[vk::binding(40, 0)]] RWTexture2D<float4> diffuseOutput : register(u8);
[[vk::binding(41, 0)]] RWTexture2D<float4> exposureOutput : register(u9);
[[vk::binding(42, 0)]] RWTexture2D<float4> luminanceOutput : register(u10);
[[vk::binding(43, 0)]] RWTexture2D<float> filteredLuminanceOutput : register(u11);
[[vk::binding(44, 0)]] RWTexture2DArray<float4> lightingSamplesOutput : register(u12);
[[vk::binding(48, 0)]] SamplerState linearSampler : register(s0);

float3 linearToSrgb(float3 c) { return select(c <= 0.0031308, c * 12.92, 1.055 * pow(max(c, 0), 1.0 / 2.4) - 0.055); }
float4 fetch(uint offset, int2 p, uint2 size, bool srgb)
{
    // sampleMip's frac(uv) guarantees each bilinear corner is in [-1, size].
    // Wrap that single boundary without two integer divisions per coordinate.
    p = select(p < 0, p + int2(size), p);
    p = select(p >= int2(size), p - int2(size), p);
    uint packed = texels[offset + p.y * size.x + p.x];
    uint3 rgb = uint3(packed & 255, (packed >> 8) & 255, (packed >> 16) & 255);
    // Scene.Load stores the exact 8-bit sRGB decode table in the buffer prefix.
    float3 linear = srgb ? asfloat(uint3(texels[rgb.x], texels[rgb.y], texels[rgb.z])) : float3(rgb) / 255.0;
    return float4(linear, (packed >> 24) / 255.0);
}
float4 sampleMip(uint offset, uint2 size, float2 uv, bool srgb)
{
    float2 p = frac(uv) * size - 0.5;
    int2 pixel = int2(floor(p));
    float2 f = frac(p);
    float4 a = fetch(offset, pixel, size, srgb), b = fetch(offset, pixel + int2(1, 0), size, srgb);
    float4 c = fetch(offset, pixel + int2(0, 1), size, srgb), d = fetch(offset, pixel + 1, size, srgb);
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
float4 sampleTextureGrad(int index, float2 uv, float2 dx, float2 dy, float bias, bool srgb)
{
    if (index < 0) return 1;
    float2 size = textureInfo[index].yz;
    dx *= size * exp2(bias);
    dy *= size * exp2(bias);
    // Principal axes of the pixel footprint in texel space. Selecting only its
    // longest dimension as an isotropic mip blurs grazing floors and walls.
    float xx = dx.x * dx.x + dy.x * dy.x;
    float xy = dx.x * dx.y + dy.x * dy.y;
    float yy = dx.y * dx.y + dy.y * dy.y;
    float difference = sqrt(max((xx - yy) * (xx - yy) + 4 * xy * xy, 0));
    float majorSquared = max(0.5 * (xx + yy + difference), 0);
    float minorSquared = max(0.5 * (xx + yy - difference), 0);
    float major = sqrt(majorSquared);
    const uint maxAnisotropy = 8;
    float tapWidth = max(1, max(sqrt(minorSquared), major / maxAnisotropy));
    uint taps = clamp((uint)ceil(major / tapWidth), 1u, maxAnisotropy);
    float2 axis = float2(xy, majorSquared - xx);
    axis = dot(axis, axis) > 0 ? normalize(axis) : float2(1, 0);
    float2 span = axis * major / size;
    float lod = log2(tapWidth);
    float4 result = 0;
    for (uint i = 0; i < taps; i++)
        result += sampleTexture(index, uv + span * ((i + 0.5) / taps - 0.5), lod, srgb);
    return result / taps;
}
// Keep the actual triangle normal separate from interpolated/normal-mapped shading.
// Reflection must stay above the geometric surface even when a normal map tilts it.
float2 octahedralSign(float2 v) { return float2(v.x < 0 ? -1 : 1, v.y < 0 ? -1 : 1); }
float2 encodeGeometricNormal(float3 n)
{
    n /= dot(abs(n), float3(1, 1, 1));
    return n.z >= 0 ? n.xy : (1 - abs(n.yx)) * octahedralSign(n.xy);
}
float3 decodeGeometricNormal(float2 encoded)
{
    float3 n = float3(encoded, 1 - abs(encoded.x) - abs(encoded.y));
    if (n.z < 0) n.xy = (1 - abs(n.yx)) * octahedralSign(n.xy);
    return normalize(n);
}
struct Surface { float3 albedo; float3 normal; float3 geometricNormal; float3 emissive; float metallic; float roughness; float alpha; };
bool aboveSurface(Surface s, float3 direction)
{
    return dot(s.geometricNormal, direction) > 0 && dot(s.normal, direction) > 0;
}
float3 rayOrigin(float3 p, Surface s) { return p + s.geometricNormal * g.scene.z; }
Surface decodeSurface(Material m, float3 n, float4 tangent, float4 baseColor, float4 normalMap, float4 mr, float3 emission)
{
    Surface s;
    float4 color = m.baseColor * baseColor;
    s.albedo = color.rgb;
    s.alpha = color.a;
    s.metallic = saturate(m.emissiveMetallic.w * mr.b);
    s.roughness = clamp(m.parameters.x * mr.g, 0.045, 1);
    s.emissive = m.emissiveMetallic.rgb * emission;
    s.normal = normalize(n);
    s.geometricNormal = s.normal;
    if (m.textures.y >= 0)
    {
        float3 t = tangent.xyz - n * dot(n, tangent.xyz);
        // Some imported faces have a tangent parallel to their normal. Match
        // the loader's missing-tangent fallback instead of normalizing zero and
        // contaminating the indirect path and RR input with NaNs.
        float tangentLengthSquared = dot(t, t);
        t = tangentLengthSquared > 0 ? t * rsqrt(tangentLengthSquared) : tangentAxis(n);
        float3 b = cross(n, t) * tangent.w;
        float3 mapped = normalMap.xyz * 2 - 1;
        mapped.xy *= m.parameters.y;
        s.normal = normalize(t * mapped.x + b * mapped.y + n * mapped.z);
    }
    return s;
}
Surface surface(Material m, float2 uv, float3 n, float4 tangent, float4 lod)
{
    return decodeSurface(m, n, tangent,
        sampleTexture((int)m.textures.x, uv, lod.x, true),
        sampleTexture((int)m.textures.y, uv, lod.y, false),
        sampleTexture((int)m.textures.z, uv, lod.z, false),
        sampleTexture((int)m.textures.w, uv, lod.w, true).rgb);
}
struct RasterVertex
{
    precise float4 position : SV_Position;
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
    if (g.parameters.w > 0)
    {
        // Strict reverse-depth prepass keeps the first surface at an exactly equal depth.
        // Reverse only triangle order (not winding) so the equal-depth color
        // pass leaves that same first surface last when coplanar faces overlap.
        id = ((uint)g.parameters.w - 1 - id / 3) * 3 + id % 3;
    }
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
struct GBuffer { float4 albedo : SV_Target0; float4 normal : SV_Target1; float4 emissive : SV_Target2; float2 motion : SV_Target3; float2 geometricNormal : SV_Target4; };
[shader("fragment")]
void DepthPS(RasterVertex input, bool front : SV_IsFrontFace)
{
    Material m = materials[input.material];
    if (!front && m.parameters.w == 0) discard;
    if (m.parameters.z >= 0)
    {
        // Match the color pass's coverage, including reconstruction mip bias
        // and anisotropic footprint. Opaque surfaces do not sample any textures.
        float alpha = m.baseColor.a * sampleTextureGrad((int)m.textures.x, input.uv, ddx(input.uv), ddy(input.uv), g.jitter.w, true).a;
        clip(alpha - m.parameters.z);
    }
}
[shader("fragment")]
[earlydepthstencil]
GBuffer ScenePS(RasterVertex input, bool front : SV_IsFrontFace)
{
    Material m = materials[input.material];
    if (!front && m.parameters.w == 0) discard;
    float2 uvDx = ddx(input.uv), uvDy = ddy(input.uv);
    Surface s = decodeSurface(m, normalize(input.normal), input.tangent,
        sampleTextureGrad((int)m.textures.x, input.uv, uvDx, uvDy, g.jitter.w, true),
        sampleTextureGrad((int)m.textures.y, input.uv, uvDx, uvDy, g.jitter.w, false),
        sampleTextureGrad((int)m.textures.z, input.uv, uvDx, uvDy, g.jitter.w, false),
        sampleTextureGrad((int)m.textures.w, input.uv, uvDx, uvDy, g.jitter.w, true).rgb);
    clip(s.alpha - m.parameters.z);
    if (dot(s.normal, g.camera.xyz - input.world) < 0) s.normal = -s.normal;
    float3 geometricNormal = normalize(cross(ddx(input.world), ddy(input.world)));
    if (dot(geometricNormal, g.camera.xyz - input.world) < 0) geometricNormal = -geometricNormal;
    GBuffer output;
    output.albedo = float4(s.albedo, 1);
    output.normal = float4(s.normal, s.roughness);
    output.emissive = float4(s.emissive, s.metallic);
    float2 motion = input.previous.xy / input.previous.w - input.current.xy / input.current.w;
    output.motion = motion * float2(0.5, -0.5) * g.size.xy;
    output.geometricNormal = encodeGeometricNormal(geometricNormal);
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
// Material facing controls camera visibility, not whether an opaque wall blocks
// light. Back-face culling here lets visibility and bounce rays escape thin roofs.
static const uint traversalFlags = RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES;
void traverse<let flags : uint>(inout RayQuery<flags> query, float3 origin, float3 direction, float limit)
{
    RayDesc ray;
    ray.Origin = origin;
    ray.Direction = direction;
    ray.TMin = g.scene.z;
    ray.TMax = limit;
    query.TraceRayInline(sceneAcceleration, RAY_FLAG_NONE, 0xff, ray);
    while (query.Proceed())
    {
        if (query.CandidateType() != CANDIDATE_NON_OPAQUE_TRIANGLE) continue;
        uint objectIndex = query.CandidateInstanceID();
        // Each BLAS contains one non-indexed triangle geometry.
        uint firstVertex = objects[objectIndex].geometry.x + query.CandidatePrimitiveIndex() * 3;
        Vertex a = vertices[firstVertex], b = vertices[firstVertex + 1], c = vertices[firstVertex + 2];
        Material material = materials[(uint)a.normal.w];
        // Opaque hits are resolved by traversal. Alpha-tested surfaces retain
        // their cutouts on both sides for visibility and indirect-light rays.
        if (material.parameters.z >= 0)
        {
            float2 bary = query.CandidateTriangleBarycentrics();
            float2 uv = a.uv.xy * (1 - bary.x - bary.y) + b.uv.xy * bary.x + c.uv.xy * bary.y;
            if (material.baseColor.a * sampleTexture((int)material.textures.x, uv, 0, true).a < material.parameters.z) continue;
        }
        query.CommitNonOpaqueTriangleHit();
    }
}
bool occluded(float3 origin, float3 direction, float limit)
{
    if (limit <= g.scene.z) return false;
    // Visibility only needs a committed hit. Compile-time first-hit termination
    // avoids carrying nearest-hit distance, instance and barycentric results.
    RayQuery<traversalFlags | RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH> query;
    traverse(query, origin, direction, limit);
    return query.CommittedStatus() == COMMITTED_TRIANGLE_HIT;
}
Hit trace(float3 origin, float3 direction, float limit)
{
    Hit hit;
    hit.distance = limit; hit.triangle = 0xffffffff; hit.object = 0; hit.bary = 0;
    if (limit <= g.scene.z) return hit;
    RayQuery<traversalFlags> query;
    traverse(query, origin, direction, limit);
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
float random(inout uint state) { state += 0x9e3779b9; return float(hash(state) >> 8) / 16777216.0; }
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
    float denominator = (1 - nh * nh) + nh * nh * alpha2;
    return alpha2 / (3.14159265 * denominator * denominator);
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
    if (!aboveSurface(s, l)) return 0;
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
#include "EnvironmentLighting.slang"

float3 directLight(Surface s, float3 p, float3 view, float3 sunDirection)
{
    if (!aboveSurface(s, sunDirection)) return 0;
    float visibility;
#if HAS_RAY_QUERY
    visibility = occluded(rayOrigin(p, s), sunDirection, g.scene.w * 4) ? 0 : 1;
#else
    visibility = shadowVisibility(p, s.geometricNormal);
#endif
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
            if (depth <= 0) continue;
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
// RR guide section 3.4.9 specifies FP16_MAX or FP32_MAX for the sky. This is
// independent of the finite traversal limit used to trace the scene's geometry.
static const float skyHitDistance = 65504.0;
Surface primarySurface(uint2 pixel, float3 view)
{
    float4 normal = normalImage[pixel], emissive = emissiveImage[pixel];
    Surface s; s.albedo = albedoImage[pixel].rgb; s.normal = normalize(normal.xyz); s.roughness = normal.w;
    s.geometricNormal = decodeGeometricNormal(geometricNormalImage[pixel]);
    s.emissive = emissive.rgb; s.metallic = emissive.w; s.alpha = 1;
    if (dot(s.normal, view) < 0) s.normal = -s.normal;
    return s;
}
#include "PathTracing.slang"
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
    if (depth <= 0)
    {
        sceneOutput[pixel] = float4(sky(-view), 1);
        if (g.jitter.z > 0)
        {
            specularOutput[pixel] = float4(0, 0, 0, 1);
            diffuseOutput[pixel] = 0;
            hitOutput[pixel] = skyHitDistance;
        }
        float4 current = mul(float4(-view, 0), g.currentViewProjection);
        float4 previous = mul(float4(-view, 0), g.previousViewProjection);
        motionOutput[pixel] = (previous.xy / previous.w - current.xy / current.w) * float2(0.5, -0.5) * g.size.xy;
        return;
    }
    Surface s = primarySurface(pixel, view);
    float3 color = s.emissive;
    float3 f0 = lerp(0.04, s.albedo, s.metallic);
    float nv = saturate(dot(s.normal, view));
    float3 specularAlbedo = 0;
    if (g.jitter.z > 0 || g.parameters.x == 0)
        specularAlbedo = integratedSpecularAlbedo(f0, s.roughness, nv);
    if (g.parameters.x > 0)
    {
        float3 sampled = 0;
        for (uint path = 0; path < primarySampleCount * 2; path++)
            sampled += lightingSamplesImage.Load(int4(pixel, path, 0)).rgb;
        color += sampled / primarySampleCount;
    }
    else
    {
        color += brdf(s, view, g.sun.xyz) * float3(1, 0.96, 0.88) * g.lighting.x * shadowVisibility(p, s.geometricNormal);
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
    }
}
float3 sampleHdr(float2 uv)
{
    return g.parameters.z > 0 ? reconstructedImage.SampleLevel(linearSampler, uv, 0).rgb : sceneImage.SampleLevel(linearSampler, uv, 0).rgb;
}
#include "Exposure.slang"

float3 sampleDisplay(float2 uv, bool toneMap)
{
    if (!toneMap) return displayInputImage.SampleLevel(linearSampler, uv, 0).rgb;
    float3 hdr = sampleHdr(uv);
    return linearToSrgb(agxToneMap(hdr * exp2(localExposure(uv, hdr) + g.parameters.y)));
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
