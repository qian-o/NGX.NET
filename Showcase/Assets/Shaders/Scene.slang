// GPU scene bindings and presentation. Light transport lives in the PBRT modules.
struct Vertex { float4 position; float4 normal; float4 tangent; float4 uv; };
struct Material { float4 baseColor; float4 emissiveMetallic; float4 parameters; float4 textures; };
struct ObjectData { float4 offset; float4 previousOffset; uint4 geometry; };
struct AreaLight { uint triangle; float area; float pmf; float cdf; };
struct Globals
{
    row_major float4x4 viewProjection;
    row_major float4x4 currentViewProjection;
    row_major float4x4 previousViewProjection;
    row_major float4x4 inverseViewProjection;
    float4 camera;
    float4 size;
    float4 sun;
    float4 scene; // area-light count, camera near, camera far, scene diameter
    float4 parameters;
    float4 lighting; // top-of-atmosphere solar irradiance, reserved, display EV, NGX enabled
    float4 lightSelection; // reserved xy, area-light power / scene projected area, reserved
    uint4 integrator; // samples/frame, max depth, first sample index, previous film samples
    float4 exposure; // elapsed seconds, initialize display exposure, reserved
    float4 atmosphere; // ground height, world-to-km, observer altitude km, aerial range km
    row_major float4x4 inverseAtmosphereViewProjection; // unjittered camera for aerial LUT
};
[[vk::binding(0, 0)]] ConstantBuffer<Globals> g : register(b0);
[[vk::binding(1, 0)]] StructuredBuffer<Vertex> vertices : register(t0);
[[vk::binding(2, 0)]] StructuredBuffer<Material> materials : register(t1);
[[vk::binding(3, 0)]] StructuredBuffer<uint> texels : register(t2);
[[vk::binding(4, 0)]] StructuredBuffer<uint4> textureInfo : register(t3);
[[vk::binding(5, 0)]] StructuredBuffer<ObjectData> objects : register(t4);
[[vk::binding(6, 0)]] StructuredBuffer<AreaLight> areaLights : register(t5);
[[vk::binding(7, 0)]] RaytracingAccelerationStructure sceneAcceleration : register(t6);
[[vk::binding(8, 0)]] Texture2D<float4> normalImage : register(t7);
[[vk::binding(9, 0)]] Texture2D<float2> motionImage : register(t8);
[[vk::binding(10, 0)]] Texture2D<float> depthImage : register(t9);
[[vk::binding(11, 0)]] Texture2D<float4> sceneImage : register(t10);
[[vk::binding(12, 0)]] Texture2D<float4> specularImage : register(t11);
[[vk::binding(13, 0)]] Texture2D<float> specularHitDistanceImage : register(t12);
[[vk::binding(14, 0)]] Texture2D<float4> reconstructedImage : register(t13);
[[vk::binding(15, 0)]] Texture2D<float4> displayInputImage : register(t14);
[[vk::binding(16, 0)]] Texture2D<float4> hudlessImage : register(t15);
[[vk::binding(17, 0)]] Texture2D<float4> uiImage : register(t16);
[[vk::binding(18, 0)]] Texture2D<float4> finalImage : register(t17);
[[vk::binding(19, 0)]] Texture2D<float4> diffuseImage : register(t18);
[[vk::binding(20, 0)]] Texture2D<float4> fontImage : register(t19);
[[vk::binding(21, 0)]] Texture2D<float4> atmosphereTransmittanceImage : register(t20);
[[vk::binding(22, 0)]] Texture2D<float4> atmosphereMultipleScatteringImage : register(t21);
[[vk::binding(23, 0)]] Texture2D<float4> atmosphereSkyImage : register(t22);
[[vk::binding(24, 0)]] Texture2D<float4> atmosphereDistributionImage : register(t23);
[[vk::binding(25, 0)]] Texture2D<float4> atmosphereAerialScatteringImage : register(t24);
[[vk::binding(26, 0)]] Texture2D<float4> atmosphereAerialTransmittanceImage : register(t25);
[[vk::binding(27, 0)]] Texture2D<float4> atmosphereGroundIrradianceImage : register(t26);
[[vk::binding(32, 0)]] RWTexture2D<float4> sceneOutput : register(u0);
[[vk::binding(33, 0)]] RWTexture2D<float4> specularOutput : register(u1);
[[vk::binding(34, 0)]] RWTexture2D<float> specularHitDistanceOutput : register(u2);
[[vk::binding(35, 0)]] RWTexture2D<float4> reconstructedOutput : register(u3);
[[vk::binding(36, 0)]] RWTexture2D<float4> displayInputOutput : register(u4);
[[vk::binding(37, 0)]] RWTexture2D<float4> hudlessOutput : register(u5);
[[vk::binding(38, 0)]] RWTexture2D<float4> finalOutput : register(u6);
[[vk::binding(39, 0)]] RWTexture2D<float2> motionOutput : register(u7);
[[vk::binding(40, 0)]] RWTexture2D<float4> diffuseOutput : register(u8);
[[vk::binding(41, 0)]] RWTexture2D<float4> normalOutput : register(u9);
[[vk::binding(42, 0)]] RWTexture2D<float> depthOutput : register(u10);
[[vk::binding(43, 0)]] RWTexture2D<float4> film : register(u11);
[[vk::binding(44, 0)]] RWTexture2D<float> exposureState : register(u12);
[[vk::binding(45, 0)]] RWTexture2D<float4> atmosphereTransmittanceOutput : register(u13);
[[vk::binding(46, 0)]] RWTexture2D<float4> atmosphereMultipleScatteringOutput : register(u14);
[[vk::binding(47, 0)]] RWTexture2D<float4> atmosphereSkyOutput : register(u15);
[[vk::binding(48, 0)]] RWTexture2D<float4> atmosphereDistributionOutput : register(u16);
[[vk::binding(49, 0)]] RWTexture2D<float4> atmosphereAerialScatteringOutput : register(u17);
[[vk::binding(50, 0)]] RWTexture2D<float4> atmosphereAerialTransmittanceOutput : register(u18);
[[vk::binding(51, 0)]] RWTexture2D<float4> atmosphereGroundIrradianceOutput : register(u19);
[[vk::binding(64, 0)]] SamplerState linearSampler : register(s0);

#include "Textures.slang"
#include "SurfaceInteraction.slang"
#include "PbrtSampling.slang"
#include "PbrtBsdf.slang"
#include "Atmosphere.slang"
#include "AerialPerspective.slang"
#include "EnvironmentLighting.slang"
#include "PathTracing.slang"
#include "CameraFilm.slang"
#include "ToneMapping.slang"
#include "AutoExposure.slang"

float3 linearToSrgb(float3 c)
{
    return select(c <= 0.0031308, c * 12.92, 1.055 * pow(max(c, 0), 1.0 / 2.4) - 0.055);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void ToneMap(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= uint2(g.size.zw))) return;
    float2 uv = (id.xy + 0.5) / g.size.zw;
    float3 hdr = g.parameters.z > 0 ? reconstructedImage.SampleLevel(linearSampler, uv, 0).rgb : sceneImage.SampleLevel(linearSampler, uv, 0).rgb;
    // Display-only, global exposure. Film and NGX always retain linear radiance.
    displayInputOutput[id.xy] = float4(linearToSrgb(displayToneMap(hdr * exp2(exposureState[uint2(0)] + g.lighting.z))), 1);
}
[shader("compute")]
[numthreads(8, 8, 1)]
void CopyDisplay(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= uint2(g.size.zw))) return;
    hudlessOutput[id.xy] = displayInputImage[id.xy];
}
[shader("compute")]
[numthreads(8, 8, 1)]
void Composite(uint3 id : SV_DispatchThreadID)
{
    if (any(id.xy >= uint2(g.size.zw))) return;
    float4 ui = uiImage[id.xy];
    finalOutput[id.xy] = float4(hudlessImage[id.xy].rgb * (1 - ui.a) + ui.rgb, 1);
}
struct UiInput { float2 position : POSITION; float2 uv : TEXCOORD; float4 color : COLOR; };
struct UiVertex { float4 position : SV_Position; float2 uv : TEXCOORD; float4 color : COLOR; };
[shader("vertex")]
UiVertex UiVS(UiInput input)
{
    UiVertex output;
    output.position = float4(input.position / g.size.zw * float2(2, -2) + float2(-1, 1), 0, 1);
    output.uv = input.uv;
    output.color = input.color;
    return output;
}
[shader("fragment")]
float4 UiPS(UiVertex input) : SV_Target
{
    float4 value = input.color * fontImage.Sample(linearSampler, input.uv);
    return float4(value.rgb * value.a, value.a);
}
