// PBRT perspective-camera / box-filter / RGB film adaptation for NGX resources.
// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0; see Pbrt.LICENSE.txt.
#include "ReconstructionGuides.slang"
Ray cameraRay(float2 pixel, float2 offset)
{
    float2 ndc = ((pixel + 0.5 + offset) / g.size.xy) * float2(2, -2) + float2(-1, 1);
    float4 nearPoint = mul(float4(ndc, 1, 1), g.inverseViewProjection);
    nearPoint.xyz /= nearPoint.w;
    Ray ray;
    ray.origin = g.camera.xyz;
    float3 direction = nearPoint.xyz - ray.origin;
    ray.tMin = length(direction);
    ray.tMax = ray.tMin * g.scene.z / g.scene.y;
    ray.direction = direction / ray.tMin;
    return ray;
}
Surface cameraInteraction(Hit hit, Ray ray, float2 pixel, float2 offset)
{
    uint first = hit.triangle * 3;
    Vertex a = vertices[first], b = vertices[first + 1], c = vertices[first + 2];
    // One output pixel preserves detail recoverable by SR/RR. Native rendering
    // has the same input/output dimensions. Neighbor rays share the same jitter.
    float2 step = g.size.xy / g.size.zw;
    Ray rx = cameraRay(pixel + float2(step.x, 0), offset);
    Ray ry = cameraRay(pixel + float2(0, step.y), offset);
    float3 dx = (rx.direction * rx.tMin - ray.direction * ray.tMin) / ray.tMin;
    float3 dy = (ry.direction * ry.tMin - ray.direction * ray.tMin) / ray.tMin;
    // Analytic plane derivatives avoid neighboring rays crossing the horizon.
    // Only the current triangle's plane is used; no extra scene rays are traced.
    TextureFootprint footprint;
    footprint.dx = planeUvDerivative(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz,
        b.uv.xy - a.uv.xy, c.uv.xy - a.uv.xy, ray.direction, dx, hit.distance);
    footprint.dy = planeUvDerivative(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz,
        b.uv.xy - a.uv.xy, c.uv.xy - a.uv.xy, ray.direction, dy, hit.distance);
    return interaction(hit, ray, footprint);
}
float2 surfaceMotion(Surface surface)
{
    float3 previous = surface.p + objects[surface.object].previousOffset.xyz - objects[surface.object].offset.xyz;
    float4 a = mul(float4(surface.p, 1), g.currentViewProjection);
    float4 b = mul(float4(previous, 1), g.previousViewProjection);
    if (a.w <= 0 || b.w <= 0) return 0;
    return (b.xy / b.w - a.xy / a.w) * float2(0.5, -0.5) * g.size.xy;
}
void writeGuides(uint2 pixel, Ray ray, Hit hit, Surface surface)
{
    if (hit.triangle == 0xffffffff)
    {
        depthOutput[pixel] = 0;
        normalOutput[pixel] = float4(0, 0, 1, 1);
        diffuseOutput[pixel] = 0;
        specularOutput[pixel] = 0;
        float4 a = mul(float4(ray.direction, 0), g.currentViewProjection);
        float4 b = mul(float4(ray.direction, 0), g.previousViewProjection);
        motionOutput[pixel] = a.w > 0 && b.w > 0 ? (b.xy / b.w - a.xy / a.w) * float2(0.5, -0.5) * g.size.xy : 0;
        return;
    }
    float4 clip = mul(float4(surface.p, 1), g.viewProjection);
    depthOutput[pixel] = saturate(clip.z / clip.w);
    float3 normal = dot(surface.normal, -ray.direction) >= 0 ? surface.normal : -surface.normal;
    normalOutput[pixel] = float4(normal, surface.roughness);
    motionOutput[pixel] = surfaceMotion(surface);
    // Stable material guides, independent of stochastic MixMaterial selection.
    // These are NGX albedo descriptors; the path integrator evaluates the full
    // glTF dielectric / conductor BSDF and never uses these approximations.
    diffuseOutput[pixel] = float4(surface.albedo * (1 - surface.metallic), 1);
    specularOutput[pixel] = float4(reconstructionSpecularAlbedo(surface.albedo, surface.metallic,
        surface.roughness, dot(normal, -ray.direction)), 1);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void PathTrace(uint3 id : SV_DispatchThreadID)
{
    uint2 pixel = id.xy;
    if (any(pixel >= uint2(g.size.xy))) return;
    Ray centerRay = cameraRay(pixel, 0);
    Hit centerHit = intersect(centerRay);
    Surface centerSurface = (Surface)0;
    if (centerHit.triangle != 0xffffffff) centerSurface = cameraInteraction(centerHit, centerRay, pixel, 0);
    writeGuides(pixel, centerRay, centerHit, centerSurface);
    float3 batch = 0;
    float specularDistance = 0;
    bool recordedDistance = false;
    bool ngxCamera = g.lighting.w > 0;
    for (uint i = 0; i < g.integrator.x; i++)
    {
        PathSampler sampler = makeSampler(pixel, g.integrator.z + i);
        float2 offset = next2D(sampler) - 0.5;
        // Consume the same camera dimensions in both modes. NGX's frame jitter
        // already defines one shared primary ray for its samples and guides.
        Ray ray = centerRay;
        Hit hit = centerHit;
        Surface surface = centerSurface;
        if (!ngxCamera)
        {
            // Native box-filter samples remain distinct from the guide center.
            ray = cameraRay(pixel, offset);
            hit = intersect(ray);
            surface = (Surface)0;
            if (hit.triangle != 0xffffffff) surface = cameraInteraction(hit, ray, pixel, offset);
        }
        float distance;
        bool hasDistance;
        float3 radiance = pathRadiance(ray, hit, surface, sampler, distance, hasDistance);
        batch += applyAerialPerspective(radiance, ray, hit);
        if (hasDistance && !recordedDistance)
        {
            specularDistance = distance;
            recordedDistance = true;
        }
    }
    // NVIDIA's guide uses the first sampled glossy/impulse segment and FLT_MAX
    // for sky. Keep its world-space distance; do not average a miss sentinel
    // into distances or fabricate a reflected virtual-point motion vector.
    specularHitDistanceOutput[pixel] = reconstructionHitDistance(centerHit.triangle == 0xffffffff,
        recordedDistance, specularDistance);
    float3 current = batch / g.integrator.x;
    if (g.lighting.w == 0)
    {
        // Store the progressive mean in full precision. A zero history count
        // skips the uninitialized allocation after resize or a scene change.
        float total = float(g.integrator.w) + float(g.integrator.x);
        float3 mean = g.integrator.w == 0 ? current : film[pixel].rgb + (current - film[pixel].rgb) * (float(g.integrator.x) / total);
        film[pixel] = float4(mean, total);
        current = mean;
    }
    // No radiance clamping, spatial denoising or local exposure in film.
    sceneOutput[pixel] = float4(current, 1);
}
