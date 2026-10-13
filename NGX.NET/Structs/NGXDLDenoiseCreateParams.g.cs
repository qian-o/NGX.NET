namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXDLDenoiseCreateParams
{
    public NGXFeatureCreateParams Feature;

    public int FeatureCreateFlags;
}
