namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 904)]
internal unsafe struct NGXD3D12DLSSDEvalParamsNative(in NGXD3D12DLSSDEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public nint PInDiffuseAlbedo = value.DiffuseAlbedo;

    [FieldOffset(8)]
    public nint PInSpecularAlbedo = value.SpecularAlbedo;

    [FieldOffset(16)]
    public nint PInNormals = value.Normals;

    [FieldOffset(24)]
    public nint PInRoughness = value.Roughness;

    [FieldOffset(32)]
    public nint PInColor = value.Color;

    [FieldOffset(40)]
    public nint PInAlpha = value.Alpha;

    [FieldOffset(48)]
    public nint PInOutput = value.Output;

    [FieldOffset(56)]
    public nint PInOutputAlpha = value.OutputAlpha;

    [FieldOffset(64)]
    public nint PInDepth = value.Depth;

    [FieldOffset(72)]
    public nint PInMotionVectors = value.MotionVectors;

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
    public nint PInTransparencyMask = value.TransparencyMask;

    [FieldOffset(120)]
    public nint PInExposureTexture = value.ExposureTexture;

    [FieldOffset(128)]
    public nint PInBiasCurrentColorMask = value.BiasCurrentColorMask;

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
    public nint PInReflectedAlbedo = value.ReflectedAlbedo;

    [FieldOffset(240)]
    public nint PInColorBeforeParticles = value.ColorBeforeParticles;

    [FieldOffset(248)]
    public nint PInColorAfterParticles = value.ColorAfterParticles;

    [FieldOffset(256)]
    public nint PInColorBeforeTransparency = value.ColorBeforeTransparency;

    [FieldOffset(264)]
    public nint PInColorAfterTransparency = value.ColorAfterTransparency;

    [FieldOffset(272)]
    public nint PInColorBeforeFog = value.ColorBeforeFog;

    [FieldOffset(280)]
    public nint PInColorAfterFog = value.ColorAfterFog;

    [FieldOffset(288)]
    public nint PInScreenSpaceSubsurfaceScatteringGuide = value.ScreenSpaceSubsurfaceScatteringGuide;

    [FieldOffset(296)]
    public nint PInColorBeforeScreenSpaceSubsurfaceScattering = value.ColorBeforeScreenSpaceSubsurfaceScattering;

    [FieldOffset(304)]
    public nint PInColorAfterScreenSpaceSubsurfaceScattering = value.ColorAfterScreenSpaceSubsurfaceScattering;

    [FieldOffset(312)]
    public nint PInScreenSpaceRefractionGuide = value.ScreenSpaceRefractionGuide;

    [FieldOffset(320)]
    public nint PInColorBeforeScreenSpaceRefraction = value.ColorBeforeScreenSpaceRefraction;

    [FieldOffset(328)]
    public nint PInColorAfterScreenSpaceRefraction = value.ColorAfterScreenSpaceRefraction;

    [FieldOffset(336)]
    public nint PInDepthOfFieldGuide = value.DepthOfFieldGuide;

    [FieldOffset(344)]
    public nint PInColorBeforeDepthOfField = value.ColorBeforeDepthOfField;

    [FieldOffset(352)]
    public nint PInColorAfterDepthOfField = value.ColorAfterDepthOfField;

    [FieldOffset(360)]
    public nint PInDiffuseHitDistance = value.DiffuseHitDistance;

    [FieldOffset(368)]
    public nint PInSpecularHitDistance = value.SpecularHitDistance;

    [FieldOffset(376)]
    public nint PInDiffuseRayDirection = value.DiffuseRayDirection;

    [FieldOffset(384)]
    public nint PInSpecularRayDirection = value.SpecularRayDirection;

    [FieldOffset(392)]
    public nint PInDiffuseRayDirectionHitDistance = value.DiffuseRayDirectionHitDistance;

    [FieldOffset(400)]
    public nint PInSpecularRayDirectionHitDistance = value.SpecularRayDirectionHitDistance;

    [FieldOffset(408)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);

    [FieldOffset(416)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);

    [FieldOffset(424)]
    public NGXCoordinatesNative InColorAfterParticlesSubrectBase = new(in value.ColorAfterParticlesSubrectBase);

    [FieldOffset(432)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);

    [FieldOffset(440)]
    public NGXCoordinatesNative InColorAfterTransparencySubrectBase = new(in value.ColorAfterTransparencySubrectBase);

    [FieldOffset(448)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);

    [FieldOffset(456)]
    public NGXCoordinatesNative InColorAfterFogSubrectBase = new(in value.ColorAfterFogSubrectBase);

    [FieldOffset(464)]
    public NGXCoordinatesNative InScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in value.ScreenSpaceSubsurfaceScatteringGuideSubrectBase);

    [FieldOffset(472)]
    public NGXCoordinatesNative InScreenSpaceRefractionGuideSubrectBase = new(in value.ScreenSpaceRefractionGuideSubrectBase);

    [FieldOffset(480)]
    public NGXCoordinatesNative InDepthOfFieldGuideSubrectBase = new(in value.DepthOfFieldGuideSubrectBase);

    [FieldOffset(488)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);

    [FieldOffset(496)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);

    [FieldOffset(504)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);

    [FieldOffset(512)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);

    [FieldOffset(520)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);

    [FieldOffset(528)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);

    [FieldOffset(536)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);

    [FieldOffset(544)]
    public NGXCoordinatesNative InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);

    [FieldOffset(552)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceRefractionSubrectBase = new(in value.ColorBeforeScreenSpaceRefractionSubrectBase);

    [FieldOffset(560)]
    public NGXCoordinatesNative InColorAfterScreenSpaceRefractionSubrectBase = new(in value.ColorAfterScreenSpaceRefractionSubrectBase);

    [FieldOffset(568)]
    public NGXCoordinatesNative InColorBeforeDepthOfFieldSubrectBase = new(in value.ColorBeforeDepthOfFieldSubrectBase);

    [FieldOffset(576)]
    public NGXCoordinatesNative InColorAfterDepthOfFieldSubtectBase = new(in value.ColorAfterDepthOfFieldSubrectBase);

    [FieldOffset(584)]
    public Matrix4x4* PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? scope.Alloc(value.WorldToViewMatrix.Value) : null;

    [FieldOffset(592)]
    public Matrix4x4* PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? scope.Alloc(value.ViewToClipMatrix.Value) : null;

    [FieldOffset(600)]
    public float InPreExposure = value.PreExposure;

    [FieldOffset(604)]
    public float InExposureScale = value.ExposureScale;

    [FieldOffset(608)]
    public int InIndicatorInvertXAxis = value.IndicatorInvertXAxis;

    [FieldOffset(612)]
    public int InIndicatorInvertYAxis = value.IndicatorInvertYAxis;

    [FieldOffset(616)]
    public NGXD3D12GBufferNative GBufferSurface = new(in value.GBufferSurface);

    [FieldOffset(752)]
    public NGXToneMapperType InToneMapperType = value.ToneMapperType;

    [FieldOffset(760)]
    public nint PInMotionVectors3D = value.MotionVectors3D;

    [FieldOffset(768)]
    public nint PInIsParticleMask = value.IsParticleMask;

    [FieldOffset(776)]
    public nint PInAnimatedTextureMask = value.AnimatedTextureMask;

    [FieldOffset(784)]
    public nint PInDepthHighRes = value.DepthHighResolution;

    [FieldOffset(792)]
    public nint PInPositionViewSpace = value.PositionViewSpace;

    [FieldOffset(800)]
    public float InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

    [FieldOffset(808)]
    public nint PInRayTracingHitDistance = value.RayTracingHitDistance;

    [FieldOffset(816)]
    public nint PInMotionVectorsReflections = value.MotionVectorsReflections;

    [FieldOffset(824)]
    public nint PInTransparencyLayer = value.TransparencyLayer;

    [FieldOffset(832)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);

    [FieldOffset(840)]
    public nint PInTransparencyLayerOpacity = value.TransparencyLayerOpacity;

    [FieldOffset(848)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);

    [FieldOffset(856)]
    public nint PInTransparencyLayerMvecs = value.TransparencyLayerMvecs;

    [FieldOffset(864)]
    public NGXCoordinatesNative InTransparencyLayerMvecsSubrectBase = new(in value.TransparencyLayerMvecsSubrectBase);

    [FieldOffset(872)]
    public nint PInDisocclusionMask = value.DisocclusionMask;

    [FieldOffset(880)]
    public NGXCoordinatesNative InDisocclusionMaskSubrectBase = new(in value.DisocclusionMaskSubrectBase);

    [FieldOffset(888)]
    public nint PInResponsivityMask = value.ResponsivityMask;

    [FieldOffset(896)]
    public NGXCoordinatesNative InResponsivityMaskSubrectBase = new(in value.ResponsivityMaskSubrectBase);
}
