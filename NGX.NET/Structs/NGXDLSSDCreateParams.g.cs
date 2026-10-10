#nullable enable

namespace NGX.NET;

public struct NGXDLSSDCreateParams
{
    public NGXDLSSDenoiseMode DenoiseMode;

    public NGXDLSSRoughnessMode RoughnessMode;

    public NGXDLSSDepthType UseHWDepth;

    public uint Width;

    public uint Height;

    public uint TargetWidth;

    public uint TargetHeight;

    public NGXPerfQualityValue PerfQualityValue;

    public int FeatureCreateFlags;

    public bool EnableOutputSubrects;

    internal unsafe NGXDLSSDCreateParams(in NGXDLSSDCreateParamsNative native)
    {
        DenoiseMode = native.InDenoiseMode;
        RoughnessMode = native.InRoughnessMode;
        UseHWDepth = native.InUseHWDepth;
        Width = native.InWidth;
        Height = native.InHeight;
        TargetWidth = native.InTargetWidth;
        TargetHeight = native.InTargetHeight;
        PerfQualityValue = native.InPerfQualityValue;
        FeatureCreateFlags = native.InFeatureCreateFlags;
        EnableOutputSubrects = native.InEnableOutputSubrects;
    }
}
