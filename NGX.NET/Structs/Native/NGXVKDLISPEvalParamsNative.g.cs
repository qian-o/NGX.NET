#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXVKDLISPEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXVKFeatureEvalParamsNative Feature;

    [FieldOffset(24)]
    public uint InRectX;

    [FieldOffset(28)]
    public uint InRectY;

    [FieldOffset(32)]
    public uint InRectW;

    [FieldOffset(36)]
    public uint InRectH;

    [FieldOffset(40)]
    public float InDenoise;

    public NGXVKDLISPEvalParamsNative(in NGXVKDLISPEvalParams value)
    {
        try
        {
            Feature = new(in value.Feature);
            InRectX = value.RectX;
            InRectY = value.RectY;
            InRectW = value.RectW;
            InRectH = value.RectH;
            InDenoise = value.Denoise;
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
