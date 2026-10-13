namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXPrecisionInfo
{
    public uint IsLowPrecision;

    public float Bias;

    public float Scale;
}
