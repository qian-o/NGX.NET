#nullable enable

namespace NGX.NET;

public struct NGXFeatureCreateParams
{
    public uint Width;

    public uint Height;

    public uint TargetWidth;

    public uint TargetHeight;

    public NGXPerfQualityValue PerfQualityValue;

    internal unsafe NGXFeatureCreateParams(in NGXFeatureCreateParamsNative native)
    {
        Width = native.InWidth;
        Height = native.InHeight;
        TargetWidth = native.InTargetWidth;
        TargetHeight = native.InTargetHeight;
        PerfQualityValue = native.InPerfQualityValue;
    }
}
