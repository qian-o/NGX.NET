#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 80)]
internal unsafe struct NGXVKDLSSGEvalParamsNative : IDisposable
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

    public NGXVKDLSSGEvalParamsNative(in NGXVKDLSSGEvalParams value)
    {
        this = default;

        try
        {
            if (value.Backbuffer is NGXResourceVK backbuffer)
            {
                PBackbuffer = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in backbuffer));
            }

            if (value.Depth is NGXResourceVK depth)
            {
                PDepth = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in depth));
            }

            if (value.MVecs is NGXResourceVK mVecs)
            {
                PMVecs = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in mVecs));
            }

            if (value.Hudless is NGXResourceVK hudless)
            {
                PHudless = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in hudless));
            }

            if (value.UI is NGXResourceVK ui)
            {
                PUI = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in ui));
            }

            if (value.UIAlpha is NGXResourceVK uiAlpha)
            {
                PUIAlpha = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in uiAlpha));
            }

            if (value.BidirectionalDistortionField is NGXResourceVK bidirectionalDistortionField)
            {
                PBidirectionalDistortionField = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in bidirectionalDistortionField));
            }

            if (value.OutputInterpFrame is NGXResourceVK outputInterpFrame)
            {
                POutputInterpFrame = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in outputInterpFrame));
            }

            if (value.OutputRealFrame is NGXResourceVK outputRealFrame)
            {
                POutputRealFrame = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in outputRealFrame));
            }

            if (value.OutputDisableInterpolation is NGXResourceVK outputDisableInterpolation)
            {
                POutputDisableInterpolation = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in outputDisableInterpolation));
            }
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NGXMarshal.FreeNative(POutputDisableInterpolation);
        NGXMarshal.FreeNative(POutputRealFrame);
        NGXMarshal.FreeNative(POutputInterpFrame);
        NGXMarshal.FreeNative(PBidirectionalDistortionField);
        NGXMarshal.FreeNative(PUIAlpha);
        NGXMarshal.FreeNative(PUI);
        NGXMarshal.FreeNative(PHudless);
        NGXMarshal.FreeNative(PMVecs);
        NGXMarshal.FreeNative(PDepth);
        NGXMarshal.FreeNative(PBackbuffer);
        this = default;
    }
}
