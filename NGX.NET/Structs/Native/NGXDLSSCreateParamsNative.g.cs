#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 28)]
internal unsafe struct NGXDLSSCreateParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXFeatureCreateParamsNative Feature;

    [FieldOffset(20)]
    public int InFeatureCreateFlags;

    [FieldOffset(24)]
    public Bool8 InEnableOutputSubrects;

    public NGXDLSSCreateParamsNative(in NGXDLSSCreateParams value)
    {
        try
        {
            Feature = new(in value.Feature);
            InFeatureCreateFlags = value.FeatureCreateFlags;
            InEnableOutputSubrects = value.EnableOutputSubrects;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        Feature.Dispose();
        this = default;
    }
}
