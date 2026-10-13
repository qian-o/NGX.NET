// PBRT SurfaceInteraction / OffsetRayOrigin adaptation for hardware intersections.
// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0; see Pbrt.LICENSE.txt.
#include "ShadingNormals.slang"
struct Ray { float3 origin; float3 direction; float tMin; float tMax; };
struct Hit { float distance; uint triangle; uint object; float2 bary; };
struct Surface
{
    float3 p;
    float3 error;
    float3 normal;
    float3 geometricNormal;
    float3 tangent;
    float3 albedo;
    float3 emissive;
    float metallic;
    float roughness;
    bool conductor;
    uint object;
};
static const float rayInfinity = 3.402823466e38;
float3 tangentAxis(float3 n)
{
    // PBRT CoordinateSystem (Duff et al.); no near-parallel cross product.
    float signZ = n.z >= 0 ? 1 : -1;
    float a = -1 / (signZ + n.z), b = n.x * n.y * a;
    return float3(1 + signZ * n.x * n.x * a, signZ * b, -signZ * n.x);
}
float3 basisDirection(float3 n, float3 local)
{
    float3 t = tangentAxis(n);
    return normalize(t * local.x + cross(n, t) * local.y + n * local.z);
}
float gammaError(uint n)
{
    const float epsilon = 5.960464477539063e-8; // half machine epsilon, PBRT Gamma(n)
    return (n * epsilon) / (1 - n * epsilon);
}
float nextFloat(float value, bool up)
{
    if (value == 0) return asfloat(up ? 1u : 0x80000001u);
    uint bits = asuint(value);
    bits += (value > 0) == up ? 1u : 0xffffffffu;
    return asfloat(bits);
}
float3 offsetOrigin(Surface surface, float3 direction)
{
    float distance = dot(abs(surface.geometricNormal), surface.error);
    float3 offset = distance * surface.geometricNormal;
    if (dot(direction, surface.geometricNormal) < 0) offset = -offset;
    float3 p = surface.p + offset;
    for (uint axis = 0; axis < 3; axis++)
        if (offset[axis] != 0) p[axis] = nextFloat(p[axis], offset[axis] > 0);
    return p;
}
Ray spawnRay(Surface surface, float3 direction)
{
    Ray ray;
    ray.origin = offsetOrigin(surface, direction);
    ray.direction = normalize(direction);
    ray.tMin = 0;
    ray.tMax = rayInfinity;
    return ray;
}
void traverse<let flags : uint>(inout RayQuery<flags> query, Ray ray)
{
    RayDesc desc;
    desc.Origin = ray.origin;
    desc.Direction = ray.direction;
    desc.TMin = ray.tMin;
    desc.TMax = ray.tMax;
    query.TraceRayInline(sceneAcceleration, RAY_FLAG_NONE, 0xff, desc);
    while (query.Proceed())
    {
        if (query.CandidateType() != CANDIDATE_NON_OPAQUE_TRIANGLE) continue;
        uint first = objects[query.CandidateInstanceID()].geometry.x + query.CandidatePrimitiveIndex() * 3;
        Vertex a = vertices[first], b = vertices[first + 1], c = vertices[first + 2];
        Material material = materials[(uint)a.normal.w];
        if (material.parameters.z >= 0)
        {
            float2 bary = query.CandidateTriangleBarycentrics();
            float2 uv = a.uv.xy * (1 - bary.x - bary.y) + b.uv.xy * bary.x + c.uv.xy * bary.y;
            if (material.baseColor.a * sampleTexture((int)material.textures.x, uv, 0, true).a < material.parameters.z) continue;
        }
        query.CommitNonOpaqueTriangleHit();
    }
}
Hit intersect(Ray ray)
{
    Hit hit = (Hit)0;
    hit.triangle = 0xffffffff;
    hit.distance = ray.tMax;
    RayQuery<RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES> query;
    traverse(query, ray);
    if (query.CommittedStatus() == COMMITTED_TRIANGLE_HIT)
    {
        hit.object = query.CommittedInstanceID();
        hit.triangle = objects[hit.object].geometry.x / 3 + query.CommittedPrimitiveIndex();
        hit.distance = query.CommittedRayT();
        hit.bary = query.CommittedTriangleBarycentrics();
    }
    return hit;
}
bool unoccluded(Ray ray)
{
    RayQuery<RAY_FLAG_SKIP_PROCEDURAL_PRIMITIVES | RAY_FLAG_ACCEPT_FIRST_HIT_AND_END_SEARCH> query;
    traverse(query, ray);
    return query.CommittedStatus() != COMMITTED_TRIANGLE_HIT;
}
float3 trianglePoint(Hit hit)
{
    uint first = hit.triangle * 3;
    return vertices[first].position.xyz * (1 - hit.bary.x - hit.bary.y)
        + vertices[first + 1].position.xyz * hit.bary.x + vertices[first + 2].position.xyz * hit.bary.y
        + objects[hit.object].offset.xyz;
}
float3 triangleError(Vertex a, Vertex b, Vertex c, float3 weights, float3 translation)
{
    float3 magnitude = abs(weights.x * a.position.xyz) + abs(weights.y * b.position.xyz) + abs(weights.z * c.position.xyz);
    float3 edgesA = abs(b.position.xyz - a.position.xyz), edgesB = abs(c.position.xyz - a.position.xyz);
    float3 extent3 = edgesA + edgesB + abs(edgesA - edgesB);
    float extent = max(extent3.x, max(extent3.y, extent3.z));
    // Include hardware intersection and translation error, not just PBRT's
    // barycentric interpolation error. The extent term stays nonzero on a
    // plane through the origin where the normal component of |p| is zero.
    // NVIDIA: "Solving Self-Intersection Artifacts in DirectX Raytracing".
    return gammaError(7) * magnitude + gammaError(3) * extent
        + gammaError(2) * (magnitude + abs(translation));
}
Surface interaction(Hit hit, Ray ray, TextureFootprint footprint)
{
    uint first = hit.triangle * 3;
    Vertex a = vertices[first], b = vertices[first + 1], c = vertices[first + 2];
    float3 weights = float3(1 - hit.bary.x - hit.bary.y, hit.bary);
    float3 translation = objects[hit.object].offset.xyz;
    float2 uv = a.uv.xy * weights.x + b.uv.xy * weights.y + c.uv.xy * weights.z;
    float3 ng = normalize(cross(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz));
    float3 ns = normalize(a.normal.xyz * weights.x + b.normal.xyz * weights.y + c.normal.xyz * weights.z);
    if (dot(ns, ng) < 0) ns = -ns;
    float4 tangent = a.tangent * weights.x + b.tangent * weights.y + c.tangent * weights.z;
    float3 t = tangent.xyz - ns * dot(ns, tangent.xyz);
    t = dot(t, t) > 0 ? normalize(t) : tangentAxis(ns);
    Material material = materials[(uint)a.normal.w];
    float4 base = sampleTexture((int)material.textures.x, uv, footprint, true) * material.baseColor;
    float4 mr = sampleTexture((int)material.textures.z, uv, footprint, false);
    if (material.textures.y >= 0)
    {
        // Average encoded vectors over the same primary footprint as the color
        // and roughness, then normalize once. This real-time normal-map filter
        // extends PBRT's level-zero bilerp; it is not a microfacet variance fit.
        float3 mapped = sampleTexture((int)material.textures.y, uv, footprint, false).xyz * 2 - 1;
        mapped.xy *= material.parameters.y;
        float3 mappedNormal = t * mapped.x + cross(ns, t) * tangent.w * mapped.y + ns * mapped.z;
        if (dot(mappedNormal, mappedNormal) > 0) ns = normalize(mappedNormal);
        if (dot(ns, ng) < 0) ns = -ns;
    }
    // Normal maps can turn away from a grazing incident path even though the
    // triangle is visible. Keep its reflection above the actual surface so
    // the BSDF's geometric hemisphere check does not carve out dark patches.
    // This view-dependent regularization is an opaque-scene adapter, not a
    // reciprocal physical normal-map model; geometry and ray offsets stay ng.
    ns = reflectionSafeShadingNormal(ng, ns, -ray.direction);
    Surface result = (Surface)0;
    result.p = trianglePoint(hit);
    result.error = triangleError(a, b, c, weights, translation);
    result.geometricNormal = ng;
    result.normal = ns;
    result.tangent = tangentAxis(ns);
    result.albedo = saturate(base.rgb);
    result.metallic = saturate(material.emissiveMetallic.w * mr.b);
    result.roughness = saturate(material.parameters.x * mr.g);
    result.emissive = material.emissiveMetallic.rgb * sampleTexture((int)material.textures.w, uv, footprint, true).rgb;
    // PBRT area emitters are one-sided unless explicitly authored two-sided.
    if (material.parameters.w == 0 && dot(ng, -ray.direction) <= 0) result.emissive = 0;
    result.object = hit.object;
    return result;
}
Surface interaction(Hit hit, Ray ray)
{
    // No guessed camera footprint on secondary transport or light visibility.
    return interaction(hit, ray, (TextureFootprint)0);
}
