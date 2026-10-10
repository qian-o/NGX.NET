namespace NGX.NET;

[Flags]
public enum NGXDLSSFeatureFlags : int
{
    IsInvalid = 1 << 31,

    None = 0,

    IsHdr = 1 << 0,

    MvLowRes = 1 << 1,

    MvJittered = 1 << 2,

    DepthInverted = 1 << 3,

    Reserved0 = 1 << 4,

    DoSharpening = 1 << 5,

    AutoExposure = 1 << 6,

    AlphaUpscaling = 1 << 7,

    Reserved8 = 1 << 8,
}
