// Opaque glTF metallic-roughness dielectric adapter over PBRT primitives.
// The metal branch remains PBRT ConductorBxDF. This adapter is not PBRT's
// CoatedDiffuseBxDF: glTF baseColor describes the reflected diffuse color,
// not a substrate inside a second, refractive interface.
// https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.html#dielectrics
#ifndef SHOWCASE_GLTF_BSDF
#define SHOWCASE_GLTF_BSDF
#include "GltfDirectionalAlbedo.slang"

float gltfAlbedoValue(uint index)
{
    return asfloat(texels[gltfAlbedoOffset + index]);
}
float gltfAlbedoAverageValue(uint index)
{
    return asfloat(texels[gltfAlbedoOffset + gltfAlbedoAlphaCount * gltfAlbedoCosineCount + index]);
}

// Kelemen and Szirmay-Kalos (2001), section 2.2: couple a reciprocal
// specular lobe to diffuse using its directional hemispherical reflectance E.
// Unlike Fresnel at the macro normal, E includes the microfacet roughness.
// f_d = rho/pi * (1-E(wo)) * (1-E(wi)) / (1-E_average).
// With a flat shading frame a white material returns approximately unit energy
// at every viewing angle (within the table's quadrature/interpolation error);
// colored diffuse reflectance still absorbs its own share of the energy.
// https://doi.org/10.2312/egs.20011003
float gltfDirectionalAlbedo(float alpha, float cosine)
{
    if (effectivelySmooth(alpha)) return fresnelDielectric(abs(cosine), coatingEta);
    float a = saturate(log2(alpha / 0.001) / log2(1000.0)) * (gltfAlbedoAlphaCount - 1);
    float c = saturate(log2(1 + 1000 * abs(cosine)) / log2(1001.0)) * (gltfAlbedoCosineCount - 1);
    uint ai = min(uint(a), gltfAlbedoAlphaCount - 2);
    uint ci = min(uint(c), gltfAlbedoCosineCount - 2);
    uint offset = ai * gltfAlbedoCosineCount + ci;
    float first = lerp(gltfAlbedoValue(offset), gltfAlbedoValue(offset + 1), c - ci);
    float second = lerp(gltfAlbedoValue(offset + gltfAlbedoCosineCount), gltfAlbedoValue(offset + gltfAlbedoCosineCount + 1), c - ci);
    return lerp(first, second, a - ai);
}
float gltfAverageAlbedo(float alpha)
{
    if (effectivelySmooth(alpha)) return gltfSmoothAverageAlbedo;
    float a = saturate(log2(alpha / 0.001) / log2(1000.0)) * (gltfAlbedoAlphaCount - 1);
    uint ai = min(uint(a), gltfAlbedoAlphaCount - 2);
    return lerp(gltfAlbedoAverageValue(ai), gltfAlbedoAverageValue(ai + 1), a - ai);
}
float3 gltfDiffuseF(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    float outgoing = gltfDirectionalAlbedo(alpha, wo.z);
    float incoming = gltfDirectionalAlbedo(alpha, wi.z);
    return (1 - outgoing) * (1 - incoming) / (1 - gltfAverageAlbedo(alpha)) * diffuseF(reflectance, wo, wi);
}

// A proposal probability, not a BRDF weight. Keep both proposals supported
// when a diffuse lobe exists; bounds only affect sampling variance.
float gltfReflectionProbability(float3 reflectance, float reflection)
{
    float diffuse = (1 - reflection) * maxComponent(reflectance);
    return diffuse == 0 ? 1 : clamp(reflection / (reflection + diffuse), 0.05, 0.95);
}
float gltfReflectionProbability(float3 reflectance, float alpha, float3 wo)
{
    return gltfReflectionProbability(reflectance, gltfDirectionalAlbedo(alpha, wo.z));
}
float3 gltfDielectricF(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    if (!sameHemisphere(wo, wi)) return 0;
    // Opaque back faces use the same external dielectric as front faces.
    if (wo.z < 0) { wo = -wo; wi = -wi; }
    // dielectricF already includes Fresnel and is zero for a delta lobe.
    return gltfDiffuseF(reflectance, alpha, wo, wi) + dielectricF(alpha, wo, wi, true);
}
float gltfDielectricPdf(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    if (!sameHemisphere(wo, wi)) return 0;
    if (wo.z < 0) { wo = -wo; wi = -wi; }
    float probability = gltfReflectionProbability(reflectance, alpha, wo);
    // Reflection-only is a conditional density, without another Fresnel PMF.
    // In the smooth limit dielectricPdf is zero: this function describes only
    // the continuous density, never the discrete mirror-event probability.
    return probability * dielectricPdf(alpha, wo, wi, 1)
        + (1 - probability) * diffusePdf(wo, wi);
}
BsdfSample sampleGltfDielectric(float3 reflectance, float alpha, float3 wo, float uc, float2 u)
{
    BsdfSample result = (BsdfSample)0;
    if (wo.z == 0) return result;
    bool flip = wo.z < 0;
    if (flip) wo = -wo;
    float outgoingAlbedo = gltfDirectionalAlbedo(alpha, wo.z);
    float probability = gltfReflectionProbability(reflectance, outgoingAlbedo);
    bool reflection = uc < probability;
    InterfaceSample sample;
    if (reflection)
        sample = sampleDielectric(alpha, wo, uc / probability, u, true, 1);
    else
        sample = sampleDiffuseInterface(reflectance, wo, u);
    if (!validInterface(sample)) return result;

    result.wi = sample.wi;
    result.diffuse = !reflection;
    result.delta = reflection && sample.delta;
    if (result.delta)
    {
        // A discrete mirror event has no continuous diffuse contribution.
        // Its f/pdf gives F/probability after the integrator's cosine factor.
        result.f = sample.f;
        result.pdf = probability;
    }
    else
    {
        // Both proposals represent the same aggregate continuous BSDF.
        // Reuse the outgoing lookup and lobe probability already computed
        // above; reevaluating the public F/PDF here repeats buffer reads.
        float incomingAlbedo = gltfDirectionalAlbedo(alpha, sample.wi.z);
        result.f = (1 - outgoingAlbedo) * (1 - incomingAlbedo) / (1 - gltfAverageAlbedo(alpha))
            * diffuseF(reflectance, wo, sample.wi) + dielectricF(alpha, wo, sample.wi, true);
        result.pdf = probability * dielectricPdf(alpha, wo, sample.wi, 1)
            + (1 - probability) * diffusePdf(wo, sample.wi);
    }
    // Exact aggregate density: the integrator need not estimate a layered PDF.
    result.pdfIsProportional = false;
    if (flip) result.wi = -result.wi;
    return result;
}
#endif
