// PBRT infinite / diffuse area-light sampling, adapted for the directional
// Hillaire atmosphere, finite solar disk and GPU triangle buffers.
// Both escaped rays and emissive hits participate in PathIntegrator MIS.
// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0; see Pbrt.LICENSE.txt.
#include "LightSampling.slang"
#include "SkySampling.slang"
struct LightSample
{
    float3 wi;
    float3 radiance;
    float pdf;
    float distance;
    bool delta;
    float3 target;
    float3 targetError;
    float3 targetNormal;
};
float sunOneMinusCosTheta()
{
    return solarOneMinusCosTheta(g.sun.w);
}
float3 sampleSolarDisk(float2 u)
{
    return basisDirection(g.sun.xyz, sampleSolarLocal(g.sun.w, u));
}
float3 solarDiskRadiance(float3 worldPosition, float3 direction)
{
    return solarRadiance(g.lighting.x,g.sun.w)*atmosphereSolarTransmittance(worldPosition,direction);
}
float3 escapedEnvironment(float3 direction, float3 worldPosition, float bsdfPdf, bool unweighted)
{
    float3 pmf=atmosphereLightPmf();
    float skyWeight=unweighted?1:powerHeuristic(bsdfPdf,pmf.x*atmosphereSkyPdf(direction));
    float3 result=skyWeight*g.lighting.x*atmosphereSkyRadiance(direction);
    if(solarDirectionInside(direction,g.sun.xyz,g.sun.w))
    {
        float sunWeight=unweighted?1:powerHeuristic(bsdfPdf,solarPdf(direction,g.sun.xyz,g.sun.w,pmf.y));
        result+=sunWeight*solarDiskRadiance(worldPosition,direction);
    }
    return result;
}
LightSample sampleLight(Surface surface, float choice, float2 u)
{
    LightSample sample = (LightSample)0;
    float3 pmf=atmosphereLightPmf();
    if(choice<pmf.x)
    {
        sample.wi=sampleAtmosphereSky(u);
        sample.radiance=g.lighting.x*atmosphereSkyRadiance(sample.wi);
        sample.pdf=pmf.x*atmosphereSkyPdf(sample.wi);
        sample.distance=rayInfinity;
        return sample;
    }
    if (choice < pmf.x+pmf.y)
    {
        sample.wi = sampleSolarDisk(u);
        sample.radiance = solarDiskRadiance(surface.p,sample.wi);
        sample.pdf = pmf.y / (2 * lightPi * sunOneMinusCosTheta());
        sample.distance = rayInfinity;
        return sample;
    }
    uint count = (uint)g.scene.x;
    if (count == 0 || pmf.z <= 0) return sample;
    choice = (choice - pmf.x-pmf.y) / pmf.z;
    uint low = 0, high = count - 1;
    while (low < high)
    {
        uint middle = (low + high) / 2;
        if (choice < areaLights[middle].cdf) high = middle;
        else low = middle + 1;
    }
    AreaLight light = areaLights[low];
    uint first = light.triangle * 3;
    Vertex a = vertices[first], b = vertices[first + 1], c = vertices[first + 2];
    float root = sqrt(u.x);
    float3 bary = float3(1 - root, root * (1 - u.y), root * u.y);
    float3 translation = objects[(uint)a.position.w].offset.xyz;
    sample.target = a.position.xyz * bary.x + b.position.xyz * bary.y + c.position.xyz * bary.z + translation;
    sample.targetNormal = normalize(cross(b.position.xyz - a.position.xyz, c.position.xyz - a.position.xyz));
    sample.targetError = triangleError(a, b, c, bary, translation);
    float3 difference = sample.target - surface.p;
    float distanceSquared = dot(difference, difference);
    if (distanceSquared == 0) return sample;
    sample.distance = sqrt(distanceSquared);
    sample.wi = difference / sample.distance;
    float cosine = dot(sample.targetNormal, -sample.wi);
    Material material = materials[(uint)a.normal.w];
    if (cosine == 0 || (cosine < 0 && material.parameters.w == 0)) return sample;
    float2 uv = a.uv.xy * bary.x + b.uv.xy * bary.y + c.uv.xy * bary.z;
    if (material.parameters.z >= 0 && material.baseColor.a * sampleTexture((int)material.textures.x, uv, 0, true).a < material.parameters.z) return sample;
    sample.radiance = material.emissiveMetallic.rgb * sampleTexture((int)material.textures.w, uv, 0, true).rgb;
    sample.pdf = pmf.z * light.pmf * distanceSquared / (abs(cosine) * light.area);
    return sample;
}
float areaLightPdf(Surface from, Hit hit)
{
    uint low = 0, high = (uint)g.scene.x;
    while (low < high)
    {
        uint middle = (low + high) / 2;
        if (areaLights[middle].triangle < hit.triangle) low = middle + 1;
        else high = middle;
    }
    if (low == (uint)g.scene.x || areaLights[low].triangle != hit.triangle) return 0;
    AreaLight light = areaLights[low];
    uint first = hit.triangle * 3;
    float3 a = vertices[first].position.xyz, b = vertices[first + 1].position.xyz, c = vertices[first + 2].position.xyz;
    float3 difference = trianglePoint(hit) - from.p;
    float distanceSquared = dot(difference, difference);
    if (distanceSquared == 0) return 0;
    float cosine = abs(dot(normalize(cross(b - a, c - a)), normalize(difference)));
    return cosine == 0 ? 0 : atmosphereLightPmf().z * light.pmf * distanceSquared / (light.area * cosine);
}
bool visibleLight(Surface surface, LightSample light)
{
    Ray ray = spawnRay(surface, light.wi);
    if (light.distance != rayInfinity)
    {
        Surface target = (Surface)0;
        target.p = light.target;
        target.error = light.targetError;
        target.geometricNormal = light.targetNormal;
        float3 difference = offsetOrigin(target, -light.wi) - ray.origin;
        float distance = length(difference);
        if (distance == 0) return false;
        ray.direction = difference / distance;
        ray.tMax = nextFloat(distance, false);
    }
    return unoccluded(ray);
}
