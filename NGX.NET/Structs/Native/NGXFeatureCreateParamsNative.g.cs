namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 20)]
internal unsafe struct NGXFeatureCreateParamsNative(in NGXFeatureCreateParams value)
{
    [FieldOffset(0)]
    public uint InWidth = value.Width;

    [FieldOffset(4)]
    public uint InHeight = value.Height;

    [FieldOffset(8)]
    public uint InTargetWidth = value.TargetWidth;

    [FieldOffset(12)]
    public uint InTargetHeight = value.TargetHeight;

    [FieldOffset(16)]
    public NGXPerfQualityValue InPerfQualityValue = value.PerfQualityValue;
}
