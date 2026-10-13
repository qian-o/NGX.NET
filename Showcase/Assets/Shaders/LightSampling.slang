// Scene-independent light sampling and multiple importance sampling math.
// pbrt is Copyright(c) 1998-2020 Matt Pharr, Wenzel Jakob, and Greg Humphreys.
// SPDX-License-Identifier: Apache-2.0; see Pbrt.LICENSE.txt.
// PBRT-v4 b4ce9687e6c695f5582997c61b0c66cf064bdb4a:
// PathIntegrator environment MIS and PowerHeuristic.
#ifndef SHOWCASE_LIGHT_SAMPLING
#define SHOWCASE_LIGHT_SAMPLING
static const float lightPi = 3.14159265358979323846;
float powerHeuristic(float a, float b)
{
    float scale = max(a, b);
    if (scale == 0) return 0;
    a /= scale; b /= scale;
    return a * a / (a * a + b * b);
}
float solarOneMinusCosTheta(float radius)
{
    float sine = sin(radius * 0.5);
    return 2 * sine * sine;
}
bool solarDirectionInside(float3 direction, float3 sun, float radius)
{
    float3 difference = direction - sun;
    return dot(difference, difference) <= 2 * solarOneMinusCosTheta(radius);
}
float solarRadiance(float irradiance, float radius)
{
    float sine = sin(radius);
    return irradiance / (lightPi * sine * sine);
}
float solarPdf(float3 direction, float3 sun, float radius, float selectionPmf)
{
    return solarDirectionInside(direction, sun, radius)
        ? selectionPmf / (2 * lightPi * solarOneMinusCosTheta(radius)) : 0;
}
float3 sampleSolarLocal(float radius, float2 u)
{
    float oneMinusCos = u.x * solarOneMinusCosTheta(radius);
    float sine = sqrt(max(0, oneMinusCos * (2 - oneMinusCos))), phi = 2 * lightPi * u.y;
    return float3(sine * cos(phi), sine * sin(phi), 1 - oneMinusCos);
}
#endif
