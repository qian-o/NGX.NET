namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXD3D12FeatureEvalParamsNative(in NGXD3D12FeatureEvalParams value)
{
    [FieldOffset(0)]
    public nint PInColor = value.Color;

    [FieldOffset(8)]
    public nint PInOutput = value.Output;

    [FieldOffset(16)]
    public float InSharpness = value.Sharpness;
}
