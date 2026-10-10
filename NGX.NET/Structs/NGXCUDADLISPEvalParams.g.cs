#nullable enable

namespace NGX.NET;

public struct NGXCUDADLISPEvalParams
{
    public NGXCUDAFeatureEvalParams Feature;

    public uint RectX;

    public uint RectY;

    public uint RectW;

    public uint RectH;

    public float Denoise;

    internal unsafe NGXCUDADLISPEvalParams(in NGXCUDADLISPEvalParamsNative native)
    {
        Feature = new(in native.Feature);
        RectX = native.InRectX;
        RectY = native.InRectY;
        RectW = native.InRectW;
        RectH = native.InRectH;
        Denoise = native.InDenoise;
    }
}
