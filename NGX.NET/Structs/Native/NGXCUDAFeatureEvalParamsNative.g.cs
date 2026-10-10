namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXCUDAFeatureEvalParamsNative(in NGXCUDAFeatureEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public ulong* PInColor = value.Color.HasValue ? scope.Alloc(value.Color.GetValueOrDefault()) : null;

    [FieldOffset(8)]
    public ulong* PInOutput = value.Output.HasValue ? scope.Alloc(value.Output.GetValueOrDefault()) : null;

    [FieldOffset(16)]
    public float InSharpness = value.Sharpness;
}
