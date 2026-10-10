#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXCUDAFeatureEvalParamsNative
{
    [FieldOffset(0)]
    public ulong* PInColor;

    [FieldOffset(8)]
    public ulong* PInOutput;

    [FieldOffset(16)]
    public float InSharpness;

    public NGXCUDAFeatureEvalParamsNative(in NGXCUDAFeatureEvalParams value, NativeScope scope)
    {
        PInColor = value.Color.HasValue ? scope.Alloc(value.Color.GetValueOrDefault()) : null;
        PInOutput = value.Output.HasValue ? scope.Alloc(value.Output.GetValueOrDefault()) : null;
        InSharpness = value.Sharpness;
    }
}
