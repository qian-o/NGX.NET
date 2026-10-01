// Area-to-solid-angle sampling of the scene's upper bounding rectangle guides
// rays toward the atrium opening. The rectangle emits no light: every sample
// still traces full scene visibility before evaluating the existing sky.
float skyPortalPdf(float3 p, float3 direction)
{
    float height = g.environmentMaximum.y - p.y;
    float2 extent = g.environmentMaximum.xz - g.environmentMinimum.xz;
    if (height <= g.scene.z || direction.y <= 0 || any(extent <= 0)) return 0;
    float distance = height / direction.y;
    float2 point = (p + direction * distance).xz;
    if (any(point < g.environmentMinimum.xz) || any(point > g.environmentMaximum.xz)) return 0;
    return distance * distance / (extent.x * extent.y * direction.y);
}
float3 sampleSkyPortal(float3 p, float2 u, out float pdf)
{
    float3 point = float3(lerp(g.environmentMinimum.x, g.environmentMaximum.x, u.x), g.environmentMaximum.y, lerp(g.environmentMinimum.z, g.environmentMaximum.z, u.y));
    float3 delta = point - p;
    float lengthSquared = dot(delta, delta);
    float3 direction = lengthSquared > 0 ? delta * rsqrt(lengthSquared) : float3(0, 1, 0);
    pdf = skyPortalPdf(p, direction);
    return direction;
}
float powerHeuristic(float a, float b)
{
    float scale = max(a, b);
    if (scale == 0) return 1;
    a /= scale; b /= scale;
    return a * a / (a * a + b * b);
}
float specularProbability(Surface s)
{
    if (s.metallic == 1) return 1;
    float3 f0 = lerp(0.04, s.albedo, s.metallic);
    return clamp(max(f0.r, max(f0.g, f0.b)), 0.05, 0.95);
}
float2 bsdfPdfs(Surface s, float3 view, float3 direction)
{
    float nl = dot(s.normal, direction);
    if (!aboveSurface(s, direction)) return 0;
    float nv = max(dot(s.normal, view), 1e-4);
    float3 h = normalize(view + direction);
    float alpha = s.roughness * s.roughness;
    return float2(nl / 3.14159265, ggxDistribution(saturate(dot(s.normal, h)), alpha) * smithG1(nv, alpha) / (4 * nv));
}
#if HAS_RAY_QUERY
float3 skyLighting(Surface s, float3 p, float3 view, float2 u, bool separateLobes)
{
    float lightPdf;
    float3 direction = sampleSkyPortal(p, u, lightPdf);
    float nl = dot(s.normal, direction);
    if (lightPdf <= 0 || !aboveShadingSurface(s, direction)) return 0;
    if (occluded(shadowOrigin(p, s), direction, g.scene.w * 4)) return 0;
    float2 pdf = bsdfPdfs(s, view, direction);
    float3 contribution;
    if (separateLobes)
    {
        // The primary surface samples diffuse and specular independently. Weight
        // each lobe against the light technique to avoid counting the sky twice.
        float nv = max(dot(s.normal, view), 1e-4);
        float3 h = normalize(view + direction);
        float3 f = fresnel(lerp(0.04, s.albedo, s.metallic), dot(view, h));
        float alpha = s.roughness * s.roughness;
        float3 diffuse = (1 - f) * (1 - s.metallic) * s.albedo * nl / 3.14159265;
        float3 specular = f * ggxDistribution(saturate(dot(s.normal, h)), alpha) * smithVisibility(nv, nl, alpha) * nl;
        contribution = (diffuse * powerHeuristic(lightPdf, pdf.x) + specular * powerHeuristic(lightPdf, pdf.y)) * shadowTerm(s, direction);
    }
    else
    {
        float pathPdf = lerp(pdf.x, pdf.y, specularProbability(s));
        contribution = brdf(s, view, direction) * powerHeuristic(lightPdf, pathPdf);
    }
    return contribution * sky(direction) / lightPdf;
}
#endif
