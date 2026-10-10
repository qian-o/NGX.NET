#nullable enable

namespace NGX.NET;

public struct NGXDLDenoiseCreateParams
{
    public NGXFeatureCreateParams Feature;

    public int FeatureCreateFlags;

    internal unsafe NGXDLDenoiseCreateParams(in NGXDLDenoiseCreateParamsNative native)
    {
        Feature = new(in native.Feature);
        FeatureCreateFlags = native.InFeatureCreateFlags;
    }
}
