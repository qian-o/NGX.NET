namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 368)]
internal unsafe struct NGXVKDLSSEvalParamsNative(in NGXVKDLSSEvalParams value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXVKFeatureEvalParamsNative Feature = new(in value.Feature, scope);

    [FieldOffset(24)]
    public NGXResourceVKNative* PInDepth = value.Depth is NGXResourceVK depth ? scope.Alloc(new NGXResourceVKNative(in depth)) : null;

    [FieldOffset(32)]
    public NGXResourceVKNative* PInMotionVectors = value.MotionVectors is NGXResourceVK motionVectors ? scope.Alloc(new NGXResourceVKNative(in motionVectors)) : null;

    [FieldOffset(40)]
    public float InJitterOffsetX = value.JitterOffsetX;

    [FieldOffset(44)]
    public float InJitterOffsetY = value.JitterOffsetY;

    [FieldOffset(48)]
    public NGXDimensionsNative InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);

    [FieldOffset(56)]
    public int InReset = value.Reset;

    [FieldOffset(60)]
    public float InMVScaleX = value.MVScaleX;

    [FieldOffset(64)]
    public float InMVScaleY = value.MVScaleY;

    [FieldOffset(72)]
    public NGXResourceVKNative* PInTransparencyMask = value.TransparencyMask is NGXResourceVK transparencyMask ? scope.Alloc(new NGXResourceVKNative(in transparencyMask)) : null;

    [FieldOffset(80)]
    public NGXResourceVKNative* PInExposureTexture = value.ExposureTexture is NGXResourceVK exposureTexture ? scope.Alloc(new NGXResourceVKNative(in exposureTexture)) : null;

    [FieldOffset(88)]
    public NGXResourceVKNative* PInBiasCurrentColorMask = value.BiasCurrentColorMask is NGXResourceVK biasCurrentColorMask ? scope.Alloc(new NGXResourceVKNative(in biasCurrentColorMask)) : null;

    [FieldOffset(96)]
    public NGXCoordinatesNative InColorSubrectBase = new(in value.ColorSubrectBase);

    [FieldOffset(104)]
    public NGXCoordinatesNative InDepthSubrectBase = new(in value.DepthSubrectBase);

    [FieldOffset(112)]
    public NGXCoordinatesNative InMVSubrectBase = new(in value.MVSubrectBase);

    [FieldOffset(120)]
    public NGXCoordinatesNative InTranslucencySubrectBase = new(in value.TranslucencySubrectBase);

    [FieldOffset(128)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase = new(in value.BiasCurrentColorSubrectBase);

    [FieldOffset(136)]
    public NGXCoordinatesNative InOutputSubrectBase = new(in value.OutputSubrectBase);

    [FieldOffset(144)]
    public float InPreExposure = value.PreExposure;

    [FieldOffset(148)]
    public float InExposureScale = value.ExposureScale;

    [FieldOffset(152)]
    public int InIndicatorInvertXAxis = value.IndicatorInvertXAxis;

    [FieldOffset(156)]
    public int InIndicatorInvertYAxis = value.IndicatorInvertYAxis;

    [FieldOffset(160)]
    public NGXVKGBufferNative GBufferSurface = new(in value.GBufferSurface, scope);

    [FieldOffset(296)]
    public NGXToneMapperType InToneMapperType = value.ToneMapperType;

    [FieldOffset(304)]
    public NGXResourceVKNative* PInMotionVectors3D = value.MotionVectors3D is NGXResourceVK motionVectors3D ? scope.Alloc(new NGXResourceVKNative(in motionVectors3D)) : null;

    [FieldOffset(312)]
    public NGXResourceVKNative* PInIsParticleMask = value.IsParticleMask is NGXResourceVK isParticleMask ? scope.Alloc(new NGXResourceVKNative(in isParticleMask)) : null;

    [FieldOffset(320)]
    public NGXResourceVKNative* PInAnimatedTextureMask = value.AnimatedTextureMask is NGXResourceVK animatedTextureMask ? scope.Alloc(new NGXResourceVKNative(in animatedTextureMask)) : null;

    [FieldOffset(328)]
    public NGXResourceVKNative* PInDepthHighRes = value.DepthHighResolution is NGXResourceVK depthHighResolution ? scope.Alloc(new NGXResourceVKNative(in depthHighResolution)) : null;

    [FieldOffset(336)]
    public NGXResourceVKNative* PInPositionViewSpace = value.PositionViewSpace is NGXResourceVK positionViewSpace ? scope.Alloc(new NGXResourceVKNative(in positionViewSpace)) : null;

    [FieldOffset(344)]
    public float InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

    [FieldOffset(352)]
    public NGXResourceVKNative* PInRayTracingHitDistance = value.RayTracingHitDistance is NGXResourceVK rayTracingHitDistance ? scope.Alloc(new NGXResourceVKNative(in rayTracingHitDistance)) : null;

    [FieldOffset(360)]
    public NGXResourceVKNative* PInMotionVectorsReflections = value.MotionVectorsReflections is NGXResourceVK motionVectorsReflections ? scope.Alloc(new NGXResourceVKNative(in motionVectorsReflections)) : null;
}
