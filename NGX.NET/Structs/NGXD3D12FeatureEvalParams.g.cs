#nullable enable

namespace NGX.NET;

public struct NGXD3D12FeatureEvalParams
{
    public nint Color;

    public nint Output;

    public float Sharpness;

    internal unsafe NGXD3D12FeatureEvalParams(in NGXD3D12FeatureEvalParamsNative native)
    {
        Color = native.PInColor;
        Output = native.PInOutput;
        Sharpness = native.InSharpness;
    }
}
