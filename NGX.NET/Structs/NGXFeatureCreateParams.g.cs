#nullable enable

namespace NGX.NET;

public struct NGXFeatureCreateParams
{
    public uint Width;

    public uint Height;

    public uint TargetWidth;

    public uint TargetHeight;

    public NGXPerfQualityValue PerfQualityValue;
}
