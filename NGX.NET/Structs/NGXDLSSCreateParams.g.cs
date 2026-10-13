namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXDLSSCreateParams
{
    public NGXFeatureCreateParams Feature;

    public int FeatureCreateFlags;

    public Bool8 EnableOutputSubrects;
}
