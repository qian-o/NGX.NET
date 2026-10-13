// Scene texture storage and glTF sRGB decoding. Shared by all traced interactions.
#include "TextureFootprint.slang"
float4 fetch(uint offset, int2 p, uint2 size, bool srgb)
{
    // sampleMip's frac(uv) guarantees each bilinear corner is in [-1, size].
    // Wrap that single boundary without two integer divisions per coordinate.
    p = select(p < 0, p + int2(size), p);
    p = select(p >= int2(size), p - int2(size), p);
    uint packed = texels[offset + p.y * size.x + p.x];
    uint3 rgb = uint3(packed & 255, (packed >> 8) & 255, (packed >> 16) & 255);
    // Scene.Load stores the exact 8-bit sRGB decode table in the buffer prefix.
    float3 linear = srgb ? asfloat(uint3(texels[rgb.x], texels[rgb.y], texels[rgb.z])) : float3(rgb) / 255.0;
    return float4(linear, (packed >> 24) / 255.0);
}
float4 sampleMip(uint offset, uint2 size, float2 uv, bool srgb)
{
    float2 p = frac(uv) * size - 0.5;
    int2 pixel = int2(floor(p));
    float2 f = frac(p);
    float4 a = fetch(offset, pixel, size, srgb), b = fetch(offset, pixel + int2(1, 0), size, srgb);
    float4 c = fetch(offset, pixel + int2(0, 1), size, srgb), d = fetch(offset, pixel + 1, size, srgb);
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
float4 sampleTexture(int index, float2 uv, float lod, bool srgb)
{
    if (index < 0) return 1;
    uint4 info = textureInfo[index];
    float mip = clamp(lod, 0, float(info.w - 1));
    uint level = (uint)mip;
    uint2 size = info.yz;
    uint offset = info.x;
    for (uint i = 0; i < level; i++) { offset += size.x * size.y; size = max(size / 2, 1); }
    float4 color = sampleMip(offset, size, uv, srgb);
    if (level + 1 >= info.w || frac(mip) == 0) return color;
    return lerp(color, sampleMip(offset + size.x * size.y, max(size / 2, 1), uv, srgb), frac(mip));
}
float4 sampleTexture(int index, float2 uv, TextureFootprint footprint, bool srgb)
{
    if (index < 0) return 1;
    TextureFilter filter = textureFilter(footprint, textureInfo[index].yz);
    if (filter.taps == 1) return sampleTexture(index, uv, filter.lod, srgb);
    float4 value = 0;
    for (uint tap = 0; tap < filter.taps; tap++)
    {
        float offset = (float(tap) + 0.5) / filter.taps - 0.5;
        value += sampleTexture(index, uv + offset * filter.major, filter.lod, srgb);
    }
    return value / filter.taps;
}
