namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 28)]
internal unsafe struct NGXDLSSCreateParamsNative(in NGXDLSSCreateParams value)
{
    [FieldOffset(0)]
    public NGXFeatureCreateParamsNative Feature = new(in value.Feature);

    [FieldOffset(20)]
    public int InFeatureCreateFlags = value.FeatureCreateFlags;

    [FieldOffset(24)]
    public Bool8 InEnableOutputSubrects = value.EnableOutputSubrects;
}
