#nullable enable

namespace NGX.NET;

public struct NGXD3D11FeatureEvalParams
{
    public nint Color;

    public nint Output;

    public float Sharpness;

    internal unsafe NGXD3D11FeatureEvalParams(in NGXD3D11FeatureEvalParamsNative native)
    {
        Color = native.PInColor;
        Output = native.PInOutput;
        Sharpness = native.InSharpness;
    }
}
