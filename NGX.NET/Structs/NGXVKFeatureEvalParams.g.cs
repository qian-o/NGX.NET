#nullable enable

namespace NGX.NET;

public struct NGXVKFeatureEvalParams
{
    public NGXResourceVK? Color;

    public NGXResourceVK? Output;

    public float Sharpness;

    internal unsafe NGXVKFeatureEvalParams(in NGXVKFeatureEvalParamsNative native)
    {
        Color = native.PInColor is null ? null : new NGXResourceVK(in *native.PInColor);
        Output = native.PInOutput is null ? null : new NGXResourceVK(in *native.PInOutput);
        Sharpness = native.InSharpness;
    }
}
