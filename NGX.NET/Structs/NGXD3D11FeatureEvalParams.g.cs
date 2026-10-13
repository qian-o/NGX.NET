namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXD3D11FeatureEvalParams
{
    public nint Color;

    public nint Output;

    public float Sharpness;
}
