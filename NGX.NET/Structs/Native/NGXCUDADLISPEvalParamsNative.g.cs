namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXCUDADLISPEvalParamsNative(in NGXCUDADLISPEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXCUDAFeatureEvalParamsNative Feature = new(in value.Feature, scope);

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
