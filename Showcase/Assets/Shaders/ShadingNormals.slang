// Normal-map regularization for opaque, reflection-only surfaces.
#ifndef SHOWCASE_SHADING_NORMALS
#define SHOWCASE_SHADING_NORMALS

float3 reflectionSafeShadingNormal(float3 ng, float3 ns, float3 wo)
{
    // Work on the incident side, retaining the authored orientation on return.
    // Simply flipping ns toward wo does not repair a lobe below the geometry.
    float side = dot(ng, wo) >= 0 ? 1 : -1;
    ng *= side;
    ns *= side;
    float geometricCosine = dot(ng, wo);
    if (geometricCosine <= 0) return ng * side;

    float shadingCosine = dot(ns, wo), normalCosine = dot(ng, ns);
    // Keep already valid normals exactly, with a small relative clearance at
    // the horizon to absorb rounding of the corrected reflection direction.
    float clearance = 0.01 * geometricCosine;
    if (2 * shadingCosine * normalCosine - geometricCosine >= clearance)
        return ns * side;

    // Rotate toward ng: n = normalize(ns + t * ng). Solve
    // dot(2 * dot(n, wo) * n - wo, ng) = clearance for positive t.
    // The constant term is negative here, so there is exactly one positive
    // root. Select the stable quadratic form to avoid subtracting near equals.
    float a = geometricCosine - clearance;
    float b = shadingCosine - clearance * normalCosine;
    float c = 2 * shadingCosine * normalCosine - geometricCosine - clearance;
    float root = sqrt(max(0, b * b - a * c));
    float t = b >= 0 ? -c / (b + root) : (root - b) / a;
    return normalize(ns + t * ng) * side;
}

#endif
