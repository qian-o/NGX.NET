namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal unsafe struct NGXVKDLSSGEvalParamsNative(in NGXVKDLSSGEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PBackbuffer = value.Backbuffer is NGXResourceVK backbuffer ? scope.Alloc(new NGXResourceVKNative(in backbuffer)) : null;

    [FieldOffset(8)]
    public NGXResourceVKNative* PDepth = value.Depth is NGXResourceVK depth ? scope.Alloc(new NGXResourceVKNative(in depth)) : null;

    [FieldOffset(16)]
    public NGXResourceVKNative* PMVecs = value.MVecs is NGXResourceVK mVecs ? scope.Alloc(new NGXResourceVKNative(in mVecs)) : null;

    [FieldOffset(24)]
    public NGXResourceVKNative* PHudless = value.Hudless is NGXResourceVK hudless ? scope.Alloc(new NGXResourceVKNative(in hudless)) : null;

    [FieldOffset(32)]
    public NGXResourceVKNative* PUI = value.UI is NGXResourceVK ui ? scope.Alloc(new NGXResourceVKNative(in ui)) : null;

    [FieldOffset(40)]
    public NGXResourceVKNative* PUIAlpha = value.UIAlpha is NGXResourceVK uiAlpha ? scope.Alloc(new NGXResourceVKNative(in uiAlpha)) : null;

    [FieldOffset(48)]
    public NGXResourceVKNative* PBidirectionalDistortionField = value.BidirectionalDistortionField is NGXResourceVK bidirectionalDistortionField ? scope.Alloc(new NGXResourceVKNative(in bidirectionalDistortionField)) : null;

    [FieldOffset(56)]
    public NGXResourceVKNative* POutputInterpFrame = value.OutputInterpFrame is NGXResourceVK outputInterpFrame ? scope.Alloc(new NGXResourceVKNative(in outputInterpFrame)) : null;

    [FieldOffset(64)]
    public NGXResourceVKNative* POutputRealFrame = value.OutputRealFrame is NGXResourceVK outputRealFrame ? scope.Alloc(new NGXResourceVKNative(in outputRealFrame)) : null;

    [FieldOffset(72)]
    public NGXResourceVKNative* POutputDisableInterpolation = value.OutputDisableInterpolation is NGXResourceVK outputDisableInterpolation ? scope.Alloc(new NGXResourceVKNative(in outputDisableInterpolation)) : null;
}
