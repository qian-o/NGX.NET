// Global Reinhard-style compression, applied to peak RGB so one scalar keeps
// linear RGB ratios and fits every channel into the display gamut. This is a
// peak/gray-anchor adaptation, not the paper's luminance-based implementation.
// Reinhard et al., "Photographic Tone Reproduction for Digital Images" (2002):
// https://www-old.cs.utah.edu/docs/techreports/2002/pdf/UUCS-02-001.pdf
// Applying x/(1+x) to x=peak/(1-gray) gives peak/(1-gray+peak), making
// neutral gray=0.18 a fixed point. Above gray, a logarithmic shoulder compresses
// bright outdoor values without changing dark interiors or exposure metering.
float3 displayToneMap(float3 color)
{
    color = max(color, 0);
    float peak = max(color.r, max(color.g, color.b));
    if (peak <= 0.18) return color / (0.82 + peak);

    // q = gray + log(1 + k*(peak-gray))/k, with k=4. This form avoids
    // multiplying large HDR peaks by k. q and its first derivative join the
    // unchanged low branch at gray; q remains unbounded, with no white cap.
    float compressedPeak = 0.18 + 0.17328679514 * (log2(peak + 0.07) + 2);
    float mappedPeak = compressedPeak / (0.82 + compressedPeak);
    // Avoid a subnormal reciprocal being flushed to zero for huge finite HDR
    // inputs. Scale numerator and denominator together before the division;
    // precise prevents fast-math from cancelling this numerical safeguard.
    if (peak > 1e20)
    {
        precise float3 scaledColor = color * 1e-20;
        precise float scaledPeak = max(scaledColor.r, max(scaledColor.g, scaledColor.b));
        return (scaledColor / scaledPeak) * mappedPeak;
    }
    // No channel sum, black offset, or forced white mix.
    return (color / peak) * mappedPeak;
}
