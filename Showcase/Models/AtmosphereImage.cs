namespace Showcase.Models;

// Shared atmosphere tables, ordered identically in the shader SRV/UAV bindings.
internal enum AtmosphereImage
{
    Transmittance,

    MultipleScattering,

    SkyView,

    SkyDistribution,

    AerialScattering,

    AerialTransmittance,

    GroundIrradiance,

    Count
}
