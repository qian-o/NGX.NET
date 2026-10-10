#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal unsafe struct NGXDLSSDCreateParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXDLSSDenoiseMode InDenoiseMode;

    [FieldOffset(4)]
    public NGXDLSSRoughnessMode InRoughnessMode;

    [FieldOffset(8)]
    public NGXDLSSDepthType InUseHWDepth;

    [FieldOffset(12)]
    public uint InWidth;

    [FieldOffset(16)]
    public uint InHeight;

    [FieldOffset(20)]
    public uint InTargetWidth;

    [FieldOffset(24)]
    public uint InTargetHeight;

    [FieldOffset(28)]
    public NGXPerfQualityValue InPerfQualityValue;

    [FieldOffset(32)]
    public int InFeatureCreateFlags;

    [FieldOffset(36)]
    public Bool8 InEnableOutputSubrects;

    public NGXDLSSDCreateParamsNative(in NGXDLSSDCreateParams value)
    {
        try
        {
            InDenoiseMode = value.DenoiseMode;
            InRoughnessMode = value.RoughnessMode;
            InUseHWDepth = value.UseHWDepth;
            InWidth = value.Width;
            InHeight = value.Height;
            InTargetWidth = value.TargetWidth;
            InTargetHeight = value.TargetHeight;
            InPerfQualityValue = value.PerfQualityValue;
            InFeatureCreateFlags = value.FeatureCreateFlags;
            InEnableOutputSubrects = value.EnableOutputSubrects;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
