// AgX default view, adapted from three.js (MIT; see LICENSE-AgX.txt).
// The published matrices and curve coefficients are retained; no contrast look
// or saturation adjustment is applied. Input and output are linear sRGB.
float3 agxContrast(float3 x)
{
    float3 x2 = x * x, x4 = x2 * x2;
    return 15.5 * x4 * x2 - 40.14 * x4 * x + 31.96 * x4
        - 6.868 * x2 * x + 0.4298 * x2 + 0.1191 * x - 0.00232;
}

float3 agxToneMap(float3 color)
{
    // Explicit rows: GLSL's column constructors are transposed for Slang mul(M, v).
    const float3x3 srgbToRec2020 = float3x3(
        0.6274, 0.3293, 0.0433,
        0.0691, 0.9195, 0.0113,
        0.0164, 0.0880, 0.8956);
    const float3x3 inset = float3x3(
        0.856627153315983, 0.0951212405381588, 0.0482516061458583,
        0.137318972929847, 0.761241990602591, 0.101439036467562,
        0.11189821299995, 0.0767994186031903, 0.811302368396859);
    const float3x3 outset = float3x3(
        1.1271005818144368, -0.11060664309660323, -0.016493938717834573,
        -0.1413297634984383, 1.157823702216272, -0.016493938717834257,
        -0.14132976349843826, -0.11060664309660294, 1.2519364065950405);
    const float3x3 rec2020ToSrgb = float3x3(
        1.6605, -0.5876, -0.0728,
        -0.1246, 1.1329, -0.0083,
        -0.0182, -0.1006, 1.1187);
    // AgX's standard 16.5-stop input range around 18% middle gray.
    const float minEv = -12.47393, maxEv = 4.026069;
    color = mul(inset, mul(srgbToRec2020, max(color, 0)));
    color = saturate((log2(max(color, 1e-10)) - minEv) / (maxEv - minEv));
    color = mul(outset, agxContrast(color));
    color = pow(max(color, 0), 2.2);
    return saturate(mul(rec2020ToSrgb, color));
}
