#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal unsafe struct NGXD3D12DLSSGEvalParamsNative
{
    [FieldOffset(0)]
    public nint PBackbuffer;

    [FieldOffset(8)]
    public nint PDepth;

    [FieldOffset(16)]
    public nint PMVecs;

    [FieldOffset(24)]
    public nint PHudless;

    [FieldOffset(32)]
    public nint PUI;

    [FieldOffset(40)]
    public nint PUIAlpha;

    [FieldOffset(48)]
    public nint PBidirectionalDistortionField;

    [FieldOffset(56)]
    public nint POutputInterpFrame;

    [FieldOffset(64)]
    public nint POutputRealFrame;

    [FieldOffset(72)]
    public nint POutputDisableInterpolation;

    public NGXD3D12DLSSGEvalParamsNative(in NGXD3D12DLSSGEvalParams value)
    {
        PBackbuffer = value.Backbuffer;
        PDepth = value.Depth;
        PMVecs = value.MVecs;
        PHudless = value.Hudless;
        PUI = value.UI;
        PUIAlpha = value.UIAlpha;
        PBidirectionalDistortionField = value.BidirectionalDistortionField;
        POutputInterpFrame = value.OutputInterpFrame;
        POutputRealFrame = value.OutputRealFrame;
        POutputDisableInterpolation = value.OutputDisableInterpolation;
    }
}
