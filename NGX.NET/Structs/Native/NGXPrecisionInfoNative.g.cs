#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 12)]
internal unsafe struct NGXPrecisionInfoNative
{
    [FieldOffset(0)]
    public uint IsLowPrecision;

    [FieldOffset(4)]
    public float Bias;

    [FieldOffset(8)]
    public float Scale;

    public NGXPrecisionInfoNative(in NGXPrecisionInfo value)
    {
        IsLowPrecision = value.IsLowPrecision;
        Bias = value.Bias;
        Scale = value.Scale;
    }
}
