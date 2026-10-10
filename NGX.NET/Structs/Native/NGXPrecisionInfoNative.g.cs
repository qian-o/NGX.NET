namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 12)]
internal unsafe struct NGXPrecisionInfoNative(in NGXPrecisionInfo value)
{
    [FieldOffset(0)]
    public uint IsLowPrecision = value.IsLowPrecision;

    [FieldOffset(4)]
    public float Bias = value.Bias;

    [FieldOffset(8)]
    public float Scale = value.Scale;
}
