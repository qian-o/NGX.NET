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
}
