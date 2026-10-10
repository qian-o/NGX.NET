#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 904)]
internal unsafe struct NGXVKDLSSDEvalParamsNative
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInDiffuseAlbedo;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInSpecularAlbedo;

    [FieldOffset(16)]
    public NGXResourceVKNative* PInNormals;

    [FieldOffset(24)]
    public NGXResourceVKNative* PInRoughness;

    [FieldOffset(32)]
    public NGXResourceVKNative* PInColor;

    [FieldOffset(40)]
    public NGXResourceVKNative* PInAlpha;

    [FieldOffset(48)]
    public NGXResourceVKNative* PInOutput;

    [FieldOffset(56)]
    public NGXResourceVKNative* PInOutputAlpha;

    [FieldOffset(64)]
    public NGXResourceVKNative* PInDepth;

    [FieldOffset(72)]
    public NGXResourceVKNative* PInMotionVectors;

    [FieldOffset(80)]
    public float InJitterOffsetX;

    [FieldOffset(84)]
    public float InJitterOffsetY;

    [FieldOffset(88)]
    public NGXDimensionsNative InRenderSubrectDimensions;

    [FieldOffset(96)]
    public int InReset;

    [FieldOffset(100)]
    public float InMVScaleX;

    [FieldOffset(104)]
    public float InMVScaleY;

    [FieldOffset(112)]
    public NGXResourceVKNative* PInTransparencyMask;

    [FieldOffset(120)]
    public NGXResourceVKNative* PInExposureTexture;

    [FieldOffset(128)]
    public NGXResourceVKNative* PInBiasCurrentColorMask;

    [FieldOffset(136)]
    public NGXCoordinatesNative InAlphaSubrectBase;

    [FieldOffset(144)]
    public NGXCoordinatesNative InOutputAlphaSubrectBase;

    [FieldOffset(152)]
    public NGXCoordinatesNative InDiffuseAlbedoSubrectBase;

    [FieldOffset(160)]
    public NGXCoordinatesNative InSpecularAlbedoSubrectBase;

    [FieldOffset(168)]
    public NGXCoordinatesNative InNormalsSubrectBase;

    [FieldOffset(176)]
    public NGXCoordinatesNative InRoughnessSubrectBase;

    [FieldOffset(184)]
    public NGXCoordinatesNative InColorSubrectBase;

    [FieldOffset(192)]
    public NGXCoordinatesNative InDepthSubrectBase;

    [FieldOffset(200)]
    public NGXCoordinatesNative InMVSubrectBase;

    [FieldOffset(208)]
    public NGXCoordinatesNative InTranslucencySubrectBase;

    [FieldOffset(216)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase;

    [FieldOffset(224)]
    public NGXCoordinatesNative InOutputSubrectBase;

    [FieldOffset(232)]
    public float InPreExposure;

    [FieldOffset(236)]
    public float InExposureScale;

    [FieldOffset(240)]
    public int InIndicatorInvertXAxis;

    [FieldOffset(244)]
    public int InIndicatorInvertYAxis;

    [FieldOffset(248)]
    public NGXResourceVKNative* PInReflectedAlbedo;

    [FieldOffset(256)]
    public NGXResourceVKNative* PInColorBeforeParticles;

    [FieldOffset(264)]
    public NGXResourceVKNative* PInColorAfterParticles;

    [FieldOffset(272)]
    public NGXResourceVKNative* PInColorBeforeTransparency;

    [FieldOffset(280)]
    public NGXResourceVKNative* PInColorAfterTransparency;

    [FieldOffset(288)]
    public NGXResourceVKNative* PInColorBeforeFog;

    [FieldOffset(296)]
    public NGXResourceVKNative* PInColorAfterFog;

    [FieldOffset(304)]
    public NGXResourceVKNative* PInScreenSpaceSubsurfaceScatteringGuide;

    [FieldOffset(312)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceSubsurfaceScattering;

    [FieldOffset(320)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceSubsurfaceScattering;

    [FieldOffset(328)]
    public NGXResourceVKNative* PInScreenSpaceRefractionGuide;

    [FieldOffset(336)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceRefraction;

    [FieldOffset(344)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceRefraction;

    [FieldOffset(352)]
    public NGXResourceVKNative* PInDepthOfFieldGuide;

    [FieldOffset(360)]
    public NGXResourceVKNative* PInColorBeforeDepthOfField;

    [FieldOffset(368)]
    public NGXResourceVKNative* PInColorAfterDepthOfField;

    [FieldOffset(376)]
    public NGXResourceVKNative* PInDiffuseHitDistance;

    [FieldOffset(384)]
    public NGXResourceVKNative* PInSpecularHitDistance;

    [FieldOffset(392)]
    public NGXResourceVKNative* PInDiffuseRayDirection;

    [FieldOffset(400)]
    public NGXResourceVKNative* PInSpecularRayDirection;

    [FieldOffset(408)]
    public NGXResourceVKNative* PInDiffuseRayDirectionHitDistance;

    [FieldOffset(416)]
    public NGXResourceVKNative* PInSpecularRayDirectionHitDistance;

    [FieldOffset(424)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase;

    [FieldOffset(432)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase;

    [FieldOffset(440)]
    public NGXCoordinatesNative InColorAfterParticlesSubrectBase;

    [FieldOffset(448)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase;

    [FieldOffset(456)]
    public NGXCoordinatesNative InColorAfterTransparencySubrectBase;

    [FieldOffset(464)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase;

    [FieldOffset(472)]
    public NGXCoordinatesNative InColorAfterFogSubrectBase;

    [FieldOffset(480)]
    public NGXCoordinatesNative InScreenSpaceSubsurfaceScatteringGuideSubrectBase;

    [FieldOffset(488)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(496)]
    public NGXCoordinatesNative InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(504)]
    public NGXCoordinatesNative InScreenSpaceRefractionGuideSubrectBase;

    [FieldOffset(512)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceRefractionSubrectBase;

    [FieldOffset(520)]
    public NGXCoordinatesNative InColorAfterScreenSpaceRefractionSubrectBase;

    [FieldOffset(528)]
    public NGXCoordinatesNative InDepthOfFieldGuideSubrectBase;

    [FieldOffset(536)]
    public NGXCoordinatesNative InColorBeforeDepthOfFieldSubrectBase;

    [FieldOffset(544)]
    public NGXCoordinatesNative InColorAfterDepthOfFieldSubrectBase;

    [FieldOffset(552)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase;

    [FieldOffset(560)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase;

    [FieldOffset(568)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase;

    [FieldOffset(576)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase;

    [FieldOffset(584)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase;

    [FieldOffset(592)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase;

    [FieldOffset(600)]
    public Matrix4x4* PInWorldToViewMatrix;

    [FieldOffset(608)]
    public Matrix4x4* PInViewToClipMatrix;

    [FieldOffset(616)]
    public NGXVKGBufferNative GBufferSurface;

    [FieldOffset(752)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(760)]
    public NGXResourceVKNative* PInMotionVectors3D;

    [FieldOffset(768)]
    public NGXResourceVKNative* PInIsParticleMask;

    [FieldOffset(776)]
    public NGXResourceVKNative* PInAnimatedTextureMask;

    [FieldOffset(784)]
    public NGXResourceVKNative* PInDepthHighRes;

    [FieldOffset(792)]
    public NGXResourceVKNative* PInPositionViewSpace;

    [FieldOffset(800)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(808)]
    public NGXResourceVKNative* PInRayTracingHitDistance;

    [FieldOffset(816)]
    public NGXResourceVKNative* PInMotionVectorsReflections;

    [FieldOffset(824)]
    public NGXResourceVKNative* PInTransparencyLayer;

    [FieldOffset(832)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase;

    [FieldOffset(840)]
    public NGXResourceVKNative* PInTransparencyLayerOpacity;

    [FieldOffset(848)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase;

    [FieldOffset(856)]
    public NGXResourceVKNative* PInTransparencyLayerMvecs;

    [FieldOffset(864)]
    public NGXCoordinatesNative InTransparencyLayerMvecsSubrectBase;

    [FieldOffset(872)]
    public NGXResourceVKNative* PInDisocclusionMask;

    [FieldOffset(880)]
    public NGXCoordinatesNative InDisocclusionMaskSubrectBase;

    [FieldOffset(888)]
    public NGXResourceVKNative* PInResponsivityMask;

    [FieldOffset(896)]
    public NGXCoordinatesNative InResponsivityMaskSubrectBase;

    public NGXVKDLSSDEvalParamsNative(in NGXVKDLSSDEvalParams value, NativeScope scope)
    {
        PInDiffuseAlbedo = value.DiffuseAlbedo is NGXResourceVK diffuseAlbedo ? scope.Alloc(new NGXResourceVKNative(in diffuseAlbedo)) : null;
        PInSpecularAlbedo = value.SpecularAlbedo is NGXResourceVK specularAlbedo ? scope.Alloc(new NGXResourceVKNative(in specularAlbedo)) : null;
        PInNormals = value.Normals is NGXResourceVK normals ? scope.Alloc(new NGXResourceVKNative(in normals)) : null;
        PInRoughness = value.Roughness is NGXResourceVK roughness ? scope.Alloc(new NGXResourceVKNative(in roughness)) : null;
        PInColor = value.Color is NGXResourceVK color ? scope.Alloc(new NGXResourceVKNative(in color)) : null;
        PInAlpha = value.Alpha is NGXResourceVK alpha ? scope.Alloc(new NGXResourceVKNative(in alpha)) : null;
        PInOutput = value.Output is NGXResourceVK output ? scope.Alloc(new NGXResourceVKNative(in output)) : null;
        PInOutputAlpha = value.OutputAlpha is NGXResourceVK outputAlpha ? scope.Alloc(new NGXResourceVKNative(in outputAlpha)) : null;
        PInDepth = value.Depth is NGXResourceVK depth ? scope.Alloc(new NGXResourceVKNative(in depth)) : null;
        PInMotionVectors = value.MotionVectors is NGXResourceVK motionVectors ? scope.Alloc(new NGXResourceVKNative(in motionVectors)) : null;
        InJitterOffsetX = value.JitterOffsetX;
        InJitterOffsetY = value.JitterOffsetY;
        InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);
        InReset = value.Reset;
        InMVScaleX = value.MVScaleX;
        InMVScaleY = value.MVScaleY;
        PInTransparencyMask = value.TransparencyMask is NGXResourceVK transparencyMask ? scope.Alloc(new NGXResourceVKNative(in transparencyMask)) : null;
        PInExposureTexture = value.ExposureTexture is NGXResourceVK exposureTexture ? scope.Alloc(new NGXResourceVKNative(in exposureTexture)) : null;
        PInBiasCurrentColorMask = value.BiasCurrentColorMask is NGXResourceVK biasCurrentColorMask ? scope.Alloc(new NGXResourceVKNative(in biasCurrentColorMask)) : null;
        InAlphaSubrectBase = new(in value.AlphaSubrectBase);
        InOutputAlphaSubrectBase = new(in value.OutputAlphaSubrectBase);
        InDiffuseAlbedoSubrectBase = new(in value.DiffuseAlbedoSubrectBase);
        InSpecularAlbedoSubrectBase = new(in value.SpecularAlbedoSubrectBase);
        InNormalsSubrectBase = new(in value.NormalsSubrectBase);
        InRoughnessSubrectBase = new(in value.RoughnessSubrectBase);
        InColorSubrectBase = new(in value.ColorSubrectBase);
        InDepthSubrectBase = new(in value.DepthSubrectBase);
        InMVSubrectBase = new(in value.MVSubrectBase);
        InTranslucencySubrectBase = new(in value.TranslucencySubrectBase);
        InBiasCurrentColorSubrectBase = new(in value.BiasCurrentColorSubrectBase);
        InOutputSubrectBase = new(in value.OutputSubrectBase);
        InPreExposure = value.PreExposure;
        InExposureScale = value.ExposureScale;
        InIndicatorInvertXAxis = value.IndicatorInvertXAxis;
        InIndicatorInvertYAxis = value.IndicatorInvertYAxis;
        PInReflectedAlbedo = value.ReflectedAlbedo is NGXResourceVK reflectedAlbedo ? scope.Alloc(new NGXResourceVKNative(in reflectedAlbedo)) : null;
        PInColorBeforeParticles = value.ColorBeforeParticles is NGXResourceVK colorBeforeParticles ? scope.Alloc(new NGXResourceVKNative(in colorBeforeParticles)) : null;
        PInColorAfterParticles = value.ColorAfterParticles is NGXResourceVK colorAfterParticles ? scope.Alloc(new NGXResourceVKNative(in colorAfterParticles)) : null;
        PInColorBeforeTransparency = value.ColorBeforeTransparency is NGXResourceVK colorBeforeTransparency ? scope.Alloc(new NGXResourceVKNative(in colorBeforeTransparency)) : null;
        PInColorAfterTransparency = value.ColorAfterTransparency is NGXResourceVK colorAfterTransparency ? scope.Alloc(new NGXResourceVKNative(in colorAfterTransparency)) : null;
        PInColorBeforeFog = value.ColorBeforeFog is NGXResourceVK colorBeforeFog ? scope.Alloc(new NGXResourceVKNative(in colorBeforeFog)) : null;
        PInColorAfterFog = value.ColorAfterFog is NGXResourceVK colorAfterFog ? scope.Alloc(new NGXResourceVKNative(in colorAfterFog)) : null;
        PInScreenSpaceSubsurfaceScatteringGuide = value.ScreenSpaceSubsurfaceScatteringGuide is NGXResourceVK screenSpaceSubsurfaceScatteringGuide ? scope.Alloc(new NGXResourceVKNative(in screenSpaceSubsurfaceScatteringGuide)) : null;
        PInColorBeforeScreenSpaceSubsurfaceScattering = value.ColorBeforeScreenSpaceSubsurfaceScattering is NGXResourceVK colorBeforeScreenSpaceSubsurfaceScattering ? scope.Alloc(new NGXResourceVKNative(in colorBeforeScreenSpaceSubsurfaceScattering)) : null;
        PInColorAfterScreenSpaceSubsurfaceScattering = value.ColorAfterScreenSpaceSubsurfaceScattering is NGXResourceVK colorAfterScreenSpaceSubsurfaceScattering ? scope.Alloc(new NGXResourceVKNative(in colorAfterScreenSpaceSubsurfaceScattering)) : null;
        PInScreenSpaceRefractionGuide = value.ScreenSpaceRefractionGuide is NGXResourceVK screenSpaceRefractionGuide ? scope.Alloc(new NGXResourceVKNative(in screenSpaceRefractionGuide)) : null;
        PInColorBeforeScreenSpaceRefraction = value.ColorBeforeScreenSpaceRefraction is NGXResourceVK colorBeforeScreenSpaceRefraction ? scope.Alloc(new NGXResourceVKNative(in colorBeforeScreenSpaceRefraction)) : null;
        PInColorAfterScreenSpaceRefraction = value.ColorAfterScreenSpaceRefraction is NGXResourceVK colorAfterScreenSpaceRefraction ? scope.Alloc(new NGXResourceVKNative(in colorAfterScreenSpaceRefraction)) : null;
        PInDepthOfFieldGuide = value.DepthOfFieldGuide is NGXResourceVK depthOfFieldGuide ? scope.Alloc(new NGXResourceVKNative(in depthOfFieldGuide)) : null;
        PInColorBeforeDepthOfField = value.ColorBeforeDepthOfField is NGXResourceVK colorBeforeDepthOfField ? scope.Alloc(new NGXResourceVKNative(in colorBeforeDepthOfField)) : null;
        PInColorAfterDepthOfField = value.ColorAfterDepthOfField is NGXResourceVK colorAfterDepthOfField ? scope.Alloc(new NGXResourceVKNative(in colorAfterDepthOfField)) : null;
        PInDiffuseHitDistance = value.DiffuseHitDistance is NGXResourceVK diffuseHitDistance ? scope.Alloc(new NGXResourceVKNative(in diffuseHitDistance)) : null;
        PInSpecularHitDistance = value.SpecularHitDistance is NGXResourceVK specularHitDistance ? scope.Alloc(new NGXResourceVKNative(in specularHitDistance)) : null;
        PInDiffuseRayDirection = value.DiffuseRayDirection is NGXResourceVK diffuseRayDirection ? scope.Alloc(new NGXResourceVKNative(in diffuseRayDirection)) : null;
        PInSpecularRayDirection = value.SpecularRayDirection is NGXResourceVK specularRayDirection ? scope.Alloc(new NGXResourceVKNative(in specularRayDirection)) : null;
        PInDiffuseRayDirectionHitDistance = value.DiffuseRayDirectionHitDistance is NGXResourceVK diffuseRayDirectionHitDistance ? scope.Alloc(new NGXResourceVKNative(in diffuseRayDirectionHitDistance)) : null;
        PInSpecularRayDirectionHitDistance = value.SpecularRayDirectionHitDistance is NGXResourceVK specularRayDirectionHitDistance ? scope.Alloc(new NGXResourceVKNative(in specularRayDirectionHitDistance)) : null;
        InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);
        InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);
        InColorAfterParticlesSubrectBase = new(in value.ColorAfterParticlesSubrectBase);
        InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);
        InColorAfterTransparencySubrectBase = new(in value.ColorAfterTransparencySubrectBase);
        InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);
        InColorAfterFogSubrectBase = new(in value.ColorAfterFogSubrectBase);
        InScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in value.ScreenSpaceSubsurfaceScatteringGuideSubrectBase);
        InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);
        InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);
        InScreenSpaceRefractionGuideSubrectBase = new(in value.ScreenSpaceRefractionGuideSubrectBase);
        InColorBeforeScreenSpaceRefractionSubrectBase = new(in value.ColorBeforeScreenSpaceRefractionSubrectBase);
        InColorAfterScreenSpaceRefractionSubrectBase = new(in value.ColorAfterScreenSpaceRefractionSubrectBase);
        InDepthOfFieldGuideSubrectBase = new(in value.DepthOfFieldGuideSubrectBase);
        InColorBeforeDepthOfFieldSubrectBase = new(in value.ColorBeforeDepthOfFieldSubrectBase);
        InColorAfterDepthOfFieldSubrectBase = new(in value.ColorAfterDepthOfFieldSubrectBase);
        InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);
        InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);
        InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);
        InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);
        InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);
        InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);
        PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? scope.Alloc(value.WorldToViewMatrix.Value) : null;
        PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? scope.Alloc(value.ViewToClipMatrix.Value) : null;
        GBufferSurface = new(in value.GBufferSurface, scope);
        InToneMapperType = value.ToneMapperType;
        PInMotionVectors3D = value.MotionVectors3D is NGXResourceVK motionVectors3D ? scope.Alloc(new NGXResourceVKNative(in motionVectors3D)) : null;
        PInIsParticleMask = value.IsParticleMask is NGXResourceVK isParticleMask ? scope.Alloc(new NGXResourceVKNative(in isParticleMask)) : null;
        PInAnimatedTextureMask = value.AnimatedTextureMask is NGXResourceVK animatedTextureMask ? scope.Alloc(new NGXResourceVKNative(in animatedTextureMask)) : null;
        PInDepthHighRes = value.DepthHighResolution is NGXResourceVK depthHighResolution ? scope.Alloc(new NGXResourceVKNative(in depthHighResolution)) : null;
        PInPositionViewSpace = value.PositionViewSpace is NGXResourceVK positionViewSpace ? scope.Alloc(new NGXResourceVKNative(in positionViewSpace)) : null;
        InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;
        PInRayTracingHitDistance = value.RayTracingHitDistance is NGXResourceVK rayTracingHitDistance ? scope.Alloc(new NGXResourceVKNative(in rayTracingHitDistance)) : null;
        PInMotionVectorsReflections = value.MotionVectorsReflections is NGXResourceVK motionVectorsReflections ? scope.Alloc(new NGXResourceVKNative(in motionVectorsReflections)) : null;
        PInTransparencyLayer = value.TransparencyLayer is NGXResourceVK transparencyLayer ? scope.Alloc(new NGXResourceVKNative(in transparencyLayer)) : null;
        InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);
        PInTransparencyLayerOpacity = value.TransparencyLayerOpacity is NGXResourceVK transparencyLayerOpacity ? scope.Alloc(new NGXResourceVKNative(in transparencyLayerOpacity)) : null;
        InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);
        PInTransparencyLayerMvecs = value.TransparencyLayerMvecs is NGXResourceVK transparencyLayerMvecs ? scope.Alloc(new NGXResourceVKNative(in transparencyLayerMvecs)) : null;
        InTransparencyLayerMvecsSubrectBase = new(in value.TransparencyLayerMvecsSubrectBase);
        PInDisocclusionMask = value.DisocclusionMask is NGXResourceVK disocclusionMask ? scope.Alloc(new NGXResourceVKNative(in disocclusionMask)) : null;
        InDisocclusionMaskSubrectBase = new(in value.DisocclusionMaskSubrectBase);
        PInResponsivityMask = value.ResponsivityMask is NGXResourceVK responsivityMask ? scope.Alloc(new NGXResourceVKNative(in responsivityMask)) : null;
        InResponsivityMaskSubrectBase = new(in value.ResponsivityMaskSubrectBase);
    }
}
