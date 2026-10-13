// Importance sampling of the directional Hillaire atmosphere radiance table.
// Resample its horizon-warped visual LUT onto equal-area importance cells with
// solid angle 4*pi/(256*128), including the ground half. The radiance lookup and
// proposal parameterizations are deliberately separate; no warped-PDF Jacobian.
// The sky table excludes the solar disk; both emitters retain separate MIS.
#ifndef SHOWCASE_SKY_SAMPLING
#define SHOWCASE_SKY_SAMPLING
#include "LightSampling.slang"
static const uint skySamplingWidth = 256;
static const uint skySamplingHeight = 128;
static const float skyUniformMixture = 0.01;

float skyLuminance(float3 radiance)
{
    return dot(max(0,radiance), float3(0.2126,0.7152,0.0722));
}
float2 skySamplingUv(float3 direction)
{
    return float2(frac(atan2(direction.z,direction.x)/(2*lightPi)+1),
        saturate((1-direction.y)*0.5));
}
float3 skySamplingDirection(float2 uv)
{
    float cosine=1-2*uv.y, sine=sqrt(max(0,1-cosine*cosine));
    float phi=2*lightPi*uv.x;
    return float3(sine*cos(phi),cosine,sine*sin(phi));
}
float skyConditionalCdf(uint column, uint row)
{
    return atmosphereDistributionImage.Load(int3(column,row,0)).x;
}
float skyMarginalCdf(uint row)
{
    return atmosphereDistributionImage.Load(int3(row,skySamplingHeight,0)).x;
}
float3 atmosphereLightPmf()
{
    return atmosphereDistributionImage.Load(int3(129,skySamplingHeight,0)).yzw;
}
float atmosphereSkyPdf(float3 direction)
{
    float2 uv=skySamplingUv(direction);
    uint2 cell=min(uint2(uv*float2(skySamplingWidth,skySamplingHeight)),
        uint2(skySamplingWidth-1,skySamplingHeight-1));
    float rowPmf=max(0,skyMarginalCdf(cell.y+1)-skyMarginalCdf(cell.y));
    float columnPmf=max(0,skyConditionalCdf(cell.x+1,cell.y)-skyConditionalCdf(cell.x,cell.y));
    // Bilinear radiance can be positive beside an empty importance cell. The
    // explicit uniform mixture supplies every direction without biasing color.
    return ((1-skyUniformMixture)*rowPmf*columnPmf*skySamplingWidth*skySamplingHeight
        +skyUniformMixture)/(4*lightPi);
}
float3 sampleAtmosphereSky(float2 u)
{
    if(u.x<skyUniformMixture)
        return skySamplingDirection(float2(u.y,u.x/skyUniformMixture));
    float marginalU=min((u.x-skyUniformMixture)/(1-skyUniformMixture),0.9999999403953552);
    uint low=0,high=skySamplingHeight-1;
    while(low<high)
    {
        uint middle=(low+high)/2;
        if(marginalU>=skyMarginalCdf(middle+1)) low=middle+1;
        else high=middle;
    }
    uint row=low;
    float rowStart=skyMarginalCdf(row), rowWidth=skyMarginalCdf(row+1)-rowStart;
    float rowOffset=rowWidth>0?saturate((marginalU-rowStart)/rowWidth):0.5;
    low=0;high=skySamplingWidth-1;
    while(low<high)
    {
        uint middle=(low+high)/2;
        if(u.y>=skyConditionalCdf(middle+1,row)) low=middle+1;
        else high=middle;
    }
    uint column=low;
    float start=skyConditionalCdf(column,row), width=skyConditionalCdf(column+1,row)-start;
    float columnOffset=width>0?saturate((u.y-start)/width):0.5;
    return skySamplingDirection(float2((column+columnOffset)/skySamplingWidth,
        (row+rowOffset)/skySamplingHeight));
}

groupshared float skyRowSum[skySamplingHeight];
[shader("compute")]
[numthreads(256,1,1)]
void AtmosphereDistribution(uint3 group : SV_GroupID, uint lane : SV_GroupIndex)
{
    if(any(group!=0)) return;
    // One workgroup builds all rows; synchronization is entirely local.
    // Define reserved metadata texels too, keeping LUT captures deterministic.
    if(lane>130)
        atmosphereDistributionOutput[uint2(lane,skySamplingHeight)]=0;
    if(lane==255)
        atmosphereDistributionOutput[uint2(256,skySamplingHeight)]=0;
    if(lane<skySamplingHeight)
    {
        float total=0;
        for(uint column=0;column<skySamplingWidth;column++)
        {
            float3 direction=skySamplingDirection((float2(column,lane)+0.5)/float2(skySamplingWidth,skySamplingHeight));
            total+=skyLuminance(atmosphereSkyRadiance(direction));
        }
        skyRowSum[lane]=total;
        float cumulative=0;
        atmosphereDistributionOutput[uint2(0,lane)]=0;
        for(uint column=0;column<skySamplingWidth;column++)
        {
            float3 direction=skySamplingDirection((float2(column,lane)+0.5)/float2(skySamplingWidth,skySamplingHeight));
            cumulative+=skyLuminance(atmosphereSkyRadiance(direction));
            float cdf=total>0?saturate(cumulative/total):float(column+1)/skySamplingWidth;
            atmosphereDistributionOutput[uint2(column+1,lane)]=float4(column+1==skySamplingWidth?1:cdf,0,0,0);
        }
    }
    GroupMemoryBarrierWithGroupSync();
    if(lane==0)
    {
        float total=0;
        for(uint row=0;row<skySamplingHeight;row++) total+=skyRowSum[row];
        float cumulative=0;
        atmosphereDistributionOutput[uint2(0,skySamplingHeight)]=0;
        for(uint row=0;row<skySamplingHeight;row++)
        {
            cumulative+=skyRowSum[row];
            float cdf=total>0?saturate(cumulative/total):float(row+1)/skySamplingHeight;
            atmosphereDistributionOutput[uint2(row+1,skySamplingHeight)]=float4(row+1==skySamplingHeight?1:cdf,0,0,0);
        }
        // Integrate attenuation across the finite disk so a partly visible
        // sunset still receives direct-light samples when its center is hidden.
        float3 averageSun=0;
        for(uint i=0;i<64;i++)
        {
            float2 u=float2((i+0.5)/64,float(reversebits(i))*2.3283064365386963e-10);
            float3 direction=basisDirection(g.sun.xyz,sampleSolarLocal(g.sun.w,u));
            averageSun+=g.lighting.x*atmosphereSolarTransmittance(g.camera.xyz,direction)/64;
        }
        float skyPower=total*g.lighting.x*(4*lightPi/(skySamplingWidth*skySamplingHeight));
        float sunPower=skyLuminance(averageSun);
        float areaPower=max(0,g.lightSelection.z);
        float allPower=skyPower+sunPower+areaPower;
        float3 pmf=allPower>0?float3(skyPower,sunPower,areaPower)/allPower:float3(1,0,0);
        // A power estimate may miss a tiny visible disk segment or altitude
        // difference. Positive solar support preserves unbiased surface light.
        if(g.lighting.x>0)
        {
            pmf*=0.999;
            pmf.y+=0.001;
        }
        atmosphereDistributionOutput[uint2(129,skySamplingHeight)]=float4(skyPower,pmf);
        atmosphereDistributionOutput[uint2(130,skySamplingHeight)]=float4(averageSun,0);
    }
}
#endif
