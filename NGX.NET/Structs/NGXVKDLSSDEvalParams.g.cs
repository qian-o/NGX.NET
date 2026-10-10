#nullable enable

namespace NGX.NET;

public struct NGXVKDLSSDEvalParams
{
    public NGXResourceVK? DiffuseAlbedo;

    public NGXResourceVK? SpecularAlbedo;

    public NGXResourceVK? Normals;

    public NGXResourceVK? Roughness;

    public NGXResourceVK? Color;

    public NGXResourceVK? Alpha;

    public NGXResourceVK? Output;

    public NGXResourceVK? OutputAlpha;

    public NGXResourceVK? Depth;

    public NGXResourceVK? MotionVectors;

    public float JitterOffsetX;

    public float JitterOffsetY;

    public NGXDimensions RenderSubrectDimensions;

    public int Reset;

    public float MVScaleX;

    public float MVScaleY;

    public NGXResourceVK? TransparencyMask;

    public NGXResourceVK? ExposureTexture;

    public NGXResourceVK? BiasCurrentColorMask;

    public NGXCoordinates AlphaSubrectBase;

    public NGXCoordinates OutputAlphaSubrectBase;

    public NGXCoordinates DiffuseAlbedoSubrectBase;

    public NGXCoordinates SpecularAlbedoSubrectBase;

    public NGXCoordinates NormalsSubrectBase;

    public NGXCoordinates RoughnessSubrectBase;

    public NGXCoordinates ColorSubrectBase;

    public NGXCoordinates DepthSubrectBase;

    public NGXCoordinates MVSubrectBase;

    public NGXCoordinates TranslucencySubrectBase;

    public NGXCoordinates BiasCurrentColorSubrectBase;

    public NGXCoordinates OutputSubrectBase;

    public float PreExposure;

    public float ExposureScale;

    public int IndicatorInvertXAxis;

    public int IndicatorInvertYAxis;

    public NGXResourceVK? ReflectedAlbedo;

    public NGXResourceVK? ColorBeforeParticles;

    public NGXResourceVK? ColorAfterParticles;

    public NGXResourceVK? ColorBeforeTransparency;

    public NGXResourceVK? ColorAfterTransparency;

    public NGXResourceVK? ColorBeforeFog;

    public NGXResourceVK? ColorAfterFog;

    public NGXResourceVK? ScreenSpaceSubsurfaceScatteringGuide;

    public NGXResourceVK? ColorBeforeScreenSpaceSubsurfaceScattering;

    public NGXResourceVK? ColorAfterScreenSpaceSubsurfaceScattering;

    public NGXResourceVK? ScreenSpaceRefractionGuide;

    public NGXResourceVK? ColorBeforeScreenSpaceRefraction;

    public NGXResourceVK? ColorAfterScreenSpaceRefraction;

    public NGXResourceVK? DepthOfFieldGuide;

    public NGXResourceVK? ColorBeforeDepthOfField;

    public NGXResourceVK? ColorAfterDepthOfField;

    public NGXResourceVK? DiffuseHitDistance;

    public NGXResourceVK? SpecularHitDistance;

    public NGXResourceVK? DiffuseRayDirection;

    public NGXResourceVK? SpecularRayDirection;

    public NGXResourceVK? DiffuseRayDirectionHitDistance;

    public NGXResourceVK? SpecularRayDirectionHitDistance;

    public NGXCoordinates ReflectedAlbedoSubrectBase;

    public NGXCoordinates ColorBeforeParticlesSubrectBase;

    public NGXCoordinates ColorAfterParticlesSubrectBase;

    public NGXCoordinates ColorBeforeTransparencySubrectBase;

    public NGXCoordinates ColorAfterTransparencySubrectBase;

    public NGXCoordinates ColorBeforeFogSubrectBase;

    public NGXCoordinates ColorAfterFogSubrectBase;

    public NGXCoordinates ScreenSpaceSubsurfaceScatteringGuideSubrectBase;

    public NGXCoordinates ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;

    public NGXCoordinates ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;

    public NGXCoordinates ScreenSpaceRefractionGuideSubrectBase;

    public NGXCoordinates ColorBeforeScreenSpaceRefractionSubrectBase;

    public NGXCoordinates ColorAfterScreenSpaceRefractionSubrectBase;

    public NGXCoordinates DepthOfFieldGuideSubrectBase;

    public NGXCoordinates ColorBeforeDepthOfFieldSubrectBase;

    public NGXCoordinates ColorAfterDepthOfFieldSubrectBase;

    public NGXCoordinates DiffuseHitDistanceSubrectBase;

    public NGXCoordinates SpecularHitDistanceSubrectBase;

    public NGXCoordinates DiffuseRayDirectionSubrectBase;

    public NGXCoordinates SpecularRayDirectionSubrectBase;

    public NGXCoordinates DiffuseRayDirectionHitDistanceSubrectBase;

    public NGXCoordinates SpecularRayDirectionHitDistanceSubrectBase;

    public Matrix4x4? WorldToViewMatrix;

    public Matrix4x4? ViewToClipMatrix;

    public NGXVKGBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public NGXResourceVK? MotionVectors3D;

    public NGXResourceVK? IsParticleMask;

    public NGXResourceVK? AnimatedTextureMask;

    public NGXResourceVK? DepthHighResolution;

    public NGXResourceVK? PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public NGXResourceVK? RayTracingHitDistance;

    public NGXResourceVK? MotionVectorsReflections;

    public NGXResourceVK? TransparencyLayer;

    public NGXCoordinates TransparencyLayerSubrectBase;

    public NGXResourceVK? TransparencyLayerOpacity;

    public NGXCoordinates TransparencyLayerOpacitySubrectBase;

    public NGXResourceVK? TransparencyLayerMvecs;

    public NGXCoordinates TransparencyLayerMvecsSubrectBase;

    public NGXResourceVK? DisocclusionMask;

    public NGXCoordinates DisocclusionMaskSubrectBase;

    public NGXResourceVK? ResponsivityMask;

    public NGXCoordinates ResponsivityMaskSubrectBase;
}
