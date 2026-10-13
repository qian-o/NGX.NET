// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0
// Slang/RGB port of pbrt-v4 b4ce9687e6c695f5582997c61b0c66cf064bdb4a:
// bxdfs.h / bxdfs.cpp: DiffuseBxDF, DielectricBxDF, ConductorBxDF, LayeredBxDF
// util/scattering.h / util/vecmath.h: Fresnel, GGX and Frame
// materials.h: conductor reflectance parameterization and MixMaterial
// See Pbrt.LICENSE.txt in this directory and Showcase/README.md.
//
// PBRT scattering primitives and the retained zero-thickness LayeredBxDF port.
// GltfBsdf.slang adapts opaque glTF nonmetals to their diffuse/specular material
// model; baseColor is not a substrate underneath an implicit refractive coating.
// The caller selects dielectric/conductor once per interaction using metallic.
// Both branches use alpha=roughness^2 without PBRT roughness remapping.
#ifndef SHOWCASE_PBRT_BSDF
#define SHOWCASE_PBRT_BSDF
#include "PbrtSampling.slang"

static const float bsdfPi = 3.14159265358979323846;
static const float coatingEta = 1.5;
static const uint coatingMaxDepth = 10; // PBRT CoatedDiffuseMaterial default.
// PBRT's default nSamples=1 is retained for layered f and PDF estimates.

struct BsdfSample
{
    float3 wi;
    float3 f;
    float pdf;
    bool delta;
    bool pdfIsProportional;
    bool diffuse; // Guide classification: the selected sampling lobe is diffuse.
};
struct InterfaceSample
{
    float3 wi;
    float3 f;
    float pdf;
    bool delta;
    bool transmission;
};
struct PbrtFrame { float3 x; float3 y; float3 z; };
PbrtFrame bsdfFrame(Surface s)
{
    PbrtFrame frame;
    frame.z = normalize(s.normal);
    float3 tangent = s.tangent - dot(s.tangent, frame.z) * frame.z;
    if (dot(tangent, tangent) > 0)
    {
        frame.x = normalize(tangent);
        frame.y = cross(frame.z, frame.x);
    }
    else
    {
        // PBRT CoordinateSystem / Frame::FromZ (Duff et al.).
        float sign = frame.z.z < 0 ? -1 : 1;
        float a = -1 / (sign + frame.z.z), b = frame.z.x * frame.z.y * a;
        frame.x = float3(1 + sign * frame.z.x * frame.z.x * a, sign * b, -sign * frame.z.x);
        frame.y = float3(b, sign + frame.z.y * frame.z.y * a, -frame.z.y);
    }
    return frame;
}
float3 toLocal(PbrtFrame frame, float3 w) { return float3(dot(w, frame.x), dot(w, frame.y), dot(w, frame.z)); }
float3 fromLocal(PbrtFrame frame, float3 w) { return w.x * frame.x + w.y * frame.y + w.z * frame.z; }
float maxComponent(float3 v) { return max(v.x, max(v.y, v.z)); }
bool sameHemisphere(float3 a, float3 b) { return a.z * b.z > 0; }
bool validInterface(InterfaceSample sample) { return sample.pdf > 0 && any(sample.f > 0) && sample.wi.z != 0; }
float bsdfPowerHeuristic(float a, float b)
{
    float scale = max(a, b);
    if (scale == 0) return 0;
    a /= scale; b /= scale;
    return a * a / (a * a + b * b);
}
float2 sampleDiskConcentric(float2 u)
{
    float2 offset = 2 * u - 1;
    if (all(offset == 0)) return 0;
    float radius, theta;
    if (abs(offset.x) > abs(offset.y)) { radius = offset.x; theta = (bsdfPi / 4) * (offset.y / offset.x); }
    else { radius = offset.y; theta = bsdfPi / 2 - (bsdfPi / 4) * (offset.x / offset.y); }
    return radius * float2(cos(theta), sin(theta));
}
float3 sampleCosineHemisphere(float2 u)
{
    float2 disk = sampleDiskConcentric(u);
    return float3(disk, sqrt(max(0, 1 - dot(disk, disk))));
}
bool effectivelySmooth(float alpha) { return alpha < 1e-3; }
float trowbridgeReitzD(float nh, float alpha)
{
    float cosSquared = nh * nh;
    if (cosSquared * cosSquared < 1e-16) return 0;
    float alphaSquared = alpha * alpha;
    float denominator = max(0, 1 - cosSquared) + alphaSquared * cosSquared;
    return alphaSquared / (bsdfPi * denominator * denominator);
}
float trowbridgeReitzLambda(float cosine, float alpha)
{
    float cosSquared = cosine * cosine;
    if (cosSquared == 0) return 0;
    float tanSquared = max(0, 1 - cosSquared) / cosSquared;
    if (isinf(tanSquared)) return 0;
    return (sqrt(1 + alpha * alpha * tanSquared) - 1) / 2;
}
float trowbridgeReitzG1(float cosine, float alpha) { return 1 / (1 + trowbridgeReitzLambda(cosine, alpha)); }
float trowbridgeReitzG(float no, float ni, float alpha)
{
    return 1 / (1 + trowbridgeReitzLambda(no, alpha) + trowbridgeReitzLambda(ni, alpha));
}
float3 sampleTrowbridgeReitzWm(float3 wo, float alpha, float2 u)
{
    float3 wh = normalize(float3(alpha * wo.xy, wo.z));
    if (wh.z < 0) wh = -wh;
    float3 t1 = wh.z < 0.99999 ? normalize(cross(float3(0, 0, 1), wh)) : float3(1, 0, 0);
    float3 t2 = cross(wh, t1);
    float radius = sqrt(u.x), phi = 2 * bsdfPi * u.y;
    float2 p = radius * float2(cos(phi), sin(phi));
    p.y = lerp(sqrt(max(0, 1 - p.x * p.x)), p.y, (1 + wh.z) / 2);
    float3 nh = p.x * t1 + p.y * t2 + sqrt(max(0, 1 - dot(p, p))) * wh;
    return normalize(float3(alpha * nh.xy, max(1e-6, nh.z)));
}
float visibleNormalPdf(float3 wo, float3 wm, float alpha)
{
    return trowbridgeReitzG1(wo.z, alpha) * trowbridgeReitzD(wm.z, alpha) * abs(dot(wo, wm)) / abs(wo.z);
}
float fresnelDielectric(float cosine, float eta)
{
    cosine = clamp(cosine, -1, 1);
    if (cosine < 0) { eta = 1 / eta; cosine = -cosine; }
    float sinSquared = max(0, 1 - cosine * cosine) / (eta * eta);
    if (sinSquared >= 1) return 1;
    float transmittedCosine = sqrt(1 - sinSquared);
    float parallel = (eta * cosine - transmittedCosine) / (eta * cosine + transmittedCosine);
    float perpendicular = (cosine - eta * transmittedCosine) / (cosine + eta * transmittedCosine);
    return (parallel * parallel + perpendicular * perpendicular) / 2;
}
float2 complexMultiply(float2 a, float2 b) { return float2(a.x * b.x - a.y * b.y, a.x * b.y + a.y * b.x); }
float2 complexDivide(float2 a, float2 b)
{
    return float2(dot(a, b), a.y * b.x - a.x * b.y) / dot(b, b);
}
float2 complexSqrt(float2 value)
{
    float magnitude = length(value);
    return float2(sqrt(max(0, (magnitude + value.x) / 2)), (value.y < 0 ? -1 : 1) * sqrt(max(0, (magnitude - value.x) / 2)));
}
float fresnelConductorChannel(float reflectance, float cosine)
{
    // PBRT ConductorMaterial's reflectance input gives eta=1 and this k.
    float r = clamp(reflectance, 0, 0.9999);
    if (r == 0) return 0; // eta=1,k=0 is an index-matched interface.
    float2 eta = float2(1, 2 * sqrt(r) / sqrt(1 - r));
    cosine = saturate(cosine);
    float2 cosThetaT = complexSqrt(float2(1, 0) - complexDivide(float2(1 - cosine * cosine, 0), complexMultiply(eta, eta)));
    float2 parallel = complexDivide(eta * cosine - cosThetaT, eta * cosine + cosThetaT);
    float2 etaCosT = complexMultiply(eta, cosThetaT);
    float2 perpendicular = complexDivide(float2(cosine, 0) - etaCosT, float2(cosine, 0) + etaCosT);
    return (dot(parallel, parallel) + dot(perpendicular, perpendicular)) / 2;
}
float3 fresnelConductor(float3 reflectance, float cosine)
{
    return float3(fresnelConductorChannel(reflectance.x, cosine), fresnelConductorChannel(reflectance.y, cosine), fresnelConductorChannel(reflectance.z, cosine));
}
bool refractInterface(float3 wo, float3 normal, float eta, out float3 wi, out float etap)
{
    float cosine = dot(normal, wo);
    if (cosine < 0) { eta = 1 / eta; cosine = -cosine; normal = -normal; }
    etap = eta;
    wi = 0;
    float sinSquared = max(0, 1 - cosine * cosine) / (eta * eta);
    if (sinSquared >= 1) return false;
    wi = -wo / eta + (cosine / eta - sqrt(1 - sinSquared)) * normal;
    return true;
}

float3 diffuseF(float3 reflectance, float3 wo, float3 wi)
{
    return sameHemisphere(wo, wi) ? reflectance / bsdfPi : float3(0);
}
float diffusePdf(float3 wo, float3 wi) { return sameHemisphere(wo, wi) ? abs(wi.z) / bsdfPi : 0; }
InterfaceSample sampleDiffuseInterface(float3 reflectance, float3 wo, float2 u)
{
    InterfaceSample sample = (InterfaceSample)0;
    sample.wi = sampleCosineHemisphere(u);
    if (wo.z < 0) sample.wi.z = -sample.wi.z;
    sample.f = reflectance / bsdfPi;
    sample.pdf = abs(sample.wi.z) / bsdfPi;
    return sample;
}
float3 conductorF(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    if (!sameHemisphere(wo, wi) || effectivelySmooth(alpha)) return 0;
    float3 wm = normalize(wo + wi);
    return fresnelConductor(reflectance, abs(dot(wo, wm))) * trowbridgeReitzD(wm.z, alpha)
        * trowbridgeReitzG(wo.z, wi.z, alpha) / (4 * abs(wo.z * wi.z));
}
float conductorPdf(float alpha, float3 wo, float3 wi)
{
    if (!sameHemisphere(wo, wi) || effectivelySmooth(alpha)) return 0;
    float3 wm = normalize(wo + wi);
    if (wm.z < 0) wm = -wm;
    return visibleNormalPdf(wo, wm, alpha) / (4 * abs(dot(wo, wm)));
}
BsdfSample sampleConductor(float3 reflectance, float alpha, float3 wo, float2 u)
{
    BsdfSample sample = (BsdfSample)0;
    if (wo.z == 0) return sample;
    if (effectivelySmooth(alpha))
    {
        sample.wi = float3(-wo.xy, wo.z);
        sample.f = fresnelConductor(reflectance, abs(wo.z)) / abs(wo.z);
        sample.pdf = 1;
        sample.delta = true;
        return sample;
    }
    sample.wi = reflect(-wo, sampleTrowbridgeReitzWm(wo, alpha, u));
    if (!sameHemisphere(wo, sample.wi)) return (BsdfSample)0;
    sample.f = conductorF(reflectance, alpha, wo, sample.wi);
    sample.pdf = conductorPdf(alpha, wo, sample.wi);
    return sample;
}

// Flags 1=reflection, 2=transmission, 3=both; radiance=false is importance
// transport for the virtual-light endpoint in PBRT's layered f/PDF estimates.
float dielectricF(float alpha, float3 wo, float3 wi, bool radiance)
{
    if (effectivelySmooth(alpha) || wo.z == 0 || wi.z == 0) return 0;
    bool reflection = sameHemisphere(wo, wi);
    float etap = reflection ? 1 : (wo.z > 0 ? coatingEta : 1 / coatingEta);
    float3 wm = wo + wi * etap;
    if (dot(wm, wm) == 0) return 0;
    wm = normalize(wm);
    if (wm.z < 0) wm = -wm;
    float dotO = dot(wo, wm), dotI = dot(wi, wm);
    if (dotO * wo.z < 0 || dotI * wi.z < 0) return 0;
    float f = fresnelDielectric(dotO, coatingEta);
    float dg = trowbridgeReitzD(wm.z, alpha) * trowbridgeReitzG(wo.z, wi.z, alpha);
    if (reflection) return dg * f / abs(4 * wo.z * wi.z);
    float denominator = dotI + dotO / etap;
    float value = dg * (1 - f) * abs(dotI * dotO / (denominator * denominator * wo.z * wi.z));
    return radiance ? value / (etap * etap) : value;
}
float dielectricPdf(float alpha, float3 wo, float3 wi, uint flags)
{
    if (effectivelySmooth(alpha) || wo.z == 0 || wi.z == 0) return 0;
    bool reflection = sameHemisphere(wo, wi);
    float etap = reflection ? 1 : (wo.z > 0 ? coatingEta : 1 / coatingEta);
    float3 wm = wo + wi * etap;
    if (dot(wm, wm) == 0) return 0;
    wm = normalize(wm);
    if (wm.z < 0) wm = -wm;
    float dotO = dot(wo, wm), dotI = dot(wi, wm);
    if (dotO * wo.z < 0 || dotI * wi.z < 0) return 0;
    float r = fresnelDielectric(dotO, coatingEta);
    float pr = (flags & 1) != 0 ? r : 0, pt = (flags & 2) != 0 ? 1 - r : 0;
    if (pr + pt == 0) return 0;
    float pdf = visibleNormalPdf(wo, wm, alpha);
    if (reflection) return pdf / (4 * abs(dotO)) * pr / (pr + pt);
    float denominator = dotI + dotO / etap;
    return pdf * abs(dotI) / (denominator * denominator) * pt / (pr + pt);
}
InterfaceSample sampleDielectric(float alpha, float3 wo, float uc, float2 u, bool radiance, uint flags)
{
    InterfaceSample sample = (InterfaceSample)0;
    if (wo.z == 0) return sample;
    bool smooth = effectivelySmooth(alpha);
    float3 wm = smooth ? float3(0, 0, 1) : sampleTrowbridgeReitzWm(wo, alpha, u);
    float r = fresnelDielectric(dot(wo, wm), coatingEta);
    float pr = (flags & 1) != 0 ? r : 0, pt = (flags & 2) != 0 ? 1 - r : 0;
    if (pr + pt == 0) return sample;
    if (uc < pr / (pr + pt))
    {
        sample.wi = reflect(-wo, wm);
        if (!sameHemisphere(wo, sample.wi)) return (InterfaceSample)0;
        sample.pdf = pr / (pr + pt);
        if (smooth) sample.f = r / abs(sample.wi.z);
        else
        {
            sample.pdf *= visibleNormalPdf(wo, wm, alpha) / (4 * abs(dot(wo, wm)));
            sample.f = r * trowbridgeReitzD(wm.z, alpha) * trowbridgeReitzG(wo.z, sample.wi.z, alpha) / (4 * wo.z * sample.wi.z);
        }
    }
    else
    {
        float etap;
        if (!refractInterface(wo, wm, coatingEta, sample.wi, etap) || sameHemisphere(wo, sample.wi) || sample.wi.z == 0)
            return (InterfaceSample)0;
        sample.pdf = pt / (pr + pt);
        if (smooth) sample.f = (1 - r) / abs(sample.wi.z);
        else
        {
            float dotO = dot(wo, wm), dotI = dot(sample.wi, wm);
            float denominator = dotI + dotO / etap;
            denominator *= denominator;
            sample.pdf *= visibleNormalPdf(wo, wm, alpha) * abs(dotI) / denominator;
            sample.f = (1 - r) * trowbridgeReitzD(wm.z, alpha) * trowbridgeReitzG(wo.z, sample.wi.z, alpha)
                * abs(dotI * dotO / (wo.z * sample.wi.z * denominator));
        }
        if (radiance) sample.f /= etap * etap;
        sample.transmission = true;
    }
    sample.delta = smooth;
    return sample;
}

float3 coatedDiffuseF(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    if (!sameHemisphere(wo, wi)) return 0;
    if (wo.z < 0) { wo = -wo; wi = -wi; }
    float3 value = dielectricF(alpha, wo, wi, true);
    PbrtRng rng = makePbrtRng(pbrtHash(wo), pbrtHash(wi));
    float uc = rng1D(rng); float2 u = rng2D(rng);
    InterfaceSample wos = sampleDielectric(alpha, wo, uc, u, true, 2);
    uc = rng1D(rng); u = rng2D(rng);
    InterfaceSample wis = sampleDielectric(alpha, wi, uc, u, false, 2);
    if (!validInterface(wos) || !validInterface(wis)) return value;
    float3 beta = wos.f * abs(wos.wi.z) / wos.pdf;
    float3 w = wos.wi;
    bool atTop = true;
    [loop]
    for (uint depth = 0; depth < coatingMaxDepth; depth++)
    {
        if (depth > 3 && maxComponent(beta) < 0.25)
        {
            float survival = maxComponent(beta);
            if (rng1D(rng) >= survival) break;
            beta /= survival;
        }
        atTop = !atTop; // Zero-thickness/no-medium specialization: Tr=1.
        if (atTop)
        {
            uc = rng1D(rng); u = rng2D(rng);
            InterfaceSample reflected = sampleDielectric(alpha, -w, uc, u, true, 1);
            if (!validInterface(reflected)) break;
            beta *= reflected.f * abs(reflected.wi.z) / reflected.pdf;
            w = reflected.wi;
        }
        else
        {
            float weight = effectivelySmooth(alpha) ? 1 : bsdfPowerHeuristic(wis.pdf, diffusePdf(-w, -wis.wi));
            value += beta * diffuseF(reflectance, -w, -wis.wi) * abs(wis.wi.z) * weight * wis.f / wis.pdf;
            InterfaceSample reflected = sampleDiffuseInterface(reflectance, -w, rng2D(rng));
            if (!validInterface(reflected)) break;
            beta *= reflected.f * abs(reflected.wi.z) / reflected.pdf;
            w = reflected.wi;
            if (!effectivelySmooth(alpha))
            {
                float exitF = dielectricF(alpha, -w, wi, true);
                // Both MIS proposals are densities over the internal direction:
                // diffuse sampling and the virtual light sampled from external wi.
                // The pinned PBRT source uses the forward exit PDF here, which is
                // in external solid angle and does not complement the first weight.
                float exitPdf = dielectricPdf(alpha, wi, -w, 2);
                value += beta * exitF * bsdfPowerHeuristic(reflected.pdf, exitPdf);
            }
        }
    }
    return value;
}
float coatedDiffusePdf(float3 reflectance, float alpha, float3 wo, float3 wi)
{
    if (wo.z < 0) { wo = -wo; wi = -wi; }
    float pdf = 0;
    if (sameHemisphere(wo, wi))
    {
        pdf = dielectricPdf(alpha, wo, wi, 1);
        PbrtRng rng = makePbrtRng(pbrtHash(wi), pbrtHash(wo));
        float uc = rng1D(rng); float2 u = rng2D(rng);
        InterfaceSample wos = sampleDielectric(alpha, wo, uc, u, true, 2);
        uc = rng1D(rng); u = rng2D(rng);
        InterfaceSample wis = sampleDielectric(alpha, wi, uc, u, false, 2);
        if (validInterface(wos) && validInterface(wis))
        {
            if (effectivelySmooth(alpha)) pdf += diffusePdf(-wos.wi, -wis.wi);
            else
            {
                InterfaceSample rs = sampleDiffuseInterface(reflectance, -wos.wi, rng2D(rng));
                if (validInterface(rs))
                {
                    float rPdf = diffusePdf(-wos.wi, -wis.wi);
                    pdf += bsdfPowerHeuristic(wis.pdf, rPdf) * rPdf;
                    float tPdf = dielectricPdf(alpha, -rs.wi, wi, 3);
                    pdf += bsdfPowerHeuristic(rs.pdf, tPdf) * tPdf;
                }
            }
        }
    }
    // PBRT's LayeredBxDF PDF is an estimate, with this documented uniform
    // mixture. It is not the extended random-walk probability returned by Sample_f.
    return lerp(1 / (4 * bsdfPi), pdf, 0.9);
}
BsdfSample sampleCoatedDiffuse(float3 reflectance, float alpha, float3 wo, float uc, float2 u)
{
    bool flip = wo.z < 0;
    if (flip) wo = -wo;
    BsdfSample result = (BsdfSample)0;
    InterfaceSample sample = sampleDielectric(alpha, wo, uc, u, true, 3);
    if (!validInterface(sample)) return result;
    if (!sample.transmission)
    {
        result.wi = flip ? -sample.wi : sample.wi;
        result.f = sample.f;
        result.pdf = sample.pdf;
        result.delta = sample.delta;
        result.pdfIsProportional = true;
        return result;
    }
    PbrtRng rng = makePbrtRng(pbrtHash(wo), pbrtHash(float3(uc, u)));
    float3 f = sample.f * abs(sample.wi.z), w = sample.wi;
    float pdf = sample.pdf;
    bool atTop = true, specularPath = sample.delta;
    [loop]
    for (uint depth = 0; depth < coatingMaxDepth; depth++)
    {
        float rrBeta = maxComponent(f) / pdf;
        if (depth > 3 && rrBeta < 0.25)
        {
            if (rng1D(rng) >= rrBeta) return (BsdfSample)0;
            pdf *= rrBeta;
        }
        if (w.z == 0) return (BsdfSample)0;
        atTop = !atTop;
        uc = rng1D(rng); u = rng2D(rng);
        if (atTop) sample = sampleDielectric(alpha, -w, uc, u, true, 3);
        else sample = sampleDiffuseInterface(reflectance, -w, u);
        if (!validInterface(sample)) return (BsdfSample)0;
        f *= sample.f;
        pdf *= sample.pdf;
        specularPath = specularPath && sample.delta;
        w = sample.wi;
        if (sample.transmission)
        {
            result.wi = flip ? -w : w;
            result.f = f;
            result.pdf = pdf;
            result.delta = specularPath;
            result.pdfIsProportional = true;
            result.diffuse = true;
            return result;
        }
        f *= abs(w.z);
    }
    return (BsdfSample)0;
}

#include "GltfBsdf.slang"

bool hasNonSpecular(Surface s)
{
    return !effectivelySmooth(s.roughness * s.roughness) || (!s.conductor && any(s.albedo > 0));
}
bool reflectsGeometrically(Surface s, float3 wo, float3 wi)
{
    return dot(s.geometricNormal, wo) * dot(s.geometricNormal, wi) > 0;
}
float3 evaluateBsdf(Surface s, float3 wo, float3 wi)
{
    if (!reflectsGeometrically(s, wo, wi)) return 0;
    PbrtFrame frame = bsdfFrame(s);
    wo = toLocal(frame, wo); wi = toLocal(frame, wi);
    if (wo.z == 0 || wi.z == 0) return 0;
    float alpha = s.roughness * s.roughness;
    if (s.conductor) return conductorF(s.albedo, alpha, wo, wi);
    return gltfDielectricF(s.albedo, alpha, wo, wi);
}
float bsdfPdf(Surface s, float3 wo, float3 wi)
{
    if (!reflectsGeometrically(s, wo, wi)) return 0;
    PbrtFrame frame = bsdfFrame(s);
    wo = toLocal(frame, wo); wi = toLocal(frame, wi);
    if (wo.z == 0 || wi.z == 0) return 0;
    float alpha = s.roughness * s.roughness;
    if (s.conductor) return conductorPdf(alpha, wo, wi);
    return gltfDielectricPdf(s.albedo, alpha, wo, wi);
}
BsdfSample sampleBsdf(Surface s, float3 wo, float uc, float2 u)
{
    PbrtFrame frame = bsdfFrame(s);
    float3 localWo = toLocal(frame, wo);
    if (localWo.z == 0) return (BsdfSample)0;
    float alpha = s.roughness * s.roughness;
    BsdfSample sample;
    if (s.conductor) sample = sampleConductor(s.albedo, alpha, localWo, u);
    else sample = sampleGltfDielectric(s.albedo, alpha, localWo, uc, u);
    if (sample.pdf <= 0 || !any(sample.f > 0) || sample.wi.z == 0) return (BsdfSample)0;
    sample.wi = fromLocal(frame, sample.wi);
    if (!reflectsGeometrically(s, wo, sample.wi)) return (BsdfSample)0;
    return sample;
}
#endif
