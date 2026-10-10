namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal unsafe struct NGXDLSSDCreateParamsNative(in NGXDLSSDCreateParams value)
{
    [FieldOffset(0)]
    public NGXDLSSDenoiseMode InDenoiseMode = value.DenoiseMode;

    [FieldOffset(4)]
    public NGXDLSSRoughnessMode InRoughnessMode = value.RoughnessMode;

    [FieldOffset(8)]
    public NGXDLSSDepthType InUseHWDepth = value.UseHWDepth;

    [FieldOffset(12)]
    public uint InWidth = value.Width;

    [FieldOffset(16)]
    public uint InHeight = value.Height;

    [FieldOffset(20)]
    public uint InTargetWidth = value.TargetWidth;

    [FieldOffset(24)]
    public uint InTargetHeight = value.TargetHeight;

    [FieldOffset(28)]
    public NGXPerfQualityValue InPerfQualityValue = value.PerfQualityValue;

    [FieldOffset(32)]
    public int InFeatureCreateFlags = value.FeatureCreateFlags;

    [FieldOffset(36)]
    public Bool8 InEnableOutputSubrects = value.EnableOutputSubrects;
}
