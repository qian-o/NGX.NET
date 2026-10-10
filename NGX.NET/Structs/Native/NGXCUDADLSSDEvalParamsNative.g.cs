namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 632)]
internal unsafe struct NGXCUDADLSSDEvalParamsNative(in NGXCUDADLSSDEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public void* PInDiffuseAlbedo = (void*)value.DiffuseAlbedo;

    [FieldOffset(8)]
    public void* PInSpecularAlbedo = (void*)value.SpecularAlbedo;

    [FieldOffset(16)]
    public void* PInNormals = (void*)value.Normals;

    [FieldOffset(24)]
    public void* PInRoughness = (void*)value.Roughness;

    [FieldOffset(32)]
    public void* PInColor = (void*)value.Color;

    [FieldOffset(40)]
    public void* PInOutput = (void*)value.Output;

    [FieldOffset(48)]
    public void* PInDepth = (void*)value.Depth;

    [FieldOffset(56)]
    public void* PInMotionVectors = (void*)value.MotionVectors;

    [FieldOffset(64)]
    public float InJitterOffsetX = value.JitterOffsetX;

    [FieldOffset(68)]
    public float InJitterOffsetY = value.JitterOffsetY;

    [FieldOffset(72)]
    public NGXDimensionsNative InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);

    [FieldOffset(80)]
    public int InReset = value.Reset;

    [FieldOffset(84)]
    public float InMVScaleX = value.MVScaleX;

    [FieldOffset(88)]
    public float InMVScaleY = value.MVScaleY;

    [FieldOffset(96)]
    public void* PInTransparencyMask = (void*)value.TransparencyMask;

    [FieldOffset(104)]
    public void* PInExposureTexture = (void*)value.ExposureTexture;

    [FieldOffset(112)]
    public void* PInBiasCurrentColorMask = (void*)value.BiasCurrentColorMask;

    [FieldOffset(120)]
    public NGXCoordinatesNative InDiffuseAlbedoSubrectBase = new(in value.DiffuseAlbedoSubrectBase);

    [FieldOffset(128)]
    public NGXCoordinatesNative InSpecularAlbedoSubrectBase = new(in value.SpecularAlbedoSubrectBase);

    [FieldOffset(136)]
    public NGXCoordinatesNative InNormalsSubrectBase = new(in value.NormalsSubrectBase);

    [FieldOffset(144)]
    public NGXCoordinatesNative InRoughnessSubrectBase = new(in value.RoughnessSubrectBase);

    [FieldOffset(152)]
    public NGXCoordinatesNative InColorSubrectBase = new(in value.ColorSubrectBase);

    [FieldOffset(160)]
    public NGXCoordinatesNative InDepthSubrectBase = new(in value.DepthSubrectBase);

    [FieldOffset(168)]
    public NGXCoordinatesNative InMVSubrectBase = new(in value.MVSubrectBase);

    [FieldOffset(176)]
    public NGXCoordinatesNative InTranslucencySubrectBase = new(in value.TranslucencySubrectBase);

    [FieldOffset(184)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase = new(in value.BiasCurrentColorSubrectBase);

    [FieldOffset(192)]
    public NGXCoordinatesNative InOutputSubrectBase = new(in value.OutputSubrectBase);

    [FieldOffset(200)]
    public void* PInReflectedAlbedo = (void*)value.ReflectedAlbedo;

    [FieldOffset(208)]
    public void* PInColorBeforeParticles = (void*)value.ColorBeforeParticles;

    [FieldOffset(216)]
    public void* PInColorBeforeTransparency = (void*)value.ColorBeforeTransparency;

    [FieldOffset(224)]
    public void* PInColorBeforeFog = (void*)value.ColorBeforeFog;

    [FieldOffset(232)]
    public void* PInDiffuseHitDistance = (void*)value.DiffuseHitDistance;

    [FieldOffset(240)]
    public void* PInSpecularHitDistance = (void*)value.SpecularHitDistance;

    [FieldOffset(248)]
    public void* PInDiffuseRayDirection = (void*)value.DiffuseRayDirection;

    [FieldOffset(256)]
    public void* PInSpecularRayDirection = (void*)value.SpecularRayDirection;

    [FieldOffset(264)]
    public void* PInDiffuseRayDirectionHitDistance = (void*)value.DiffuseRayDirectionHitDistance;

    [FieldOffset(272)]
    public void* PInSpecularRayDirectionHitDistance = (void*)value.SpecularRayDirectionHitDistance;

    [FieldOffset(280)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);

    [FieldOffset(288)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);

    [FieldOffset(296)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);

    [FieldOffset(304)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);

    [FieldOffset(312)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);

    [FieldOffset(320)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);

    [FieldOffset(328)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);

    [FieldOffset(336)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);

    [FieldOffset(344)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);

    [FieldOffset(352)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);

    [FieldOffset(360)]
    public Matrix4x4* PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? scope.Alloc(value.WorldToViewMatrix.Value) : null;

    [FieldOffset(368)]
    public Matrix4x4* PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? scope.Alloc(value.ViewToClipMatrix.Value) : null;

    [FieldOffset(376)]
    public float InPreExposure = value.PreExposure;

    [FieldOffset(380)]
    public float InExposureScale = value.ExposureScale;

    [FieldOffset(384)]
    public int InIndicatorInvertXAxis = value.IndicatorInvertXAxis;

    [FieldOffset(388)]
    public int InIndicatorInvertYAxis = value.IndicatorInvertYAxis;

    [FieldOffset(392)]
    public NGXCUDAGBufferNative GBufferSurface = new(in value.GBufferSurface, scope);

    [FieldOffset(528)]
    public NGXToneMapperType InToneMapperType = value.ToneMapperType;

    [FieldOffset(536)]
    public void* PInMotionVectors3D = (void*)value.MotionVectors3D;

    [FieldOffset(544)]
    public void* PInIsParticleMask = (void*)value.IsParticleMask;

    [FieldOffset(552)]
    public void* PInAnimatedTextureMask = (void*)value.AnimatedTextureMask;

    [FieldOffset(560)]
    public void* PInDepthHighRes = (void*)value.DepthHighResolution;

    [FieldOffset(568)]
    public void* PInPositionViewSpace = (void*)value.PositionViewSpace;

    [FieldOffset(576)]
    public float InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

    [FieldOffset(584)]
    public void* PInRayTracingHitDistance = (void*)value.RayTracingHitDistance;

    [FieldOffset(592)]
    public void* PInMotionVectorsReflections = (void*)value.MotionVectorsReflections;

    [FieldOffset(600)]
    public void* PInTransparencyLayer = (void*)value.TransparencyLayer;

    [FieldOffset(608)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);

    [FieldOffset(616)]
    public void* PInTransparencyLayerOpacity = (void*)value.TransparencyLayerOpacity;

    [FieldOffset(624)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);
}
