#nullable enable

namespace NGX.NET;

public struct NGXD3D11DLISPEvalParams
{
    public NGXD3D11FeatureEvalParams Feature;

    public uint RectX;

    public uint RectY;

    public uint RectW;

    public uint RectH;

    public float Denoise;

    internal unsafe NGXD3D11DLISPEvalParams(in NGXD3D11DLISPEvalParamsNative native)
    {
        Feature = new(in native.Feature);
        RectX = native.InRectX;
        RectY = native.InRectY;
        RectW = native.InRectW;
        RectH = native.InRectH;
        Denoise = native.InDenoise;
    }
}
