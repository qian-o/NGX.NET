// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// The pbrt source code is licensed under the Apache License, Version 2.0.
// SPDX-License-Identifier: Apache-2.0
// See the accompanying Pbrt.LICENSE.txt.
//
// Slang port of PathIntegrator::Li and SampleLd at
// b4ce9687e6c695f5582997c61b0c66cf064bdb4a, src/pbrt/cpu/integrators.cpp.
// Camera rays, surface scattering and light visibility use the same scene.
// Local surface transport is RGB and reflection-only; PBRT's etaScale is one.
// The Hillaire atmosphere supplies infinite-light radiance and primary-camera
// aerial perspective separately. Material selection happens once per hit.
#ifndef SHOWCASE_PBRT_PATH_TRACING
#define SHOWCASE_PBRT_PATH_TRACING

float3 directRadiance(Surface surface, float3 wo, inout PathSampler sampler)
{
    float lightChoice = next1D(sampler);
    float2 lightU = next2D(sampler);
    LightSample light = sampleLight(surface, lightChoice, lightU);
    if (light.pdf <= 0 || !any(light.radiance > 0)) return 0;

    float3 f = evaluateBsdf(surface, wo, light.wi) * abs(dot(surface.normal, light.wi));
    if (!any(f > 0) || !visibleLight(surface, light)) return 0;
    float weight = light.delta ? 1 : powerHeuristic(light.pdf, bsdfPdf(surface, wo, light.wi));
    // sampleLight's PDF includes the light-selection probability.
    return weight * light.radiance * f / light.pdf;
}

float3 pathRadiance(Ray ray, Hit hit, Surface surface, inout PathSampler sampler, out float specularDistance, out bool hasSpecularDistance)
{
    specularDistance = rayInfinity;
    hasSpecularDistance = false;
    float3 radiance = 0;
    float3 beta = 1;
    uint depth = 0;
    uint maxDepth = (uint)g.integrator.y;
    float previousBsdfPdf = 0;
    bool specularBounce = false;
    Surface previousSurface = (Surface)0;

    [loop]
    while (true)
    {
        // The camera stage supplies the primary intersection/material. Only
        // rays spawned by later scattering events need another scene query.
        if (depth > 0) hit = intersect(ray);
        // RR's guide is the first sampled glossy/specular segment, measured
        // from the primary surface rather than its error-offset ray origin.
        // A reflected environment keeps the format's finite maximum sentinel.
        if (depth == 1 && hasSpecularDistance && hit.triangle != 0xffffffff)
            specularDistance = length(trianglePoint(hit) - previousSurface.p);
        if (hit.triangle == 0xffffffff)
        {
            radiance += beta * escapedEnvironment(ray.direction,
                depth==0?g.camera.xyz:previousSurface.p,previousBsdfPdf,depth==0||specularBounce);
            break;
        }

        if (depth > 0) surface = interaction(hit, ray);
        if (any(surface.emissive > 0))
        {
            float weight = depth == 0 || specularBounce ? 1
                : powerHeuristic(previousBsdfPdf, areaLightPdf(previousSurface, hit));
            radiance += beta * surface.emissive * weight;
        }
        // Resolve emitted light before the scattering limit, including rays
        // launched by the final allowed BSDF event.
        if (depth == maxDepth) break;
        depth++;

        // PBRT MixMaterial selects a material before its BSDF is constructed.
        // NEE and continuation both condition on this same material selection.
        surface.conductor = next1D(sampler) < surface.metallic;
        float3 wo = -ray.direction;
        if (hasNonSpecular(surface))
            radiance += beta * directRadiance(surface, wo, sampler);

        float component = next1D(sampler);
        float2 u = next2D(sampler);
        BsdfSample scattering = sampleBsdf(surface, wo, component, u);
        if (scattering.pdf <= 0 || !any(scattering.f > 0)) break;
        if (depth == 1) hasSpecularDistance = !scattering.diffuse;

        beta *= scattering.f * abs(dot(surface.normal, scattering.wi)) / scattering.pdf;
        // Layered BSDF samples may provide a proportional sampling density.
        // Its f/pdf is the throughput weight; MIS requires the evaluated PDF.
        previousBsdfPdf = scattering.pdfIsProportional
            ? bsdfPdf(surface, wo, scattering.wi) : scattering.pdf;
        specularBounce = scattering.delta;
        previousSurface = surface;
        ray = spawnRay(surface, scattering.wi);

        float maximumBeta = max(beta.r, max(beta.g, beta.b));
        // Do not roulette away the first few dim indoor bounces. At low sample
        // counts early termination turns their energy into rare bright
        // samples that reconstruction cannot reliably retain. The finite depth
        // budget still applies; longer paths use compensated roulette.
        if (maximumBeta < 1 && depth > 5)
        {
            float termination = max(0, 1 - maximumBeta);
            if (next1D(sampler) < termination) break;
            beta /= 1 - termination;
        }
    }
    return radiance;
}

#endif
