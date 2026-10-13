namespace Showcase.Models;

internal enum ComputePass
{
    AtmosphereTransmittance,

    AtmosphereMultipleScattering,

    AtmosphereGroundIrradiance,

    AtmosphereSky,

    AtmosphereDistribution,

    AtmosphereAerial,

    PathTrace,

    AutoExposure,

    ToneMap,

    CopyDisplay,

    Composite
}
