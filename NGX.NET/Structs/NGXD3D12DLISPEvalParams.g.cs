namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXD3D12DLISPEvalParams
{
    public NGXD3D12FeatureEvalParams Feature;

    public uint RectX;

    public uint RectY;

    public uint RectW;

    public uint RectH;

    public float Denoise;
}
