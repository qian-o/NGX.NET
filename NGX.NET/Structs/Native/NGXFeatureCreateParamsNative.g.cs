#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 20)]
internal unsafe struct NGXFeatureCreateParamsNative
{
    [FieldOffset(0)]
    public uint InWidth;

    [FieldOffset(4)]
    public uint InHeight;

    [FieldOffset(8)]
    public uint InTargetWidth;

    [FieldOffset(12)]
    public uint InTargetHeight;

    [FieldOffset(16)]
    public NGXPerfQualityValue InPerfQualityValue;

    public NGXFeatureCreateParamsNative(in NGXFeatureCreateParams value)
    {
        InWidth = value.Width;
        InHeight = value.Height;
        InTargetWidth = value.TargetWidth;
        InTargetHeight = value.TargetHeight;
        InPerfQualityValue = value.PerfQualityValue;
    }
}
