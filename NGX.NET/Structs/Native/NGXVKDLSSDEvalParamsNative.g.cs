namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 904)]
internal unsafe struct NGXVKDLSSDEvalParamsNative(in NGXVKDLSSDEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInDiffuseAlbedo = value.DiffuseAlbedo is NGXResourceVK diffuseAlbedo ? scope.Alloc(new NGXResourceVKNative(in diffuseAlbedo)) : null;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInSpecularAlbedo = value.SpecularAlbedo is NGXResourceVK specularAlbedo ? scope.Alloc(new NGXResourceVKNative(in specularAlbedo)) : null;

    [FieldOffset(16)]
    public NGXResourceVKNative* PInNormals = value.Normals is NGXResourceVK normals ? scope.Alloc(new NGXResourceVKNative(in normals)) : null;

    [FieldOffset(24)]
    public NGXResourceVKNative* PInRoughness = value.Roughness is NGXResourceVK roughness ? scope.Alloc(new NGXResourceVKNative(in roughness)) : null;

    [FieldOffset(32)]
    public NGXResourceVKNative* PInColor = value.Color is NGXResourceVK color ? scope.Alloc(new NGXResourceVKNative(in color)) : null;

    [FieldOffset(40)]
    public NGXResourceVKNative* PInAlpha = value.Alpha is NGXResourceVK alpha ? scope.Alloc(new NGXResourceVKNative(in alpha)) : null;

    [FieldOffset(48)]
    public NGXResourceVKNative* PInOutput = value.Output is NGXResourceVK output ? scope.Alloc(new NGXResourceVKNative(in output)) : null;

    [FieldOffset(56)]
    public NGXResourceVKNative* PInOutputAlpha = value.OutputAlpha is NGXResourceVK outputAlpha ? scope.Alloc(new NGXResourceVKNative(in outputAlpha)) : null;

    [FieldOffset(64)]
    public NGXResourceVKNative* PInDepth = value.Depth is NGXResourceVK depth ? scope.Alloc(new NGXResourceVKNative(in depth)) : null;

    [FieldOffset(72)]
    public NGXResourceVKNative* PInMotionVectors = value.MotionVectors is NGXResourceVK motionVectors ? scope.Alloc(new NGXResourceVKNative(in motionVectors)) : null;

    [FieldOffset(80)]
    public float InJitterOffsetX = value.JitterOffsetX;

    [FieldOffset(84)]
    public float InJitterOffsetY = value.JitterOffsetY;

    [FieldOffset(88)]
    public NGXDimensionsNative InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);

    [FieldOffset(96)]
    public int InReset = value.Reset;

    [FieldOffset(100)]
    public float InMVScaleX = value.MVScaleX;

    [FieldOffset(104)]
    public float InMVScaleY = value.MVScaleY;

    [FieldOffset(112)]
    public NGXResourceVKNative* PInTransparencyMask = value.TransparencyMask is NGXResourceVK transparencyMask ? scope.Alloc(new NGXResourceVKNative(in transparencyMask)) : null;

    [FieldOffset(120)]
    public NGXResourceVKNative* PInExposureTexture = value.ExposureTexture is NGXResourceVK exposureTexture ? scope.Alloc(new NGXResourceVKNative(in exposureTexture)) : null;

    [FieldOffset(128)]
    public NGXResourceVKNative* PInBiasCurrentColorMask = value.BiasCurrentColorMask is NGXResourceVK biasCurrentColorMask ? scope.Alloc(new NGXResourceVKNative(in biasCurrentColorMask)) : null;

    [FieldOffset(136)]
    public NGXCoordinatesNative InAlphaSubrectBase = new(in value.AlphaSubrectBase);

    [FieldOffset(144)]
    public NGXCoordinatesNative InOutputAlphaSubrectBase = new(in value.OutputAlphaSubrectBase);

    [FieldOffset(152)]
    public NGXCoordinatesNative InDiffuseAlbedoSubrectBase = new(in value.DiffuseAlbedoSubrectBase);

    [FieldOffset(160)]
    public NGXCoordinatesNative InSpecularAlbedoSubrectBase = new(in value.SpecularAlbedoSubrectBase);

    [FieldOffset(168)]
    public NGXCoordinatesNative InNormalsSubrectBase = new(in value.NormalsSubrectBase);

    [FieldOffset(176)]
    public NGXCoordinatesNative InRoughnessSubrectBase = new(in value.RoughnessSubrectBase);

    [FieldOffset(184)]
    public NGXCoordinatesNative InColorSubrectBase = new(in value.ColorSubrectBase);

    [FieldOffset(192)]
    public NGXCoordinatesNative InDepthSubrectBase = new(in value.DepthSubrectBase);

    [FieldOffset(200)]
    public NGXCoordinatesNative InMVSubrectBase = new(in value.MVSubrectBase);

    [FieldOffset(208)]
    public NGXCoordinatesNative InTranslucencySubrectBase = new(in value.TranslucencySubrectBase);

    [FieldOffset(216)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase = new(in value.BiasCurrentColorSubrectBase);

    [FieldOffset(224)]
    public NGXCoordinatesNative InOutputSubrectBase = new(in value.OutputSubrectBase);

    [FieldOffset(232)]
    public float InPreExposure = value.PreExposure;

    [FieldOffset(236)]
    public float InExposureScale = value.ExposureScale;

    [FieldOffset(240)]
    public int InIndicatorInvertXAxis = value.IndicatorInvertXAxis;

    [FieldOffset(244)]
    public int InIndicatorInvertYAxis = value.IndicatorInvertYAxis;

    [FieldOffset(248)]
    public NGXResourceVKNative* PInReflectedAlbedo = value.ReflectedAlbedo is NGXResourceVK reflectedAlbedo ? scope.Alloc(new NGXResourceVKNative(in reflectedAlbedo)) : null;

    [FieldOffset(256)]
    public NGXResourceVKNative* PInColorBeforeParticles = value.ColorBeforeParticles is NGXResourceVK colorBeforeParticles ? scope.Alloc(new NGXResourceVKNative(in colorBeforeParticles)) : null;

    [FieldOffset(264)]
    public NGXResourceVKNative* PInColorAfterParticles = value.ColorAfterParticles is NGXResourceVK colorAfterParticles ? scope.Alloc(new NGXResourceVKNative(in colorAfterParticles)) : null;

    [FieldOffset(272)]
    public NGXResourceVKNative* PInColorBeforeTransparency = value.ColorBeforeTransparency is NGXResourceVK colorBeforeTransparency ? scope.Alloc(new NGXResourceVKNative(in colorBeforeTransparency)) : null;

    [FieldOffset(280)]
    public NGXResourceVKNative* PInColorAfterTransparency = value.ColorAfterTransparency is NGXResourceVK colorAfterTransparency ? scope.Alloc(new NGXResourceVKNative(in colorAfterTransparency)) : null;

    [FieldOffset(288)]
    public NGXResourceVKNative* PInColorBeforeFog = value.ColorBeforeFog is NGXResourceVK colorBeforeFog ? scope.Alloc(new NGXResourceVKNative(in colorBeforeFog)) : null;

    [FieldOffset(296)]
    public NGXResourceVKNative* PInColorAfterFog = value.ColorAfterFog is NGXResourceVK colorAfterFog ? scope.Alloc(new NGXResourceVKNative(in colorAfterFog)) : null;

    [FieldOffset(304)]
    public NGXResourceVKNative* PInScreenSpaceSubsurfaceScatteringGuide = value.ScreenSpaceSubsurfaceScatteringGuide is NGXResourceVK screenSpaceSubsurfaceScatteringGuide ? scope.Alloc(new NGXResourceVKNative(in screenSpaceSubsurfaceScatteringGuide)) : null;

    [FieldOffset(312)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceSubsurfaceScattering = value.ColorBeforeScreenSpaceSubsurfaceScattering is NGXResourceVK colorBeforeScreenSpaceSubsurfaceScattering ? scope.Alloc(new NGXResourceVKNative(in colorBeforeScreenSpaceSubsurfaceScattering)) : null;

    [FieldOffset(320)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceSubsurfaceScattering = value.ColorAfterScreenSpaceSubsurfaceScattering is NGXResourceVK colorAfterScreenSpaceSubsurfaceScattering ? scope.Alloc(new NGXResourceVKNative(in colorAfterScreenSpaceSubsurfaceScattering)) : null;

    [FieldOffset(328)]
    public NGXResourceVKNative* PInScreenSpaceRefractionGuide = value.ScreenSpaceRefractionGuide is NGXResourceVK screenSpaceRefractionGuide ? scope.Alloc(new NGXResourceVKNative(in screenSpaceRefractionGuide)) : null;

    [FieldOffset(336)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceRefraction = value.ColorBeforeScreenSpaceRefraction is NGXResourceVK colorBeforeScreenSpaceRefraction ? scope.Alloc(new NGXResourceVKNative(in colorBeforeScreenSpaceRefraction)) : null;

    [FieldOffset(344)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceRefraction = value.ColorAfterScreenSpaceRefraction is NGXResourceVK colorAfterScreenSpaceRefraction ? scope.Alloc(new NGXResourceVKNative(in colorAfterScreenSpaceRefraction)) : null;

    [FieldOffset(352)]
    public NGXResourceVKNative* PInDepthOfFieldGuide = value.DepthOfFieldGuide is NGXResourceVK depthOfFieldGuide ? scope.Alloc(new NGXResourceVKNative(in depthOfFieldGuide)) : null;

    [FieldOffset(360)]
    public NGXResourceVKNative* PInColorBeforeDepthOfField = value.ColorBeforeDepthOfField is NGXResourceVK colorBeforeDepthOfField ? scope.Alloc(new NGXResourceVKNative(in colorBeforeDepthOfField)) : null;

    [FieldOffset(368)]
    public NGXResourceVKNative* PInColorAfterDepthOfField = value.ColorAfterDepthOfField is NGXResourceVK colorAfterDepthOfField ? scope.Alloc(new NGXResourceVKNative(in colorAfterDepthOfField)) : null;

    [FieldOffset(376)]
    public NGXResourceVKNative* PInDiffuseHitDistance = value.DiffuseHitDistance is NGXResourceVK diffuseHitDistance ? scope.Alloc(new NGXResourceVKNative(in diffuseHitDistance)) : null;

    [FieldOffset(384)]
    public NGXResourceVKNative* PInSpecularHitDistance = value.SpecularHitDistance is NGXResourceVK specularHitDistance ? scope.Alloc(new NGXResourceVKNative(in specularHitDistance)) : null;

    [FieldOffset(392)]
    public NGXResourceVKNative* PInDiffuseRayDirection = value.DiffuseRayDirection is NGXResourceVK diffuseRayDirection ? scope.Alloc(new NGXResourceVKNative(in diffuseRayDirection)) : null;

    [FieldOffset(400)]
    public NGXResourceVKNative* PInSpecularRayDirection = value.SpecularRayDirection is NGXResourceVK specularRayDirection ? scope.Alloc(new NGXResourceVKNative(in specularRayDirection)) : null;

    [FieldOffset(408)]
    public NGXResourceVKNative* PInDiffuseRayDirectionHitDistance = value.DiffuseRayDirectionHitDistance is NGXResourceVK diffuseRayDirectionHitDistance ? scope.Alloc(new NGXResourceVKNative(in diffuseRayDirectionHitDistance)) : null;

    [FieldOffset(416)]
    public NGXResourceVKNative* PInSpecularRayDirectionHitDistance = value.SpecularRayDirectionHitDistance is NGXResourceVK specularRayDirectionHitDistance ? scope.Alloc(new NGXResourceVKNative(in specularRayDirectionHitDistance)) : null;

    [FieldOffset(424)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);

    [FieldOffset(432)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);

    [FieldOffset(440)]
    public NGXCoordinatesNative InColorAfterParticlesSubrectBase = new(in value.ColorAfterParticlesSubrectBase);

    [FieldOffset(448)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);

    [FieldOffset(456)]
    public NGXCoordinatesNative InColorAfterTransparencySubrectBase = new(in value.ColorAfterTransparencySubrectBase);

    [FieldOffset(464)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);

    [FieldOffset(472)]
    public NGXCoordinatesNative InColorAfterFogSubrectBase = new(in value.ColorAfterFogSubrectBase);

    [FieldOffset(480)]
    public NGXCoordinatesNative InScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in value.ScreenSpaceSubsurfaceScatteringGuideSubrectBase);

    [FieldOffset(488)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);

    [FieldOffset(496)]
    public NGXCoordinatesNative InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);

    [FieldOffset(504)]
    public NGXCoordinatesNative InScreenSpaceRefractionGuideSubrectBase = new(in value.ScreenSpaceRefractionGuideSubrectBase);

    [FieldOffset(512)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceRefractionSubrectBase = new(in value.ColorBeforeScreenSpaceRefractionSubrectBase);

    [FieldOffset(520)]
    public NGXCoordinatesNative InColorAfterScreenSpaceRefractionSubrectBase = new(in value.ColorAfterScreenSpaceRefractionSubrectBase);

    [FieldOffset(528)]
    public NGXCoordinatesNative InDepthOfFieldGuideSubrectBase = new(in value.DepthOfFieldGuideSubrectBase);

    [FieldOffset(536)]
    public NGXCoordinatesNative InColorBeforeDepthOfFieldSubrectBase = new(in value.ColorBeforeDepthOfFieldSubrectBase);

    [FieldOffset(544)]
    public NGXCoordinatesNative InColorAfterDepthOfFieldSubrectBase = new(in value.ColorAfterDepthOfFieldSubrectBase);

    [FieldOffset(552)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);

    [FieldOffset(560)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);

    [FieldOffset(568)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);

    [FieldOffset(576)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);

    [FieldOffset(584)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);

    [FieldOffset(592)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);

    [FieldOffset(600)]
    public Matrix4x4* PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? scope.Alloc(value.WorldToViewMatrix.Value) : null;

    [FieldOffset(608)]
    public Matrix4x4* PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? scope.Alloc(value.ViewToClipMatrix.Value) : null;

    [FieldOffset(616)]
    public NGXVKGBufferNative GBufferSurface = new(in value.GBufferSurface, scope);

    [FieldOffset(752)]
    public NGXToneMapperType InToneMapperType = value.ToneMapperType;

    [FieldOffset(760)]
    public NGXResourceVKNative* PInMotionVectors3D = value.MotionVectors3D is NGXResourceVK motionVectors3D ? scope.Alloc(new NGXResourceVKNative(in motionVectors3D)) : null;

    [FieldOffset(768)]
    public NGXResourceVKNative* PInIsParticleMask = value.IsParticleMask is NGXResourceVK isParticleMask ? scope.Alloc(new NGXResourceVKNative(in isParticleMask)) : null;

    [FieldOffset(776)]
    public NGXResourceVKNative* PInAnimatedTextureMask = value.AnimatedTextureMask is NGXResourceVK animatedTextureMask ? scope.Alloc(new NGXResourceVKNative(in animatedTextureMask)) : null;

    [FieldOffset(784)]
    public NGXResourceVKNative* PInDepthHighRes = value.DepthHighResolution is NGXResourceVK depthHighResolution ? scope.Alloc(new NGXResourceVKNative(in depthHighResolution)) : null;

    [FieldOffset(792)]
    public NGXResourceVKNative* PInPositionViewSpace = value.PositionViewSpace is NGXResourceVK positionViewSpace ? scope.Alloc(new NGXResourceVKNative(in positionViewSpace)) : null;

    [FieldOffset(800)]
    public float InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

    [FieldOffset(808)]
    public NGXResourceVKNative* PInRayTracingHitDistance = value.RayTracingHitDistance is NGXResourceVK rayTracingHitDistance ? scope.Alloc(new NGXResourceVKNative(in rayTracingHitDistance)) : null;

    [FieldOffset(816)]
    public NGXResourceVKNative* PInMotionVectorsReflections = value.MotionVectorsReflections is NGXResourceVK motionVectorsReflections ? scope.Alloc(new NGXResourceVKNative(in motionVectorsReflections)) : null;

    [FieldOffset(824)]
    public NGXResourceVKNative* PInTransparencyLayer = value.TransparencyLayer is NGXResourceVK transparencyLayer ? scope.Alloc(new NGXResourceVKNative(in transparencyLayer)) : null;

    [FieldOffset(832)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);

    [FieldOffset(840)]
    public NGXResourceVKNative* PInTransparencyLayerOpacity = value.TransparencyLayerOpacity is NGXResourceVK transparencyLayerOpacity ? scope.Alloc(new NGXResourceVKNative(in transparencyLayerOpacity)) : null;

    [FieldOffset(848)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);

    [FieldOffset(856)]
    public NGXResourceVKNative* PInTransparencyLayerMvecs = value.TransparencyLayerMvecs is NGXResourceVK transparencyLayerMvecs ? scope.Alloc(new NGXResourceVKNative(in transparencyLayerMvecs)) : null;

    [FieldOffset(864)]
    public NGXCoordinatesNative InTransparencyLayerMvecsSubrectBase = new(in value.TransparencyLayerMvecsSubrectBase);

    [FieldOffset(872)]
    public NGXResourceVKNative* PInDisocclusionMask = value.DisocclusionMask is NGXResourceVK disocclusionMask ? scope.Alloc(new NGXResourceVKNative(in disocclusionMask)) : null;

    [FieldOffset(880)]
    public NGXCoordinatesNative InDisocclusionMaskSubrectBase = new(in value.DisocclusionMaskSubrectBase);

    [FieldOffset(888)]
    public NGXResourceVKNative* PInResponsivityMask = value.ResponsivityMask is NGXResourceVK responsivityMask ? scope.Alloc(new NGXResourceVKNative(in responsivityMask)) : null;

    [FieldOffset(896)]
    public NGXCoordinatesNative InResponsivityMaskSubrectBase = new(in value.ResponsivityMaskSubrectBase);
}
