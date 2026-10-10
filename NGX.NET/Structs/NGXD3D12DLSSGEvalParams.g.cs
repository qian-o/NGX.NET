#nullable enable

namespace NGX.NET;

public struct NGXD3D12DLSSGEvalParams
{
    public nint Backbuffer;

    public nint Depth;

    public nint MVecs;

    public nint Hudless;

    public nint UI;

    public nint UIAlpha;

    public nint BidirectionalDistortionField;

    public nint OutputInterpFrame;

    public nint OutputRealFrame;

    public nint OutputDisableInterpolation;

    internal unsafe NGXD3D12DLSSGEvalParams(in NGXD3D12DLSSGEvalParamsNative native)
    {
        Backbuffer = native.PBackbuffer;
        Depth = native.PDepth;
        MVecs = native.PMVecs;
        Hudless = native.PHudless;
        UI = native.PUI;
        UIAlpha = native.PUIAlpha;
        BidirectionalDistortionField = native.PBidirectionalDistortionField;
        OutputInterpFrame = native.POutputInterpFrame;
        OutputRealFrame = native.POutputRealFrame;
        OutputDisableInterpolation = native.POutputDisableInterpolation;
    }
}
