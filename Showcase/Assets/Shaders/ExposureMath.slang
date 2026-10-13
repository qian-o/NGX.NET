// Display exposure in stops of gain, not photographic EV100.
groupshared float exposureCellLogs[64];
float2 trimmedExposureLog(float meanLuminance, uint index)
{
    // All finite cells, including black ones, participate identically. Invalid
    // cells sort after the data; they do not become artificial dark samples.
    exposureCellLogs[index] = isfinite(meanLuminance) && meanLuminance >= 0
        ? log2(max(meanLuminance, 1e-6)) : asfloat(0x7f800000u);
    GroupMemoryBarrierWithGroupSync();
    // Bitonic sort. Each lane owns its output; synchronize between reading a
    // partner and writing so no lane can observe values from the next stage.
    for (uint block = 2; block <= 64; block <<= 1)
        for (uint stride = block >> 1; stride > 0; stride >>= 1)
        {
            float own = exposureCellLogs[index], other = exposureCellLogs[index ^ stride];
            bool lower = (index & stride) == 0, ascending = (index & block) == 0;
            float ordered = lower == ascending ? min(own, other) : max(own, other);
            GroupMemoryBarrierWithGroupSync();
            exposureCellLogs[index] = ordered;
            GroupMemoryBarrierWithGroupSync();
        }
    if (index != 0) return 0;
    uint count = 0;
    for (uint i = 0; i < 64; i++)
    {
        if (!isfinite(exposureCellLogs[i])) break;
        count++;
    }
    if (count == 0) return 0;
    // Trim roughly 10% from each tail, retaining 52 of 64 ordinary cells.
    // Small windows and sparse bright paths cannot dictate the whole view's
    // exposure. No sky/depth classification changes the metering rule.
    uint trim = count / 10;
    float sum = 0;
    for (uint i = trim; i < count - trim; i++) sum += exposureCellLogs[i];
    return float2(sum / (count - 2 * trim), count);
}
float daylightExposureCompensation(float currentIrradiance, float noonIrradiance)
{
    if (!isfinite(currentIrradiance) || !isfinite(noonIrradiance)
        || currentIrradiance < 0 || noonIrradiance <= 0) return 0;
    // Display policy: retain half of the physical daylight change in stops,
    // while the view meter still adapts when entering/leaving an interior.
    // Use measured atmosphere irradiance, never clock time or a color tint.
    // A finite floor limits the adjustment to -8 stops for near-zero daylight.
    float daylightRatio = clamp(currentIrradiance / noonIrradiance, 1.0 / 65536, 1);
    return 0.5 * log2(daylightRatio);
}
float targetExposure(float logLuminance, float daylightCompensation)
{
    return clamp(log2(0.18) + daylightCompensation - logLuminance, -6, 14);
}
float adaptExposure(float previous, float target, float seconds)
{
    // Recover from a bright view faster than adapting to a dark one.
    float rate = target < previous ? 3 : 1;
    return lerp(previous, target, 1 - exp(-rate * clamp(seconds, 0, 0.25)));
}
