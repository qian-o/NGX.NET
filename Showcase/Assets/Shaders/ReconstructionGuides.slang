// DLSS-RR material guides; these values never enter path throughput.
// EnvBRDFApprox2 adapted from nvpro-samples/vk_gltf_renderer/shaders/dlss_util.h:
// Copyright (c) 2023-2026 NVIDIA CORPORATION. All rights reserved.
// SPDX-License-Identifier: Apache-2.0; see Pbrt.LICENSE.txt for the license text.
// https://github.com/nvpro-samples/vk_gltf_renderer/blob/master/shaders/dlss_util.h
// Modification: scalar polynomial expansion and bounded guide output below.
#ifndef SHOWCASE_RECONSTRUCTION_GUIDES
#define SHOWCASE_RECONSTRUCTION_GUIDES

float3 reconstructionConductorAlbedo(float3 reflectance, float alpha, float cosine)
{
    if (effectivelySmooth(alpha)) return fresnelConductor(reflectance, cosine);
    // NVIDIA DLSS-RR Integration Guide v310.9.1, Appendix (page 36),
    // EnvBRDFApprox2, based on Ray Tracing Gems chapter 32.
    // Scalar expansion of the published rational fit. This approximates the
    // integrated rough-conductor guide; transport retains exact PBRT Fresnel.
    float c2 = cosine * cosine, c3 = c2 * cosine;
    float a2 = alpha * alpha, a3 = a2 * alpha;
    float bias = (0.99044 - 1.28514 * cosine + alpha * (1.29678 - 0.755907 * cosine))
        / (1 + 2.92338 * cosine + 59.4188 * c3
            + alpha * (20.3225 - 27.0302 * cosine + 222.592 * c3)
            + a3 * (121.563 + 626.13 * cosine + 316.627 * c3));
    float scale = (0.0365463 + 3.32707 * cosine + alpha * (9.0632 - 9.04756 * cosine))
        / (1 + 3.59685 * c2 - 1.36772 * c3
            + a2 * (9.04401 - 16.3174 * c2 + 9.22949 * c3)
            + a3 * (5.56589 + 19.7886 * c2 - 20.2123 * c3));
    bias *= saturate(reflectance.g * 50);
    return saturate(reflectance * max(0, scale) + max(0, bias));
}

float3 reconstructionSpecularAlbedo(float3 reflectance, float metallic, float roughness, float cosine)
{
    float alpha = roughness * roughness;
    cosine = saturate(abs(cosine));
    // RR expects average specular reflectivity at the current view direction,
    // not normal-incidence F0. Reuse the dielectric lobe's measured integral.
    float dielectric = gltfDirectionalAlbedo(alpha, cosine);
    if (metallic == 0) return float3(dielectric);
    return lerp(float3(dielectric), reconstructionConductorAlbedo(reflectance, alpha, cosine), metallic);
}

float reconstructionHitDistance(bool primarySky, bool hasSpecularDistance, float distance)
{
    // DLSS-RR v310.9.1 section 3.4.9: sky pixels use FP32_MAX. A surface
    // without a sampled specular segment retains the no-distance value zero.
    return primarySky ? 3.402823466e38 : (hasSpecularDistance ? distance : 0);
}
#endif
