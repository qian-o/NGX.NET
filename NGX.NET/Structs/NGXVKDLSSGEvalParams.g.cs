#nullable enable

namespace NGX.NET;

public struct NGXVKDLSSGEvalParams
{
    public NGXResourceVK? Backbuffer;

    public NGXResourceVK? Depth;

    public NGXResourceVK? MVecs;

    public NGXResourceVK? Hudless;

    public NGXResourceVK? UI;

    public NGXResourceVK? UIAlpha;

    public NGXResourceVK? BidirectionalDistortionField;

    public NGXResourceVK? OutputInterpFrame;

    public NGXResourceVK? OutputRealFrame;

    public NGXResourceVK? OutputDisableInterpolation;

    internal unsafe NGXVKDLSSGEvalParams(in NGXVKDLSSGEvalParamsNative native)
    {
        Backbuffer = native.PBackbuffer is null ? null : new NGXResourceVK(in *native.PBackbuffer);
        Depth = native.PDepth is null ? null : new NGXResourceVK(in *native.PDepth);
        MVecs = native.PMVecs is null ? null : new NGXResourceVK(in *native.PMVecs);
        Hudless = native.PHudless is null ? null : new NGXResourceVK(in *native.PHudless);
        UI = native.PUI is null ? null : new NGXResourceVK(in *native.PUI);
        UIAlpha = native.PUIAlpha is null ? null : new NGXResourceVK(in *native.PUIAlpha);
        BidirectionalDistortionField = native.PBidirectionalDistortionField is null ? null : new NGXResourceVK(in *native.PBidirectionalDistortionField);
        OutputInterpFrame = native.POutputInterpFrame is null ? null : new NGXResourceVK(in *native.POutputInterpFrame);
        OutputRealFrame = native.POutputRealFrame is null ? null : new NGXResourceVK(in *native.POutputRealFrame);
        OutputDisableInterpolation = native.POutputDisableInterpolation is null ? null : new NGXResourceVK(in *native.POutputDisableInterpolation);
    }
}
