// Primary-ray texture footprints; secondary transport retains point footprints.
#ifndef SHOWCASE_TEXTURE_FOOTPRINT
#define SHOWCASE_TEXTURE_FOOTPRINT
struct TextureFootprint { float2 dx; float2 dy; };
struct TextureFilter { float2 major; float lod; uint taps; };

float2 planeUvDerivative(float3 edge1, float3 edge2, float2 uv1, float2 uv2,
    float3 direction, float3 directionDelta, float distance)
{
    // Scale before forming cross products, retaining tiny and large triangles.
    float scale = max(max(abs(edge1.x), max(abs(edge1.y), abs(edge1.z))),
        max(abs(edge2.x), max(abs(edge2.y), abs(edge2.z))));
    if (scale == 0) return 0;
    edge1 /= scale; edge2 /= scale;
    float3 area = cross(edge1, edge2);
    float areaSquared = dot(area, area);
    if (areaSquared == 0) return 0;
    float3 normal = area * rsqrt(areaSquared);
    float cosine = dot(normal, direction);
    // A grazing footprint already covers the coarsest mip. Bound the singular
    // tangent-plane derivative rather than letting NaNs reach atlas addressing.
    cosine = (cosine < 0 ? -1 : 1) * max(abs(cosine), 1e-7);
    float3 dp = (distance / scale) *
        (directionDelta - direction * (dot(normal, directionDelta) / cosine));
    float2 baryDelta = float2(dot(dp, cross(edge2, area)), dot(dp, cross(area, edge1))) / areaSquared;
    float2 derivative = uv1 * baryDelta.x + uv2 * baryDelta.y;
    return select(isfinite(derivative), clamp(derivative, -1e8, 1e8), float2(0));
}

TextureFilter textureFilter(TextureFootprint footprint, uint2 dimensions)
{
    TextureFilter filter = (TextureFilter)0;
    filter.taps = 1;
    float2 dx = footprint.dx * dimensions, dy = footprint.dy * dimensions;
    float scale = max(max(abs(dx.x), abs(dx.y)), max(abs(dy.x), abs(dy.y)));
    if (scale == 0 || !isfinite(scale)) return filter;
    dx /= scale; dy /= scale;
    // Principal axes of the pixel ellipse in texel space. The determinant form
    // preserves the minor axis when grazing anisotropy would cause cancellation
    // in the usual trace-minus-root formula.
    float a = dx.x * dx.x + dy.x * dy.x;
    float b = dx.x * dx.y + dy.x * dy.y;
    float c = dx.y * dx.y + dy.y * dy.y;
    float eigenvalue = 0.5 * (a + c + sqrt((a - c) * (a - c) + 4 * b * b));
    float root = sqrt(eigenvalue);
    float major = scale * root;
    if (major <= 1) return filter; // Preserve magnified level-zero detail.
    float minor = scale * abs(dx.x * dy.y - dx.y * dy.x) / root;
    float width = max(1, max(minor, major / 4));
    filter.lod = log2(width);
    // Avoid a spurious second tap from roundoff on an isotropic footprint.
    filter.taps = max(1u, min(4u, (uint)ceil(major / width - 1e-4)));
    float2 axisA = float2(b, eigenvalue - a), axisB = float2(eigenvalue - c, b);
    float2 axis = dot(axisA, axisA) > dot(axisB, axisB) ? axisA : axisB;
    if (dot(axis, axis) == 0) axis = float2(1, 0);
    filter.major = normalize(axis) * major / dimensions;
    return filter;
}
#endif
