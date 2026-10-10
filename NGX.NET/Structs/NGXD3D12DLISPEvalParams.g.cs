#nullable enable

namespace NGX.NET;

public struct NGXD3D12DLISPEvalParams
{
    public NGXD3D12FeatureEvalParams Feature;

    public uint RectX;

    public uint RectY;

    public uint RectW;

    public uint RectH;

    public float Denoise;

    internal unsafe NGXD3D12DLISPEvalParams(in NGXD3D12DLISPEvalParamsNative native)
    {
        Feature = new(in native.Feature);
        RectX = native.InRectX;
        RectY = native.InRectY;
        RectW = native.InRectW;
        RectH = native.InRectH;
        Denoise = native.InDenoise;
    }
}
