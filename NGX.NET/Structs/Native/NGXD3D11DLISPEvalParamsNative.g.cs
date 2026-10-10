#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXD3D11DLISPEvalParamsNative
{
    [FieldOffset(0)]
    public NGXD3D11FeatureEvalParamsNative Feature;

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

    public NGXD3D11DLISPEvalParamsNative(in NGXD3D11DLISPEvalParams value)
    {
        Feature = new(in value.Feature);
        InRectX = value.RectX;
        InRectY = value.RectY;
        InRectW = value.RectW;
        InRectH = value.RectH;
        InDenoise = value.Denoise;
    }
}
