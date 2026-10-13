// Global display adaptation. Neither film nor NGX inputs are exposed.
// Every pixel uses the same meter, irrespective of sky, surface or reflection.
// Spatial means precede logs so sparse path samples are not individually
// treated as dark pixels. A trimmed log mean limits extreme cells' influence.
#include "ExposureMath.slang"

[shader("compute")]
[numthreads(8, 8, 1)]
void AutoExposure(uint3 thread : SV_GroupThreadID, uint index : SV_GroupIndex)
{
    float luminanceSum = 0;
    uint count = 0;
    for (uint y = 0; y < 8; y++)
        for (uint x = 0; x < 8; x++)
        {
            float2 uv = (float2(thread.xy * 8 + uint2(x, y)) + 0.5) / 64;
            float3 color = g.parameters.z > 0
                ? reconstructedImage.SampleLevel(linearSampler, uv, 0).rgb
                : sceneImage.SampleLevel(linearSampler, uv, 0).rgb;
            float luminance = dot(max(color, 0), float3(0.2126, 0.7152, 0.0722));
            if (!isfinite(luminance)) continue;
            // Only the meter is bounded; no image radiance is clipped.
            luminanceSum += min(luminance, 64);
            count++;
        }
    float mean = count > 0 ? luminanceSum / count : asfloat(0x7fc00000u);
    float2 meter = trimmedExposureLog(mean, index);
    if (index == 0)
    {
        bool initialize = g.exposure.y > 0;
        float previous = initialize ? 3 : exposureState[uint2(0)];
        if (!isfinite(previous)) previous = 3;
        float currentDaylight = atmosphereGroundIrradianceImage.Load(int3(0, 0, 0)).a;
        float noonDaylight = atmosphereGroundIrradianceImage.Load(int3(1, 0, 0)).a;
        float daylightCompensation = daylightExposureCompensation(currentDaylight, noonDaylight);
        // An entirely invalid image retains history; finite black remains valid.
        float target = meter.y > 0 ? targetExposure(meter.x, daylightCompensation) : previous;
        exposureState[uint2(0)] = initialize ? target : adaptExposure(previous, target, g.exposure.x);
    }
}
