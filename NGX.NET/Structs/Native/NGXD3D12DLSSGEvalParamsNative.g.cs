namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal unsafe struct NGXD3D12DLSSGEvalParamsNative(in NGXD3D12DLSSGEvalParams value)
{
    [FieldOffset(0)]
    public nint PBackbuffer = value.Backbuffer;

    [FieldOffset(8)]
    public nint PDepth = value.Depth;

    [FieldOffset(16)]
    public nint PMVecs = value.MVecs;

    [FieldOffset(24)]
    public nint PHudless = value.Hudless;

    [FieldOffset(32)]
    public nint PUI = value.UI;

    [FieldOffset(40)]
    public nint PUIAlpha = value.UIAlpha;

    [FieldOffset(48)]
    public nint PBidirectionalDistortionField = value.BidirectionalDistortionField;

    [FieldOffset(56)]
    public nint POutputInterpFrame = value.OutputInterpFrame;

    [FieldOffset(64)]
    public nint POutputRealFrame = value.OutputRealFrame;

    [FieldOffset(72)]
    public nint POutputDisableInterpolation = value.OutputDisableInterpolation;
}
