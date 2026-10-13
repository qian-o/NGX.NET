// Camera-volume adaptation of Hillaire's aerial perspective LUT. The shared
// atmosphere integral is MIT licensed; see Atmosphere.slang/Atmosphere.LICENSE.txt.
// Sixteen squared-distance layers, stored in a 32 x (32*16) 2D atlas so both
// backends can use the existing image and descriptor infrastructure.
#ifndef SHOWCASE_AERIAL_PERSPECTIVE
#define SHOWCASE_AERIAL_PERSPECTIVE
static const uint aerialResolution = 32;
static const uint aerialSlices = 16;

[shader("compute")]
[numthreads(8, 8, 1)]
void AtmosphereAerial(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= aerialResolution)) return;
    float2 uv = (float2(id.xy) + 0.5) / aerialResolution;
    float2 ndc = uv * float2(2, -2) + float2(-1, 1);
    float4 nearPoint = mul(float4(ndc, 1, 1), g.inverseAtmosphereViewProjection);
    float3 direction = normalize(nearPoint.xyz / nearPoint.w - g.camera.xyz);
    float3 origin = atmosphereObserverPosition();
    float maximumDistance = atmosphereDistanceToTop(origin, direction);
    float groundDistance = atmosphereDistanceToGround(origin, direction);
    if (groundDistance >= 0) maximumDistance = min(maximumDistance, groundDistance);
    float rayleighPhase, miePhase;
    atmospherePhaseFunctions(dot(direction, g.sun.xyz), rayleighPhase, miePhase);
    AtmosphereIntegral air = (AtmosphereIntegral)0;
    air.transmittance = 1;
    atmosphereAerialScatteringOutput[id.xy] = float4(0, 0, 0, 1);
    atmosphereAerialTransmittanceOutput[id.xy] = 1;
    const uint stepsPerSlice = 4;
    const uint totalSteps = (aerialSlices - 1) * stepsPerSlice;
    float previousDistance = 0;
    // Every depth layer is a prefix of one column integral. Resampling the
    // entire path independently at each layer can lose already accumulated
    // light at shadow boundaries, producing depth bands and camera shimmer.
    [loop]
    for (uint slice = 1; slice < aerialSlices; slice++)
    {
        float fraction = float(slice) / (aerialSlices - 1);
        float distance = min(fraction * fraction * g.atmosphere.w, maximumDistance);
        float dt = (distance - previousDistance) / stepsPerSlice;
        if (dt > 0)
        {
            for (uint step = 0; step < stepsPerSlice; step++)
            {
                float sampleDistance = previousDistance + (float(step) + 0.3) * dt;
                uint ordinal = (slice - 1) * stepsPerSlice + step;
                atmosphereAccumulateSegment(air, origin, direction, g.sun.xyz, sampleDistance, dt,
                    ordinal, totalSteps, rayleighPhase, miePhase, true, true, true);
            }
        }
        uint2 pixel = uint2(id.x, id.y + slice * aerialResolution);
        atmosphereAerialScatteringOutput[pixel] = float4(air.radiance, 1);
        atmosphereAerialTransmittanceOutput[pixel] = float4(air.transmittance, 1);
        previousDistance = distance;
    }
}

float2 aerialAtlasUv(float2 uv, uint slice)
{
    // Clamp within each layer to prevent filtering across atlas slice edges.
    float2 texel = clamp(uv * aerialResolution, 0.5, float(aerialResolution) - 0.5);
    return float2(texel.x / aerialResolution,
        (texel.y + slice * aerialResolution) / (aerialResolution * aerialSlices));
}
float3 applyAerialPerspective(float3 radiance, Ray ray, Hit hit)
{
    // Escaped camera rays already contain the atmosphere integrated to space
    // or the planet. Applying the camera volume to them would count it twice.
    if (hit.triangle == 0xffffffff) return radiance;
    float4 clip = mul(float4(ray.direction, 0), g.currentViewProjection);
    if (clip.w <= 0) return radiance;
    float2 uv = clip.xy / clip.w * float2(0.5, -0.5) + 0.5;
    float layer = sqrt(saturate(hit.distance * g.atmosphere.y / g.atmosphere.w)) * (aerialSlices - 1);
    uint first = min((uint)layer, aerialSlices - 2);
    float2 a = aerialAtlasUv(uv, first), b = aerialAtlasUv(uv, first + 1);
    float3 scattering = lerp(atmosphereAerialScatteringImage.SampleLevel(linearSampler, a, 0).rgb,
        atmosphereAerialScatteringImage.SampleLevel(linearSampler, b, 0).rgb, layer - first);
    float3 transmittance = lerp(atmosphereAerialTransmittanceImage.SampleLevel(linearSampler, a, 0).rgb,
        atmosphereAerialTransmittanceImage.SampleLevel(linearSampler, b, 0).rgb, layer - first);
    return radiance * transmittance + scattering * g.lighting.x;
}
#endif
