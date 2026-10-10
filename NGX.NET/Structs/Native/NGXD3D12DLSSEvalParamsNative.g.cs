namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 368)]
internal unsafe struct NGXD3D12DLSSEvalParamsNative(in NGXD3D12DLSSEvalParams value)
{
    [FieldOffset(0)]
    public NGXD3D12FeatureEvalParamsNative Feature = new(in value.Feature);

    [FieldOffset(24)]
    public nint PInDepth = value.Depth;

    [FieldOffset(32)]
    public nint PInMotionVectors = value.MotionVectors;

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
    public nint PInTransparencyMask = value.TransparencyMask;

    [FieldOffset(80)]
    public nint PInExposureTexture = value.ExposureTexture;

    [FieldOffset(88)]
    public nint PInBiasCurrentColorMask = value.BiasCurrentColorMask;

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
    public NGXD3D12GBufferNative GBufferSurface = new(in value.GBufferSurface);

    [FieldOffset(296)]
    public NGXToneMapperType InToneMapperType = value.ToneMapperType;

    [FieldOffset(304)]
    public nint PInMotionVectors3D = value.MotionVectors3D;

    [FieldOffset(312)]
    public nint PInIsParticleMask = value.IsParticleMask;

    [FieldOffset(320)]
    public nint PInAnimatedTextureMask = value.AnimatedTextureMask;

    [FieldOffset(328)]
    public nint PInDepthHighRes = value.DepthHighResolution;

    [FieldOffset(336)]
    public nint PInPositionViewSpace = value.PositionViewSpace;

    [FieldOffset(344)]
    public float InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

    [FieldOffset(352)]
    public nint PInRayTracingHitDistance = value.RayTracingHitDistance;

    [FieldOffset(360)]
    public nint PInMotionVectorsReflections = value.MotionVectorsReflections;
}
