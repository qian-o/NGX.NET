// Peak-based highlight compression, adapted from the Khronos PBR Neutral
// shoulder in three.js (MIT; see LICENSE-ToneMapping.txt).
// Keep scene-linear sRGB midtones unchanged. In particular, do not subtract
// the BRDF's dielectric reflectance: doing so would crush the dark interior.
float3 displayToneMap(float3 color)
{
    color = max(color, 0);
    const float startCompression = 0.8;
    const float highlightDesaturation = 0.15;
    float peak = max(color.r, max(color.g, color.b));
    if (peak < startCompression) return color;

    float shoulder = 1 - startCompression;
    float mappedPeak = 1 - shoulder * shoulder / (peak + shoulder - startCompression);
    color *= mappedPeak / peak;
    // Only values entering the highlight shoulder fade toward display white.
    float fade = 1 - 1 / (highlightDesaturation * (peak - mappedPeak) + 1);
    return lerp(color, float3(mappedPeak), fade);
}
