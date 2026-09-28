// Edge-preserving illumination/base-detail tone reproduction, following the
// bilateral decomposition of Durand & Dorsey (2002). Filtering illumination
// rather than RGB keeps texture contrast and material colors out of the blur.
static const uint luminanceTileSize = 16; // RenderLayout.LuminanceTileSize
static const float minimumLuminance = 1.0 / 65536.0;
static const float3 luminanceWeights = float3(0.2126, 0.7152, 0.0722);
static const float rangeSigmaStops = 1.5;
static const float displayBaseRangeStops = 6;
static const float maximumLocalExposureStops = 2;

float surfaceReflectance(float2 uv)
{
    // Reconstructed HDR is unjittered; the material G-buffer is still at the
    // current raster sample positions. Align them before estimating illumination.
    float2 guideUv = uv + (g.parameters.z > 0 ? g.jitter.xy / g.size.xy : float2(0, 0));
    float4 albedo = albedoImage.SampleLevel(linearSampler, guideUv, 0);
    float metallic = saturate(emissiveImage.SampleLevel(linearSampler, guideUv, 0).w);
    // Include dielectric F0 so black materials do not cause division by zero.
    float3 reflectance = lerp(0.04 + 0.96 * albedo.rgb, albedo.rgb, metallic);
    return lerp(1, max(dot(reflectance, luminanceWeights), 0.04), saturate(albedo.a));
}
[shader("compute")]
[numthreads(8, 8, 1)]
void PrepareLuminance(uint3 id : SV_DispatchThreadID)
{
    uint width, height; luminanceOutput.GetDimensions(width, height);
    if (any(id.xy >= uint2(width, height))) return;
    float illumination = 0, luminance = 0;
    uint count = 0;
    [loop]
    for (uint y = 0; y < luminanceTileSize; y++)
        [loop]
        for (uint x = 0; x < luminanceTileSize; x++)
        {
            uint2 pixel = id.xy * luminanceTileSize + uint2(x, y);
            if (any(pixel >= uint2(g.size.zw))) continue;
            float2 uv = (pixel + 0.5) / g.size.zw;
            float value = dot(max(sampleHdr(uv), 0), luminanceWeights);
            luminance += value;
            illumination += value / surfaceReflectance(uv);
            count++;
        }
    // Average noisy samples in linear light before taking a logarithm.
    luminanceOutput[id.xy] = float4(log2(max(illumination / count, minimumLuminance)), luminance / count, count, 0);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void FilterLuminance(uint3 id : SV_DispatchThreadID)
{
    uint width, height; filteredLuminanceOutput.GetDimensions(width, height);
    if (any(id.xy >= uint2(width, height))) return;
    float center = luminanceImage[id.xy].x;
    float spatialSigma = max(max(width, height) * 0.02, 1);
    int radius = (int)ceil(3 * spatialSigma);
    float weighted = 0, total = 0;
    [loop]
    for (int y = -radius; y <= radius; y++)
        [loop]
        for (int x = -radius; x <= radius; x++)
        {
            int2 pixel = int2(id.xy) + int2(x, y);
            if (any(pixel < 0) || any(pixel >= int2(width, height))) continue;
            float value = luminanceImage[pixel].x;
            float difference = (value - center) / rangeSigmaStops;
            float weight = exp(-0.5 * ((x * x + y * y) / (spatialSigma * spatialSigma) + difference * difference));
            weighted += value * weight;
            total += weight;
        }
    filteredLuminanceOutput[id.xy] = weighted / total;
}

groupshared float meterLuminance[64];
groupshared uint meterPixels[64];
groupshared uint illuminationPixels[64];
groupshared uint illuminationHistogram[64];
[shader("compute")]
[numthreads(8, 8, 1)]
void MeterExposure(uint index : SV_GroupIndex)
{
    if (g.exposure.x == 0)
    {
        if (index == 0) exposureOutput[uint2(0, 0)] = 0;
        return;
    }
    const float middleGray = 0.18;
    const float maximumExposureStops = 8;
    const float histogramMinimum = -16, histogramRange = 32;
    illuminationHistogram[index] = 0;
    GroupMemoryBarrierWithGroupSync();
    uint width, height; luminanceImage.GetDimensions(width, height);
    float luminance = 0;
    uint pixels = 0, litPixels = 0;
    for (uint i = index; i < width * height; i += 64)
    {
        uint2 pixel = uint2(i % width, i / width);
        float4 value = luminanceImage[pixel];
        uint count = (uint)value.z;
        luminance += value.y * count;
        pixels += count;
        if (value.y > minimumLuminance)
        {
            float base = filteredLuminanceImage[pixel];
            litPixels += count;
            uint bin = (uint)clamp((base - histogramMinimum) * 64 / histogramRange, 0, 63);
            InterlockedAdd(illuminationHistogram[bin], count);
        }
    }
    meterLuminance[index] = luminance;
    meterPixels[index] = pixels;
    illuminationPixels[index] = litPixels;
    GroupMemoryBarrierWithGroupSync();
    for (uint stride = 32; stride > 0; stride >>= 1)
    {
        if (index < stride)
        {
            meterLuminance[index] += meterLuminance[index + stride];
            meterPixels[index] += meterPixels[index + stride];
            illuminationPixels[index] += illuminationPixels[index + stride];
        }
        GroupMemoryBarrierWithGroupSync();
    }
    if (index == 0)
    {
        float averageLuminance = max(meterLuminance[0] / max(meterPixels[0], 1u), minimumLuminance);
        float target = clamp(log2(middleGray / averageLuminance), -maximumExposureStops, maximumExposureStops);
        uint cumulative = 0, lowBin = 0, highBin = 63;
        bool foundLow = false, foundHigh = false;
        for (uint bin = 0; bin < 64; bin++)
        {
            cumulative += illuminationHistogram[bin];
            if (!foundLow && cumulative >= max(1.0, illuminationPixels[0] * 0.01))
            {
                lowBin = bin; foundLow = true;
            }
            if (!foundHigh && cumulative >= max(1.0, illuminationPixels[0] * 0.99))
            {
                highBin = bin; foundHigh = true;
            }
        }
        // An arithmetic illumination anchor avoids giving deep dark areas the
        // disproportionate influence of a geometric mean. Cap at the 99th
        // percentile so a small specular glint cannot drive the whole field.
        float illuminationSum = 0;
        for (uint bin = 0; bin < 64; bin++)
            illuminationSum += illuminationHistogram[bin] * exp2(histogramMinimum + (min(bin, highBin) + 0.5) * histogramRange / 64);
        float key = log2(max(illuminationSum / max(illuminationPixels[0], 1u), minimumLuminance));
        float baseRange = (highBin - lowBin) * histogramRange / 64;
        float compression = illuminationPixels[0] == 0 ? 0 : saturate(1 - displayBaseRangeStops / max(baseRange, displayBaseRangeStops));
        float3 adapted = float3(target, key, compression);
        if (g.exposure.z == 0)
        {
            float3 previous = previousExposureImage[uint2(0, 0)].xyz;
            const float brightenHalfLifeSeconds = 1, darkenHalfLifeSeconds = 0.25;
            float halfLife = target > previous.x ? brightenHalfLifeSeconds : darkenHalfLifeSeconds;
            adapted.x = lerp(previous.x, target, 1 - exp2(-max(g.exposure.y, 0) / halfLife));
            // Smooth the local anchor and contrast range so moving highlights do
            // not cause abrupt, frame-dependent changes in the exposure field.
            adapted.yz = lerp(previous.yz, adapted.yz, 1 - exp2(-max(g.exposure.y, 0) / brightenHalfLifeSeconds));
        }
        exposureOutput[uint2(0, 0)] = float4(adapted, 0);
    }
}
float localExposure(float2 uv, float3 hdr)
{
    float3 exposure = exposureImage[uint2(0, 0)].xyz;
    if (exposure.z == 0) return exposure.x;
    float guide = log2(max(dot(max(hdr, 0), luminanceWeights) / surfaceReflectance(uv), minimumLuminance));
    uint width, height; luminanceImage.GetDimensions(width, height);
    float2 grid = uv * g.size.zw / luminanceTileSize - 0.5;
    int2 origin = int2(floor(grid));
    float2 fraction = frac(grid);
    float base = 0, total = 0;
    // Joint bilateral upsampling avoids smearing the low-resolution exposure
    // field across high-resolution silhouettes. RGB is never spatially filtered.
    for (int y = 0; y < 2; y++)
        for (int x = 0; x < 2; x++)
        {
            int2 pixel = clamp(origin + int2(x, y), int2(0), int2(width, height) - 1);
            float spatial = (x == 0 ? 1 - fraction.x : fraction.x) * (y == 0 ? 1 - fraction.y : fraction.y);
            float difference = (luminanceImage[pixel].x - guide) / rangeSigmaStops;
            float weight = spatial * exp(-0.5 * difference * difference);
            base += filteredLuminanceImage[pixel] * weight;
            total += weight;
        }
    base = total > 0 ? base / total : guide;
    return exposure.x + clamp(exposure.z * (exposure.y - base), -maximumLocalExposureStops, maximumLocalExposureStops);
}
