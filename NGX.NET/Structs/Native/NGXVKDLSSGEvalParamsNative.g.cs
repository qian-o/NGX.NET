#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal unsafe struct NGXVKDLSSGEvalParamsNative
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PBackbuffer;

    [FieldOffset(8)]
    public NGXResourceVKNative* PDepth;

    [FieldOffset(16)]
    public NGXResourceVKNative* PMVecs;

    [FieldOffset(24)]
    public NGXResourceVKNative* PHudless;

    [FieldOffset(32)]
    public NGXResourceVKNative* PUI;

    [FieldOffset(40)]
    public NGXResourceVKNative* PUIAlpha;

    [FieldOffset(48)]
    public NGXResourceVKNative* PBidirectionalDistortionField;

    [FieldOffset(56)]
    public NGXResourceVKNative* POutputInterpFrame;

    [FieldOffset(64)]
    public NGXResourceVKNative* POutputRealFrame;

    [FieldOffset(72)]
    public NGXResourceVKNative* POutputDisableInterpolation;

    public NGXVKDLSSGEvalParamsNative(in NGXVKDLSSGEvalParams value, NativeScope scope)
    {
        PBackbuffer = value.Backbuffer is NGXResourceVK backbuffer ? scope.Alloc(new NGXResourceVKNative(in backbuffer)) : null;
        PDepth = value.Depth is NGXResourceVK depth ? scope.Alloc(new NGXResourceVKNative(in depth)) : null;
        PMVecs = value.MVecs is NGXResourceVK mVecs ? scope.Alloc(new NGXResourceVKNative(in mVecs)) : null;
        PHudless = value.Hudless is NGXResourceVK hudless ? scope.Alloc(new NGXResourceVKNative(in hudless)) : null;
        PUI = value.UI is NGXResourceVK ui ? scope.Alloc(new NGXResourceVKNative(in ui)) : null;
        PUIAlpha = value.UIAlpha is NGXResourceVK uiAlpha ? scope.Alloc(new NGXResourceVKNative(in uiAlpha)) : null;
        PBidirectionalDistortionField = value.BidirectionalDistortionField is NGXResourceVK bidirectionalDistortionField ? scope.Alloc(new NGXResourceVKNative(in bidirectionalDistortionField)) : null;
        POutputInterpFrame = value.OutputInterpFrame is NGXResourceVK outputInterpFrame ? scope.Alloc(new NGXResourceVKNative(in outputInterpFrame)) : null;
        POutputRealFrame = value.OutputRealFrame is NGXResourceVK outputRealFrame ? scope.Alloc(new NGXResourceVKNative(in outputRealFrame)) : null;
        POutputDisableInterpolation = value.OutputDisableInterpolation is NGXResourceVK outputDisableInterpolation ? scope.Alloc(new NGXResourceVKNative(in outputDisableInterpolation)) : null;
    }
}
