namespace NGX.NET;

[Flags]
public enum NGXDLSSGResourceFlags : int
{
    Backbuffer = 1 << 0,

    MVecs = 1 << 1,

    Depth = 1 << 2,

    HudLess = 1 << 3,

    Ui = 1 << 4,

    BidirectionalDistortionField = 1 << 5,

    Reserved6 = 1 << 6,

    UiAlpha = 1 << 7,

    OutputInterpolated = 1 << 24,

    OutputReal = 1 << 25,

    OutputDisableInterpolation = 1 << 26,

    None = 0
}
