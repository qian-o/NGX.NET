namespace Showcase.Models;

internal enum ComputePass
{
    TraceLighting,

    Lighting,

    PrepareLuminance,

    FilterLuminance,

    MeterExposure,

    ToneMap,

    NativeResolve,

    CopyDisplay,

    Composite
}
