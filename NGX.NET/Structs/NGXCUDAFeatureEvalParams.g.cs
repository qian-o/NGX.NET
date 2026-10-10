#nullable enable

namespace NGX.NET;

public struct NGXCUDAFeatureEvalParams
{
    public ulong? Color;

    public ulong? Output;

    public float Sharpness;

    internal unsafe NGXCUDAFeatureEvalParams(in NGXCUDAFeatureEvalParamsNative native)
    {
        Color = native.PInColor is null ? null : *native.PInColor;
        Output = native.PInOutput is null ? null : *native.PInOutput;
        Sharpness = native.InSharpness;
    }
}
