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

    internal unsafe NGXVKDLSSDEvalParams(in NGXVKDLSSDEvalParamsNative native)
    {
        DiffuseAlbedo = native.PInDiffuseAlbedo is null ? null : new NGXResourceVK(in *native.PInDiffuseAlbedo);
        SpecularAlbedo = native.PInSpecularAlbedo is null ? null : new NGXResourceVK(in *native.PInSpecularAlbedo);
        Normals = native.PInNormals is null ? null : new NGXResourceVK(in *native.PInNormals);
        Roughness = native.PInRoughness is null ? null : new NGXResourceVK(in *native.PInRoughness);
        Color = native.PInColor is null ? null : new NGXResourceVK(in *native.PInColor);
        Alpha = native.PInAlpha is null ? null : new NGXResourceVK(in *native.PInAlpha);
        Output = native.PInOutput is null ? null : new NGXResourceVK(in *native.PInOutput);
        OutputAlpha = native.PInOutputAlpha is null ? null : new NGXResourceVK(in *native.PInOutputAlpha);
        Depth = native.PInDepth is null ? null : new NGXResourceVK(in *native.PInDepth);
        MotionVectors = native.PInMotionVectors is null ? null : new NGXResourceVK(in *native.PInMotionVectors);
        JitterOffsetX = native.InJitterOffsetX;
        JitterOffsetY = native.InJitterOffsetY;
        RenderSubrectDimensions = new(in native.InRenderSubrectDimensions);
        Reset = native.InReset;
        MVScaleX = native.InMVScaleX;
        MVScaleY = native.InMVScaleY;
        TransparencyMask = native.PInTransparencyMask is null ? null : new NGXResourceVK(in *native.PInTransparencyMask);
        ExposureTexture = native.PInExposureTexture is null ? null : new NGXResourceVK(in *native.PInExposureTexture);
        BiasCurrentColorMask = native.PInBiasCurrentColorMask is null ? null : new NGXResourceVK(in *native.PInBiasCurrentColorMask);
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
        PreExposure = native.InPreExposure;
        ExposureScale = native.InExposureScale;
        IndicatorInvertXAxis = native.InIndicatorInvertXAxis;
        IndicatorInvertYAxis = native.InIndicatorInvertYAxis;
        ReflectedAlbedo = native.PInReflectedAlbedo is null ? null : new NGXResourceVK(in *native.PInReflectedAlbedo);
        ColorBeforeParticles = native.PInColorBeforeParticles is null ? null : new NGXResourceVK(in *native.PInColorBeforeParticles);
        ColorAfterParticles = native.PInColorAfterParticles is null ? null : new NGXResourceVK(in *native.PInColorAfterParticles);
        ColorBeforeTransparency = native.PInColorBeforeTransparency is null ? null : new NGXResourceVK(in *native.PInColorBeforeTransparency);
        ColorAfterTransparency = native.PInColorAfterTransparency is null ? null : new NGXResourceVK(in *native.PInColorAfterTransparency);
        ColorBeforeFog = native.PInColorBeforeFog is null ? null : new NGXResourceVK(in *native.PInColorBeforeFog);
        ColorAfterFog = native.PInColorAfterFog is null ? null : new NGXResourceVK(in *native.PInColorAfterFog);
        ScreenSpaceSubsurfaceScatteringGuide = native.PInScreenSpaceSubsurfaceScatteringGuide is null ? null : new NGXResourceVK(in *native.PInScreenSpaceSubsurfaceScatteringGuide);
        ColorBeforeScreenSpaceSubsurfaceScattering = native.PInColorBeforeScreenSpaceSubsurfaceScattering is null ? null : new NGXResourceVK(in *native.PInColorBeforeScreenSpaceSubsurfaceScattering);
        ColorAfterScreenSpaceSubsurfaceScattering = native.PInColorAfterScreenSpaceSubsurfaceScattering is null ? null : new NGXResourceVK(in *native.PInColorAfterScreenSpaceSubsurfaceScattering);
        ScreenSpaceRefractionGuide = native.PInScreenSpaceRefractionGuide is null ? null : new NGXResourceVK(in *native.PInScreenSpaceRefractionGuide);
        ColorBeforeScreenSpaceRefraction = native.PInColorBeforeScreenSpaceRefraction is null ? null : new NGXResourceVK(in *native.PInColorBeforeScreenSpaceRefraction);
        ColorAfterScreenSpaceRefraction = native.PInColorAfterScreenSpaceRefraction is null ? null : new NGXResourceVK(in *native.PInColorAfterScreenSpaceRefraction);
        DepthOfFieldGuide = native.PInDepthOfFieldGuide is null ? null : new NGXResourceVK(in *native.PInDepthOfFieldGuide);
        ColorBeforeDepthOfField = native.PInColorBeforeDepthOfField is null ? null : new NGXResourceVK(in *native.PInColorBeforeDepthOfField);
        ColorAfterDepthOfField = native.PInColorAfterDepthOfField is null ? null : new NGXResourceVK(in *native.PInColorAfterDepthOfField);
        DiffuseHitDistance = native.PInDiffuseHitDistance is null ? null : new NGXResourceVK(in *native.PInDiffuseHitDistance);
        SpecularHitDistance = native.PInSpecularHitDistance is null ? null : new NGXResourceVK(in *native.PInSpecularHitDistance);
        DiffuseRayDirection = native.PInDiffuseRayDirection is null ? null : new NGXResourceVK(in *native.PInDiffuseRayDirection);
        SpecularRayDirection = native.PInSpecularRayDirection is null ? null : new NGXResourceVK(in *native.PInSpecularRayDirection);
        DiffuseRayDirectionHitDistance = native.PInDiffuseRayDirectionHitDistance is null ? null : new NGXResourceVK(in *native.PInDiffuseRayDirectionHitDistance);
        SpecularRayDirectionHitDistance = native.PInSpecularRayDirectionHitDistance is null ? null : new NGXResourceVK(in *native.PInSpecularRayDirectionHitDistance);
        ReflectedAlbedoSubrectBase = new(in native.InReflectedAlbedoSubrectBase);
        ColorBeforeParticlesSubrectBase = new(in native.InColorBeforeParticlesSubrectBase);
        ColorAfterParticlesSubrectBase = new(in native.InColorAfterParticlesSubrectBase);
        ColorBeforeTransparencySubrectBase = new(in native.InColorBeforeTransparencySubrectBase);
        ColorAfterTransparencySubrectBase = new(in native.InColorAfterTransparencySubrectBase);
        ColorBeforeFogSubrectBase = new(in native.InColorBeforeFogSubrectBase);
        ColorAfterFogSubrectBase = new(in native.InColorAfterFogSubrectBase);
        ScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in native.InScreenSpaceSubsurfaceScatteringGuideSubrectBase);
        ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in native.InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);
        ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in native.InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);
        ScreenSpaceRefractionGuideSubrectBase = new(in native.InScreenSpaceRefractionGuideSubrectBase);
        ColorBeforeScreenSpaceRefractionSubrectBase = new(in native.InColorBeforeScreenSpaceRefractionSubrectBase);
        ColorAfterScreenSpaceRefractionSubrectBase = new(in native.InColorAfterScreenSpaceRefractionSubrectBase);
        DepthOfFieldGuideSubrectBase = new(in native.InDepthOfFieldGuideSubrectBase);
        ColorBeforeDepthOfFieldSubrectBase = new(in native.InColorBeforeDepthOfFieldSubrectBase);
        ColorAfterDepthOfFieldSubrectBase = new(in native.InColorAfterDepthOfFieldSubrectBase);
        DiffuseHitDistanceSubrectBase = new(in native.InDiffuseHitDistanceSubrectBase);
        SpecularHitDistanceSubrectBase = new(in native.InSpecularHitDistanceSubrectBase);
        DiffuseRayDirectionSubrectBase = new(in native.InDiffuseRayDirectionSubrectBase);
        SpecularRayDirectionSubrectBase = new(in native.InSpecularRayDirectionSubrectBase);
        DiffuseRayDirectionHitDistanceSubrectBase = new(in native.InDiffuseRayDirectionHitDistanceSubrectBase);
        SpecularRayDirectionHitDistanceSubrectBase = new(in native.InSpecularRayDirectionHitDistanceSubrectBase);
        WorldToViewMatrix = native.PInWorldToViewMatrix is null ? null : *native.PInWorldToViewMatrix;
        ViewToClipMatrix = native.PInViewToClipMatrix is null ? null : *native.PInViewToClipMatrix;
        GBufferSurface = new(in native.GBufferSurface);
        ToneMapperType = native.InToneMapperType;
        MotionVectors3D = native.PInMotionVectors3D is null ? null : new NGXResourceVK(in *native.PInMotionVectors3D);
        IsParticleMask = native.PInIsParticleMask is null ? null : new NGXResourceVK(in *native.PInIsParticleMask);
        AnimatedTextureMask = native.PInAnimatedTextureMask is null ? null : new NGXResourceVK(in *native.PInAnimatedTextureMask);
        DepthHighResolution = native.PInDepthHighRes is null ? null : new NGXResourceVK(in *native.PInDepthHighRes);
        PositionViewSpace = native.PInPositionViewSpace is null ? null : new NGXResourceVK(in *native.PInPositionViewSpace);
        FrameTimeDeltaInMsec = native.InFrameTimeDeltaInMsec;
        RayTracingHitDistance = native.PInRayTracingHitDistance is null ? null : new NGXResourceVK(in *native.PInRayTracingHitDistance);
        MotionVectorsReflections = native.PInMotionVectorsReflections is null ? null : new NGXResourceVK(in *native.PInMotionVectorsReflections);
        TransparencyLayer = native.PInTransparencyLayer is null ? null : new NGXResourceVK(in *native.PInTransparencyLayer);
        TransparencyLayerSubrectBase = new(in native.InTransparencyLayerSubrectBase);
        TransparencyLayerOpacity = native.PInTransparencyLayerOpacity is null ? null : new NGXResourceVK(in *native.PInTransparencyLayerOpacity);
        TransparencyLayerOpacitySubrectBase = new(in native.InTransparencyLayerOpacitySubrectBase);
        TransparencyLayerMvecs = native.PInTransparencyLayerMvecs is null ? null : new NGXResourceVK(in *native.PInTransparencyLayerMvecs);
        TransparencyLayerMvecsSubrectBase = new(in native.InTransparencyLayerMvecsSubrectBase);
        DisocclusionMask = native.PInDisocclusionMask is null ? null : new NGXResourceVK(in *native.PInDisocclusionMask);
        DisocclusionMaskSubrectBase = new(in native.InDisocclusionMaskSubrectBase);
        ResponsivityMask = native.PInResponsivityMask is null ? null : new NGXResourceVK(in *native.PInResponsivityMask);
        ResponsivityMaskSubrectBase = new(in native.InResponsivityMaskSubrectBase);
    }
}
