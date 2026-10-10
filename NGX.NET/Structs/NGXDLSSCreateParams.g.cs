#nullable enable

namespace NGX.NET;

public struct NGXDLSSCreateParams
{
    public NGXFeatureCreateParams Feature;

    public int FeatureCreateFlags;

    public bool EnableOutputSubrects;

    internal unsafe NGXDLSSCreateParams(in NGXDLSSCreateParamsNative native)
    {
        Feature = new(in native.Feature);
        FeatureCreateFlags = native.InFeatureCreateFlags;
        EnableOutputSubrects = native.InEnableOutputSubrects;
    }
}
