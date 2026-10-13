// Hillaire, "A Scalable and Production Ready Sky and Atmosphere Rendering
// Technique" (2020). Adapted from sebh/UnrealEngineSkyAtmosphere at
// 183ead5bdacc701b3b626347a680a2f3cd3d4fbd, RenderSkyCommon.hlsl and
// RenderSkyRayMarching.hlsl. Copyright (c) 2020 Epic Games, Inc.
// SPDX-License-Identifier: MIT; see Atmosphere.LICENSE.txt.
// Lengths are kilometers. All LUT radiance is per unit top-of-atmosphere
// solar irradiance; lighting consumers apply the scene's solar intensity.
#ifndef SHOWCASE_ATMOSPHERE
#define SHOWCASE_ATMOSPHERE

static const float atmospherePi = 3.141592653589793;
static const float atmosphereBottomRadius = 6360;
static const float atmosphereTopRadius = 6460;
static const float3 atmosphereGroundAlbedo = float3(0.1);
static const uint2 atmosphereTransmittanceSize = uint2(256, 64);
static const uint2 atmosphereMultipleScatteringSize = uint2(32, 32);
static const uint2 atmosphereSkySize = uint2(256, 128);

struct AtmosphereMedium
{
    float3 rayleigh;
    float3 mie;
    float3 scattering;
    float3 extinction;
};
struct AtmosphereIntegral
{
    float3 radiance;
    float3 transmittance;
    float3 multipleScatteringTransfer;
};

AtmosphereMedium atmosphereMedium(float3 position)
{
    float altitude = max(0, length(position) - atmosphereBottomRadius);
    float rayleighDensity = exp(-altitude / 8);
    float mieDensity = exp(-altitude / 1.2);
    float ozoneDensity = saturate(altitude < 25 ? altitude / 15 - 2.0 / 3 : 8.0 / 3 - altitude / 15);
    AtmosphereMedium medium;
    medium.rayleigh = rayleighDensity * float3(0.005802, 0.013558, 0.033100);
    medium.mie = mieDensity * float3(0.003996);
    medium.scattering = medium.rayleigh + medium.mie;
    medium.extinction = medium.rayleigh + mieDensity * float3(0.004440)
        + ozoneDensity * float3(0.000650, 0.001881, 0.000085);
    return medium;
}

float atmosphereDistanceToTop(float3 position, float3 direction)
{
    float radius = length(position), projection = dot(position, direction);
    float c = (radius - atmosphereTopRadius) * (radius + atmosphereTopRadius);
    return max(0, -projection + sqrt(max(0, projection * projection - c)));
}
float atmosphereDistanceToGround(float3 position, float3 direction)
{
    float radius = length(position), projection = dot(position, direction);
    if (projection >= 0) return -1;
    float c = max(0, (radius - atmosphereBottomRadius) * (radius + atmosphereBottomRadius));
    float discriminant = projection * projection - c;
    if (discriminant < 0) return -1;
    // Rationalized near root avoids subtracting Earth-sized distances for
    // meter-high observers and very short aerial-perspective segments.
    return c / max(1e-6, -projection + sqrt(discriminant));
}

float2 atmosphereLutUv(float2 unit, uint2 size)
{
    return (saturate(unit) * (float2(size) - 1) + 0.5) / float2(size);
}
float2 atmosphereTransmittanceUv(float radius, float cosine)
{
    float H = sqrt(atmosphereTopRadius * atmosphereTopRadius - atmosphereBottomRadius * atmosphereBottomRadius);
    float rho = sqrt(max(0, (radius - atmosphereBottomRadius) * (radius + atmosphereBottomRadius)));
    float discriminant = radius * radius * (cosine * cosine - 1) + atmosphereTopRadius * atmosphereTopRadius;
    float distance = max(0, -radius * cosine + sqrt(max(0, discriminant)));
    float minimum = atmosphereTopRadius - radius, maximum = rho + H;
    return atmosphereLutUv(float2((distance - minimum) / max(1e-6, maximum - minimum), rho / H),
        atmosphereTransmittanceSize);
}
float3 atmosphereTransmittance(float3 position, float3 direction)
{
    if (atmosphereDistanceToGround(position, direction) >= 0) return 0;
    float radius = length(position);
    float cosine = dot(position, direction) / radius;
    return atmosphereTransmittanceImage.SampleLevel(linearSampler, atmosphereTransmittanceUv(radius, cosine), 0).rgb;
}
float3 atmosphereMultipleScattering(float3 position, float3 sunDirection)
{
    float radius = length(position);
    float cosine = clamp(dot(position, sunDirection) / radius, -1, 1);
    // Resolve the rapid sunset transition and dense lower atmosphere. A linear
    // 32-column solar grid interpolates between +/-1.85 degrees at sunset and
    // overestimates its green/blue multiple scattering by about twenty percent.
    float signedRoot = (cosine < 0 ? -1 : 1) * sqrt(abs(cosine));
    float height = saturate((radius - atmosphereBottomRadius - 0.002)
        / (atmosphereTopRadius - atmosphereBottomRadius - 0.004));
    float2 unit = float2(signedRoot * 0.5 + 0.5, sqrt(height));
    return atmosphereMultipleScatteringImage.SampleLevel(linearSampler,
        atmosphereLutUv(unit, atmosphereMultipleScatteringSize), 0).rgb;
}
float3 atmosphereObserverPosition()
{
    return float3(0, atmosphereBottomRadius + clamp(g.atmosphere.z, 0.002, 99.9), 0);
}
float3 sceneWorldToAtmosphere(float3 worldPosition)
{
    float3 position = (worldPosition - g.camera.xyz) * g.atmosphere.y + atmosphereObserverPosition();
    // Sponza can include its foundation below the authored walking surface.
    // Keep atmosphere queries above Earth without changing traced scene rays.
    float radius = length(position);
    if (radius < atmosphereBottomRadius + 0.002)
        position *= (atmosphereBottomRadius + 0.002) / radius;
    return position;
}
float3 atmosphereSolarTransmittance(float3 worldPosition, float3 direction)
{
    return atmosphereTransmittance(sceneWorldToAtmosphere(worldPosition), direction);
}
float3 atmosphereUvToDirection(float2 uv)
{
    float cosine = 1 - 2 * uv.y, phi = 2 * atmospherePi * uv.x;
    float sine = sqrt(max(0, 1 - cosine * cosine));
    return float3(sine * cos(phi), cosine, sine * sin(phi));
}
float2 atmosphereDirectionToUv(float3 direction)
{
    return float2(frac(atan2(direction.z, direction.x) / (2 * atmospherePi) + 1),
        saturate(0.5 - 0.5 * direction.y));
}
float atmosphereSkyHorizonAngle()
{
    float radius = atmosphereObserverPosition().y;
    float horizonCosine = -sqrt(max(0, (radius - atmosphereBottomRadius)
        * (radius + atmosphereBottomRadius))) / radius;
    return acos(clamp(horizonCosine, -1, 1));
}
float3 atmosphereSkyUvToDirection(float2 uv)
{
    // Hillaire's nonlinear sky-view elevation mapping puts half of the rows
    // on each side of the actual planetary horizon, with quadratic spacing.
    // Absolute azimuth remains periodic; this is independent of the equal-area
    // sphere parameterization used by multiple scattering and importance PDFs.
    float horizon = atmosphereSkyHorizonAngle();
    float coordinate = abs(2 * uv.y - 1);
    float theta = uv.y < 0.5 ? horizon * (1 - coordinate * coordinate)
        : horizon + (atmospherePi - horizon) * coordinate * coordinate;
    float phi = 2 * atmospherePi * uv.x;
    return float3(sin(theta) * cos(phi), cos(theta), sin(theta) * sin(phi));
}
float2 atmosphereDirectionToSkyUv(float3 direction)
{
    float horizon = atmosphereSkyHorizonAngle();
    float theta = atan2(length(direction.xz), direction.y);
    float v = theta < horizon ? 0.5 * (1 - sqrt(saturate(1 - theta / horizon)))
        : 0.5 + 0.5 * sqrt(saturate((theta - horizon) / (atmospherePi - horizon)));
    return float2(frac(atan2(direction.z, direction.x) / (2 * atmospherePi) + 1), v);
}
float3 atmosphereSkyRadiance(float3 direction)
{
    // The shared hardware sampler clamps. Wrap azimuth explicitly so the
    // sky environment has no seam in camera views or mirror reflections.
    float2 texel = atmosphereDirectionToSkyUv(direction) * atmosphereSkySize - 0.5;
    int2 first = int2(floor(texel));
    float2 blend = frac(texel);
    int x0 = (first.x + int(atmosphereSkySize.x)) % int(atmosphereSkySize.x);
    int x1 = (x0 + 1) % int(atmosphereSkySize.x);
    int y0 = clamp(first.y, 0, int(atmosphereSkySize.y) - 1);
    int y1 = clamp(first.y + 1, 0, int(atmosphereSkySize.y) - 1);
    return lerp(lerp(atmosphereSkyImage.Load(int3(x0, y0, 0)).rgb,
        atmosphereSkyImage.Load(int3(x1, y0, 0)).rgb, blend.x),
        lerp(atmosphereSkyImage.Load(int3(x0, y1, 0)).rgb,
        atmosphereSkyImage.Load(int3(x1, y1, 0)).rgb, blend.x), blend.y);
}

float3 atmosphereSegmentIntegral(float3 extinction, float distance, float3 transmittance)
{
    // The limiting expression distance avoids cancellation for optically thin
    // segments (especially indoor aerial perspective and upper-atmosphere rays).
    float3 opticalDepth = extinction * distance;
    return select(opticalDepth < 1e-3,
        distance * (1 - opticalDepth * 0.5 + opticalDepth * opticalDepth / 6),
        (1 - transmittance) / max(extinction, float3(1e-20)));
}
bool atmosphereSceneVisible(float3 worldPosition, float3 direction)
{
    Ray ray;
    ray.origin = worldPosition;
    ray.direction = direction;
    ray.tMin = 0;
    ray.tMax = rayInfinity;
    // Atmospheric samples are in the volume, not on a shaded surface. Use the
    // ordinary alpha-aware scene visibility without a guessed surface offset.
    return unoccluded(ray);
}
float atmosphereSceneSkyVisibility(float3 worldPosition, uint step, uint steps)
{
    float visibility = 0;
    // Each march point tests complementary elevations. A monotonic sequence
    // would correlate distant (larger) integration segments with zenith rays,
    // over-weighting openings above a roof. The fixed low-discrepancy rotation
    // keeps this quadrature deterministic across frames without that coupling.
    float firstCosine = float(reversebits(step)) * 2.3283064365386963e-10 + 0.5 / steps;
    float firstPhi = 2 * atmospherePi * frac((float(step) + 0.5) * 0.61803398875);
    for (uint sample = 0; sample < 2; sample++)
    {
        float cosine = sample == 0 ? firstCosine : 1 - firstCosine;
        float phi = firstPhi + sample * atmospherePi;
        float sine = sqrt(max(0, 1 - cosine * cosine));
        float3 direction = float3(sine * cos(phi), cosine, sine * sin(phi));
        visibility += atmosphereSceneVisible(worldPosition, direction) ? 0.5 : 0;
    }
    return visibility;
}
void atmospherePhaseFunctions(float cosine, out float rayleighPhase, out float miePhase)
{
    rayleighPhase = 3 * (1 + cosine * cosine) / (16 * atmospherePi);
    const float mieG = 0.8;
    float denominator = 1 + mieG * mieG - 2 * mieG * cosine;
    // The pinned reference enables USE_CornetteShanks. World direction points
    // toward the viewed sky, so forward solar scattering peaks at cosine = 1.
    miePhase = 3 * (1 - mieG * mieG) * (1 + cosine * cosine)
        / (8 * atmospherePi * (2 + mieG * mieG) * denominator * sqrt(denominator));
}
void atmosphereAccumulateSegment(inout AtmosphereIntegral result, float3 origin, float3 direction,
    float3 sunDirection, float sampleDistance, float dt, uint step, uint steps,
    float rayleighPhase, float miePhase, bool phaseFunctions, bool multipleScattering, bool sceneVisibility)
{
    float3 position = origin + direction * sampleDistance;
    AtmosphereMedium medium = atmosphereMedium(position);
    float3 segmentT = exp(-medium.extinction * dt);
    float3 integral = atmosphereSegmentIntegral(medium.extinction, dt, segmentT);
    float3 phaseScattering = phaseFunctions
        ? medium.rayleigh * rayleighPhase + medium.mie * miePhase
        : medium.scattering / (4 * atmospherePi);
    float sunVisibility = 1, skyVisibility = 1;
    if (sceneVisibility)
    {
        // Only the local aerial-volume pass enables this branch. Preserve
        // meter-scale precision instead of subtracting Earth-sized values.
        float3 worldPosition = g.camera.xyz + direction * (sampleDistance / g.atmosphere.y);
        sunVisibility = atmosphereSceneVisible(worldPosition, sunDirection) ? 1 : 0;
        // The multiple-scattering LUT assumes an unobstructed atmosphere.
        // Approximate its local sky access with two fixed hemisphere rays per
        // segment. This is visibility of the sky, not room GI or a new multiple-
        // scattering solution; an enclosed room receives zero.
        skyVisibility = atmosphereSceneSkyVisibility(worldPosition, step, steps);
    }
    float3 source = sunVisibility * atmosphereTransmittance(position, sunDirection) * phaseScattering;
    if (multipleScattering) source += skyVisibility * atmosphereMultipleScattering(position, sunDirection) * medium.scattering;
    result.radiance += result.transmittance * source * integral;
    result.multipleScatteringTransfer += result.transmittance * medium.scattering * integral;
    result.transmittance *= segmentT;
}
AtmosphereIntegral atmosphereIntegrate(float3 origin, float3 direction, float3 sunDirection,
    float maximumDistance, uint steps, bool phaseFunctions, bool multipleScattering, bool ground,
    bool sceneVisibility = false)
{
    AtmosphereIntegral result = (AtmosphereIntegral)0;
    result.transmittance = 1;
    float topDistance = atmosphereDistanceToTop(origin, direction);
    float groundDistance = atmosphereDistanceToGround(origin, direction);
    float distance = min(maximumDistance, groundDistance >= 0 ? min(topDistance, groundDistance) : topDistance);
    if (distance <= 0) return result;
    float rayleighPhase, miePhase;
    atmospherePhaseFunctions(dot(direction, sunDirection), rayleighPhase, miePhase);
    [loop]
    for (uint i = 0; i < steps; i++)
    {
        // Squared spacing resolves dense air near the observer while retaining
        // the complete segment integral and a deterministic, noise-free LUT.
        float start = float(i) / steps, end = float(i + 1) / steps;
        start *= start; end *= end;
        float dt = distance * (end - start);
        float sampleDistance = distance * (start + (end - start) * 0.3);
        atmosphereAccumulateSegment(result, origin, direction, sunDirection, sampleDistance, dt,
            i, steps, rayleighPhase, miePhase, phaseFunctions, multipleScattering, sceneVisibility);
    }
    if (ground && groundDistance >= 0 && groundDistance <= maximumDistance && groundDistance <= topDistance)
    {
        float3 position = origin + direction * groundDistance;
        float3 normal = normalize(position);
        // Sample just above the ground boundary, retaining planet visibility.
        float3 sunlight = atmosphereTransmittance(normal * (atmosphereBottomRadius + 0.002), sunDirection);
        // A diffuse ground receives the upper sky as well as direct sunlight.
        // Use the current observer's ground irradiance over this local scene;
        // the static multiple-scattering build cannot read this dependent LUT.
        float3 diffuseSky = multipleScattering ? atmosphereGroundIrradianceImage.Load(int3(0, 0, 0)).rgb : float3(0);
        result.radiance += result.transmittance * atmosphereGroundAlbedo
            * (sunlight * saturate(dot(normal, sunDirection)) + diffuseSky) / atmospherePi;
    }
    return result;
}

[shader("compute")]
[numthreads(8, 8, 1)]
void AtmosphereTransmittance(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= atmosphereTransmittanceSize)) return;
    float2 uv = float2(id.xy) / float2(atmosphereTransmittanceSize - 1);
    float H = sqrt(atmosphereTopRadius * atmosphereTopRadius - atmosphereBottomRadius * atmosphereBottomRadius);
    float rho = H * uv.y;
    float radius = sqrt(rho * rho + atmosphereBottomRadius * atmosphereBottomRadius);
    float minimum = atmosphereTopRadius - radius, maximum = rho + H;
    float distance = lerp(minimum, maximum, uv.x);
    float cosine = distance == 0 ? 1 : clamp((H * H - rho * rho - distance * distance) / (2 * radius * distance), -1, 1);
    float3 origin = float3(0, radius, 0), direction = float3(sqrt(max(0, 1 - cosine * cosine)), cosine, 0);
    float3 opticalDepth = 0;
    const uint steps = 64;
    for (uint i = 0; i < steps; i++)
    {
        float start = float(i) / steps, end = float(i + 1) / steps;
        start *= start; end *= end;
        float dt = distance * (end - start);
        opticalDepth += atmosphereMedium(origin + direction * (distance * (start + end) * 0.5)).extinction * dt;
    }
    atmosphereTransmittanceOutput[id.xy] = float4(exp(-opticalDepth), 1);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void AtmosphereMultipleScattering(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= atmosphereMultipleScatteringSize)) return;
    float2 uv = float2(id.xy) / float2(atmosphereMultipleScatteringSize - 1);
    float solarCoordinate = 2 * uv.x - 1;
    float cosine = solarCoordinate * abs(solarCoordinate);
    float3 sun = float3(sqrt(max(0, 1 - cosine * cosine)), cosine, 0);
    float height = lerp(atmosphereBottomRadius + 0.002, atmosphereTopRadius - 0.002, uv.y * uv.y);
    float3 radiance = 0, transfer = 0;
    [loop]
    for (uint sampleIndex = 0; sampleIndex < 64; sampleIndex++)
    {
        float2 sphereUv = (float2(sampleIndex / 8, sampleIndex % 8) + 0.5) / 8;
        AtmosphereIntegral scattering = atmosphereIntegrate(float3(0, height, 0),
            atmosphereUvToDirection(sphereUv), sun, 1e6, 20, false, false, true);
        radiance += scattering.radiance;
        transfer += scattering.multipleScatteringTransfer;
    }
    // Hillaire equations 5, 7 and 10: sphere-averaged second-order input
    // and transfer f_ms, followed by the infinite geometric series.
    float3 secondOrder = radiance / 64;
    transfer /= 64;
    atmosphereMultipleScatteringOutput[id.xy] = float4(secondOrder / max(float3(1e-4), 1 - transfer), 1);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void AtmosphereGroundIrradiance(uint3 id : SV_DispatchThreadID)
{
    if (id.x >= 2 || id.y != 0) return;
    float3 sun = id.x == 0 ? g.sun.xyz : float3(0, 1, 0);
    float3 origin = float3(0, atmosphereBottomRadius + 0.002, 0);
    float3 skyIrradiance = 0;
    // Cosine-weighted hemisphere quadrature: E_sky = pi * average(L_sky).
    // This pass precedes Sky, and never samples the ground, avoiding feedback.
    const uint skySamples = 128;
    for (uint i = 0; i < skySamples; i++)
    {
        float radial = (float(i) + 0.5) / skySamples;
        float phi = 2 * atmospherePi * float(reversebits(i)) * 2.3283064365386963e-10;
        float3 direction = float3(sqrt(radial) * cos(phi), sqrt(1 - radial), sqrt(radial) * sin(phi));
        AtmosphereIntegral sky = atmosphereIntegrate(origin, direction, sun, 1e6, 48, true, true, false);
        skyIrradiance += sky.radiance * (atmospherePi / skySamples);
    }
    float3 solarIrradiance = 0;
    float3 axis = abs(sun.y) < 0.99 ? normalize(cross(float3(0, 1, 0), sun)) : float3(1, 0, 0);
    float3 bitangent = cross(sun, axis);
    float sineHalf = sin(g.sun.w * 0.5);
    float oneMinusCos = 2 * sineHalf * sineHalf;
    const uint solarSamples = 64;
    for (uint i = 0; i < solarSamples; i++)
    {
        float q = oneMinusCos * (float(i) + 0.5) / solarSamples;
        float phi = 2 * atmospherePi * float(reversebits(i)) * 2.3283064365386963e-10;
        float sine = sqrt(max(0, q * (2 - q)));
        float3 direction = axis * (sine * cos(phi)) + bitangent * (sine * sin(phi)) + sun * (1 - q);
        solarIrradiance += atmosphereTransmittance(origin, direction) * max(0, direction.y) / solarSamples;
    }
    // The disk's solid angle divided by its projected area is 2/(1+cos r).
    solarIrradiance *= 2 / (2 - oneMinusCos);
    float totalLuminance = dot(skyIrradiance + solarIrradiance, float3(0.2126, 0.7152, 0.0722));
    atmosphereGroundIrradianceOutput[id.xy] = float4(skyIrradiance, totalLuminance);
}

float3 atmosphereSceneHorizonRadiance(float3 direction, float3 physicalRadiance)
{
    // Finite-scene background closure: Sponza has no distant terrain to cover
    // the virtual planet. Continue its own horizon haze into a lower hemisphere
    // of sky-lit fog. This is an environment presentation extension,
    // not additional atmospheric scattering or a change to the physical solver.
    // Baking it here keeps camera, reflections and importance sampling aligned.
    const float upperGuardRadians = 0.02 * atmospherePi / 180;
    const float lowerFadeRadians = 12 * atmospherePi / 180;
    const float fullStrengthHeightKm = 0.1;
    const float disableHeightKm = 1;
    float heightStrength = 1 - smoothstep(fullStrengthHeightKm, disableHeightKm, g.atmosphere.z);
    if (heightStrength <= 0) return physicalRadiance;

    float horizon = atmosphereSkyHorizonAngle();
    float theta = atan2(length(direction.xz), direction.y);
    float elevation = horizon - theta;
    if (elevation >= upperGuardRadians) return physicalRadiance;
    // Approximate the fog's incident hemispheric illumination from the actual
    // cosine-integrated skylight. No fixed fog hue or minimum brightness is
    // imposed: sunset color and darkness follow the same atmosphere.
    float3 fogRadiance = 0.5 * atmosphereGroundIrradianceImage.Load(int3(0, 0, 0)).rgb / atmospherePi;
    // The nadir has no azimuth. Return before normalizing direction.xz, and
    // avoid a second atmosphere integration across the entire lower hemisphere.
    if (elevation <= -lowerFadeRadians)
        return lerp(physicalRadiance, fogRadiance, heightStrength);
    float referenceTheta = horizon - upperGuardRadians;
    float2 azimuth = normalize(direction.xz);
    float3 referenceDirection = float3(azimuth.x * sin(referenceTheta), cos(referenceTheta),
        azimuth.y * sin(referenceTheta));
    AtmosphereIntegral haze = atmosphereIntegrate(atmosphereObserverPosition(), referenceDirection,
        g.sun.xyz, 1e6, 48, true, true, true);
    float3 background = elevation >= 0
        ? lerp(physicalRadiance, haze.radiance, 1 - smoothstep(0, upperGuardRadians, elevation))
        : lerp(haze.radiance, fogRadiance, smoothstep(0, lowerFadeRadians, -elevation));
    return lerp(physicalRadiance, background, heightStrength);
}

[shader("compute")]
[numthreads(8, 8, 1)]
void AtmosphereSky(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= atmosphereSkySize)) return;
    float3 direction = atmosphereSkyUvToDirection((float2(id.xy) + 0.5) / atmosphereSkySize);
    AtmosphereIntegral sky = atmosphereIntegrate(atmosphereObserverPosition(), direction,
        g.sun.xyz, 1e6, 48, true, true, true);
    // Horizon-resolved full sphere, excluding the solar disk. Alpha is unused.
    atmosphereSkyOutput[id.xy] = float4(atmosphereSceneHorizonRadiance(direction, sky.radiance), 1);
}
#endif
