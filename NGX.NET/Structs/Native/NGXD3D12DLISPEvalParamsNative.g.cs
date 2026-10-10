namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXD3D12DLISPEvalParamsNative(in NGXD3D12DLISPEvalParams value)
{
    [FieldOffset(0)]
    public NGXD3D12FeatureEvalParamsNative Feature = new(in value.Feature);

    [FieldOffset(24)]
    public uint InRectX = value.RectX;

    [FieldOffset(28)]
    public uint InRectY = value.RectY;

    [FieldOffset(32)]
    public uint InRectW = value.RectW;

    [FieldOffset(36)]
    public uint InRectH = value.RectH;

    [FieldOffset(40)]
    public float InDenoise = value.Denoise;
}
