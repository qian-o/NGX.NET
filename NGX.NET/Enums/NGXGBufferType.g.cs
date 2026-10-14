namespace NGX.NET;

public enum NGXGBufferType : int
{
    Albedo = 0,

    Roughness = 1,

    Metallic = 2,

    Specular = 3,

    Subsurface = 4,

    Normals = 5,

    ShadingModelId = 6,

    MaterialId = 7,

    SpecularAlbedo = 8,

    IndirectAlbedo = 9,

    SpecularMvec = 10,

    DisocclMask = 11,

    Emissive = 12,

    ResponsivityMask = 13,

    Num = 17,
}
