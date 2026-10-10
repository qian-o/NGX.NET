#nullable enable

namespace NGX.NET;

public struct NGXD3D12DLSSDEvalParams
{
    public nint DiffuseAlbedo;

    public nint SpecularAlbedo;

    public nint Normals;

    public nint Roughness;

    public nint Color;

    public nint Alpha;

    public nint Output;

    public nint OutputAlpha;

    public nint Depth;

    public nint MotionVectors;

    public float JitterOffsetX;

    public float JitterOffsetY;

    public NGXDimensions RenderSubrectDimensions;

    public int Reset;

    public float MVScaleX;

    public float MVScaleY;

    public nint TransparencyMask;

    public nint ExposureTexture;

    public nint BiasCurrentColorMask;

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

    public nint ReflectedAlbedo;

    public nint ColorBeforeParticles;

    public nint ColorAfterParticles;

    public nint ColorBeforeTransparency;

    public nint ColorAfterTransparency;

    public nint ColorBeforeFog;

    public nint ColorAfterFog;

    public nint ScreenSpaceSubsurfaceScatteringGuide;

    public nint ColorBeforeScreenSpaceSubsurfaceScattering;

    public nint ColorAfterScreenSpaceSubsurfaceScattering;

    public nint ScreenSpaceRefractionGuide;

    public nint ColorBeforeScreenSpaceRefraction;

    public nint ColorAfterScreenSpaceRefraction;

    public nint DepthOfFieldGuide;

    public nint ColorBeforeDepthOfField;

    public nint ColorAfterDepthOfField;

    public nint DiffuseHitDistance;

    public nint SpecularHitDistance;

    public nint DiffuseRayDirection;

    public nint SpecularRayDirection;

    public nint DiffuseRayDirectionHitDistance;

    public nint SpecularRayDirectionHitDistance;

    public NGXCoordinates ReflectedAlbedoSubrectBase;

    public NGXCoordinates ColorBeforeParticlesSubrectBase;

    public NGXCoordinates ColorAfterParticlesSubrectBase;

    public NGXCoordinates ColorBeforeTransparencySubrectBase;

    public NGXCoordinates ColorAfterTransparencySubrectBase;

    public NGXCoordinates ColorBeforeFogSubrectBase;

    public NGXCoordinates ColorAfterFogSubrectBase;

    public NGXCoordinates ScreenSpaceSubsurfaceScatteringGuideSubrectBase;

    public NGXCoordinates ScreenSpaceRefractionGuideSubrectBase;

    public NGXCoordinates DepthOfFieldGuideSubrectBase;

    public NGXCoordinates DiffuseHitDistanceSubrectBase;

    public NGXCoordinates SpecularHitDistanceSubrectBase;

    public NGXCoordinates DiffuseRayDirectionSubrectBase;

    public NGXCoordinates SpecularRayDirectionSubrectBase;

    public NGXCoordinates DiffuseRayDirectionHitDistanceSubrectBase;

    public NGXCoordinates SpecularRayDirectionHitDistanceSubrectBase;

    public NGXCoordinates ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;

    public NGXCoordinates ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;

    public NGXCoordinates ColorBeforeScreenSpaceRefractionSubrectBase;

    public NGXCoordinates ColorAfterScreenSpaceRefractionSubrectBase;

    public NGXCoordinates ColorBeforeDepthOfFieldSubrectBase;

    public NGXCoordinates ColorAfterDepthOfFieldSubtectBase;

    public Matrix4x4? WorldToViewMatrix;

    public Matrix4x4? ViewToClipMatrix;

    public float PreExposure;

    public float ExposureScale;

    public int IndicatorInvertXAxis;

    public int IndicatorInvertYAxis;

    public NGXD3D12GBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public nint MotionVectors3D;

    public nint IsParticleMask;

    public nint AnimatedTextureMask;

    public nint DepthHighResolution;

    public nint PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public nint RayTracingHitDistance;

    public nint MotionVectorsReflections;

    public nint TransparencyLayer;

    public NGXCoordinates TransparencyLayerSubrectBase;

    public nint TransparencyLayerOpacity;

    public NGXCoordinates TransparencyLayerOpacitySubrectBase;

    public nint TransparencyLayerMvecs;

    public NGXCoordinates TransparencyLayerMvecsSubrectBase;

    public nint DisocclusionMask;

    public NGXCoordinates DisocclusionMaskSubrectBase;

    public nint ResponsivityMask;

    public NGXCoordinates ResponsivityMaskSubrectBase;

    internal unsafe NGXD3D12DLSSDEvalParams(in NGXD3D12DLSSDEvalParamsNative native)
    {
        DiffuseAlbedo = native.PInDiffuseAlbedo;
        SpecularAlbedo = native.PInSpecularAlbedo;
        Normals = native.PInNormals;
        Roughness = native.PInRoughness;
        Color = native.PInColor;
        Alpha = native.PInAlpha;
        Output = native.PInOutput;
        OutputAlpha = native.PInOutputAlpha;
        Depth = native.PInDepth;
        MotionVectors = native.PInMotionVectors;
        JitterOffsetX = native.InJitterOffsetX;
        JitterOffsetY = native.InJitterOffsetY;
        RenderSubrectDimensions = new(in native.InRenderSubrectDimensions);
        Reset = native.InReset;
        MVScaleX = native.InMVScaleX;
        MVScaleY = native.InMVScaleY;
        TransparencyMask = native.PInTransparencyMask;
        ExposureTexture = native.PInExposureTexture;
        BiasCurrentColorMask = native.PInBiasCurrentColorMask;
        AlphaSubrectBase = new(in native.InAlphaSubrectBase);
        OutputAlphaSubrectBase = new(in native.InOutputAlphaSubrectBase);
        DiffuseAlbedoSubrectBase = new(in native.InDiffuseAlbedoSubrectBase);
        SpecularAlbedoSubrectBase = new(in native.InSpecularAlbedoSubrectBase);
        NormalsSubrectBase = new(in native.InNormalsSubrectBase);
        RoughnessSubrectBase = new(in native.InRoughnessSubrectBase);
        ColorSubrectBase = new(in native.InColorSubrectBase);
        DepthSubrectBase = new(in native.InDepthSubrectBase);
        MVSubrectBase = new(in native.InMVSubrectBase);
        TranslucencySubrectBase = new(in native.InTranslucencySubrectBase);
        BiasCurrentColorSubrectBase = new(in native.InBiasCurrentColorSubrectBase);
        OutputSubrectBase = new(in native.InOutputSubrectBase);
        ReflectedAlbedo = native.PInReflectedAlbedo;
        ColorBeforeParticles = native.PInColorBeforeParticles;
        ColorAfterParticles = native.PInColorAfterParticles;
        ColorBeforeTransparency = native.PInColorBeforeTransparency;
        ColorAfterTransparency = native.PInColorAfterTransparency;
        ColorBeforeFog = native.PInColorBeforeFog;
        ColorAfterFog = native.PInColorAfterFog;
        ScreenSpaceSubsurfaceScatteringGuide = native.PInScreenSpaceSubsurfaceScatteringGuide;
        ColorBeforeScreenSpaceSubsurfaceScattering = native.PInColorBeforeScreenSpaceSubsurfaceScattering;
        ColorAfterScreenSpaceSubsurfaceScattering = native.PInColorAfterScreenSpaceSubsurfaceScattering;
        ScreenSpaceRefractionGuide = native.PInScreenSpaceRefractionGuide;
        ColorBeforeScreenSpaceRefraction = native.PInColorBeforeScreenSpaceRefraction;
        ColorAfterScreenSpaceRefraction = native.PInColorAfterScreenSpaceRefraction;
        DepthOfFieldGuide = native.PInDepthOfFieldGuide;
        ColorBeforeDepthOfField = native.PInColorBeforeDepthOfField;
        ColorAfterDepthOfField = native.PInColorAfterDepthOfField;
        DiffuseHitDistance = native.PInDiffuseHitDistance;
        SpecularHitDistance = native.PInSpecularHitDistance;
        DiffuseRayDirection = native.PInDiffuseRayDirection;
        SpecularRayDirection = native.PInSpecularRayDirection;
        DiffuseRayDirectionHitDistance = native.PInDiffuseRayDirectionHitDistance;
        SpecularRayDirectionHitDistance = native.PInSpecularRayDirectionHitDistance;
        ReflectedAlbedoSubrectBase = new(in native.InReflectedAlbedoSubrectBase);
        ColorBeforeParticlesSubrectBase = new(in native.InColorBeforeParticlesSubrectBase);
        ColorAfterParticlesSubrectBase = new(in native.InColorAfterParticlesSubrectBase);
        ColorBeforeTransparencySubrectBase = new(in native.InColorBeforeTransparencySubrectBase);
        ColorAfterTransparencySubrectBase = new(in native.InColorAfterTransparencySubrectBase);
        ColorBeforeFogSubrectBase = new(in native.InColorBeforeFogSubrectBase);
        ColorAfterFogSubrectBase = new(in native.InColorAfterFogSubrectBase);
        ScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in native.InScreenSpaceSubsurfaceScatteringGuideSubrectBase);
        ScreenSpaceRefractionGuideSubrectBase = new(in native.InScreenSpaceRefractionGuideSubrectBase);
        DepthOfFieldGuideSubrectBase = new(in native.InDepthOfFieldGuideSubrectBase);
        DiffuseHitDistanceSubrectBase = new(in native.InDiffuseHitDistanceSubrectBase);
        SpecularHitDistanceSubrectBase = new(in native.InSpecularHitDistanceSubrectBase);
        DiffuseRayDirectionSubrectBase = new(in native.InDiffuseRayDirectionSubrectBase);
        SpecularRayDirectionSubrectBase = new(in native.InSpecularRayDirectionSubrectBase);
        DiffuseRayDirectionHitDistanceSubrectBase = new(in native.InDiffuseRayDirectionHitDistanceSubrectBase);
        SpecularRayDirectionHitDistanceSubrectBase = new(in native.InSpecularRayDirectionHitDistanceSubrectBase);
        ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in native.InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);
        ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in native.InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);
        ColorBeforeScreenSpaceRefractionSubrectBase = new(in native.InColorBeforeScreenSpaceRefractionSubrectBase);
        ColorAfterScreenSpaceRefractionSubrectBase = new(in native.InColorAfterScreenSpaceRefractionSubrectBase);
        ColorBeforeDepthOfFieldSubrectBase = new(in native.InColorBeforeDepthOfFieldSubrectBase);
        ColorAfterDepthOfFieldSubtectBase = new(in native.InColorAfterDepthOfFieldSubtectBase);
        WorldToViewMatrix = native.PInWorldToViewMatrix is null ? null : *native.PInWorldToViewMatrix;
        ViewToClipMatrix = native.PInViewToClipMatrix is null ? null : *native.PInViewToClipMatrix;
        PreExposure = native.InPreExposure;
        ExposureScale = native.InExposureScale;
        IndicatorInvertXAxis = native.InIndicatorInvertXAxis;
        IndicatorInvertYAxis = native.InIndicatorInvertYAxis;
        GBufferSurface = new(in native.GBufferSurface);
        ToneMapperType = native.InToneMapperType;
        MotionVectors3D = native.PInMotionVectors3D;
        IsParticleMask = native.PInIsParticleMask;
        AnimatedTextureMask = native.PInAnimatedTextureMask;
        DepthHighResolution = native.PInDepthHighRes;
        PositionViewSpace = native.PInPositionViewSpace;
        FrameTimeDeltaInMsec = native.InFrameTimeDeltaInMsec;
        RayTracingHitDistance = native.PInRayTracingHitDistance;
        MotionVectorsReflections = native.PInMotionVectorsReflections;
        TransparencyLayer = native.PInTransparencyLayer;
        TransparencyLayerSubrectBase = new(in native.InTransparencyLayerSubrectBase);
        TransparencyLayerOpacity = native.PInTransparencyLayerOpacity;
        TransparencyLayerOpacitySubrectBase = new(in native.InTransparencyLayerOpacitySubrectBase);
        TransparencyLayerMvecs = native.PInTransparencyLayerMvecs;
        TransparencyLayerMvecsSubrectBase = new(in native.InTransparencyLayerMvecsSubrectBase);
        DisocclusionMask = native.PInDisocclusionMask;
        DisocclusionMaskSubrectBase = new(in native.InDisocclusionMaskSubrectBase);
        ResponsivityMask = native.PInResponsivityMask;
        ResponsivityMaskSubrectBase = new(in native.InResponsivityMaskSubrectBase);
    }
}
