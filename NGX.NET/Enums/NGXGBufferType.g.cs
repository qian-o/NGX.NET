#nullable enable

namespace NGX.NET;

public enum NGXGBufferType : int
{
    GbufferAlbedo = 0,

    GbufferRoughness = 1,

    GbufferMetallic = 2,

    GbufferSpecular = 3,

    GbufferSubsurface = 4,

    GbufferNormals = 5,

    GbufferShadingmodelid = 6,

    GbufferMaterialid = 7,

    GbufferSpecularAlbedo = 8,

    GbufferIndirectAlbedo = 9,

    GbufferSpecularMvec = 10,

    GbufferDisocclMask = 11,

    GbufferEmissive = 12,

    GbufferResponsivityMask = 13,

    Num = 17,
}
