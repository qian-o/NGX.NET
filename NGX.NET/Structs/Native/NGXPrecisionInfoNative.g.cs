#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 12)]
internal unsafe struct NGXPrecisionInfoNative : IDisposable
{
    [FieldOffset(0)]
    public uint IsLowPrecision;

    [FieldOffset(4)]
    public float Bias;

    [FieldOffset(8)]
    public float Scale;

    public NGXPrecisionInfoNative(in NGXPrecisionInfo value)
    {
        try
        {
            IsLowPrecision = value.IsLowPrecision;
            Bias = value.Bias;
            Scale = value.Scale;
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
