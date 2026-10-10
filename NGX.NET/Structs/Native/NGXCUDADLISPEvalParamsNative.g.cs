#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXCUDADLISPEvalParamsNative
{
    [FieldOffset(0)]
    public NGXCUDAFeatureEvalParamsNative Feature;

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

    public NGXCUDADLISPEvalParamsNative(in NGXCUDADLISPEvalParams value, NativeScope scope)
    {
        Feature = new(in value.Feature, scope);
        InRectX = value.RectX;
        InRectY = value.RectY;
        InRectW = value.RectW;
        InRectH = value.RectH;
        InDenoise = value.Denoise;
    }
}
