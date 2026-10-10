#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXCUDAFeatureEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public ulong* PInColor;

    [FieldOffset(8)]
    public ulong* PInOutput;

    [FieldOffset(16)]
    public float InSharpness;

    public NGXCUDAFeatureEvalParamsNative(in NGXCUDAFeatureEvalParams value)
    {
        try
        {
            PInColor = value.Color.HasValue ? NGXMarshal.AllocValue(value.Color.GetValueOrDefault()) : null;
            PInOutput = value.Output.HasValue ? NGXMarshal.AllocValue(value.Output.GetValueOrDefault()) : null;
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
        NGXMarshal.Free(PInOutput);
        NGXMarshal.Free(PInColor);
        this = default;
    }
}
