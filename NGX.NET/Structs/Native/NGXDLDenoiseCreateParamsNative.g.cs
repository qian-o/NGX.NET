#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 24)]
internal unsafe struct NGXDLDenoiseCreateParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXFeatureCreateParamsNative Feature;

    [FieldOffset(20)]
    public int InFeatureCreateFlags;

    public NGXDLDenoiseCreateParamsNative(in NGXDLDenoiseCreateParams value)
    {
        try
        {
            Feature = new(in value.Feature);
            InFeatureCreateFlags = value.FeatureCreateFlags;
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
