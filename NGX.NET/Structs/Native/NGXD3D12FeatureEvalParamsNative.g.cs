#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXD3D12FeatureEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public nint PInColor;

    [FieldOffset(8)]
    public nint PInOutput;

    [FieldOffset(16)]
    public float InSharpness;

    public NGXD3D12FeatureEvalParamsNative(in NGXD3D12FeatureEvalParams value)
    {
        try
        {
            PInColor = value.Color;
            PInOutput = value.Output;
            InSharpness = value.Sharpness;
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
