#nullable enable

namespace NGX.NET;

public struct NGXPrecisionInfo
{
    public uint IsLowPrecision;

    public float Bias;

    public float Scale;

    internal unsafe NGXPrecisionInfo(in NGXPrecisionInfoNative native)
    {
        IsLowPrecision = native.IsLowPrecision;
        Bias = native.Bias;
        Scale = native.Scale;
    }
}
