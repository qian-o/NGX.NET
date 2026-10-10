#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 368)]
internal unsafe struct NGXD3D11DLSSEvalParamsNative
{
    [FieldOffset(0)]
    public NGXD3D11FeatureEvalParamsNative Feature;

    [FieldOffset(24)]
    public nint PInDepth;

    [FieldOffset(32)]
    public nint PInMotionVectors;

    [FieldOffset(40)]
    public float InJitterOffsetX;

    [FieldOffset(44)]
    public float InJitterOffsetY;

    [FieldOffset(48)]
    public NGXDimensionsNative InRenderSubrectDimensions;

    [FieldOffset(56)]
    public int InReset;

    [FieldOffset(60)]
    public float InMVScaleX;

    [FieldOffset(64)]
    public float InMVScaleY;

    [FieldOffset(72)]
    public nint PInTransparencyMask;

    [FieldOffset(80)]
    public nint PInExposureTexture;

    [FieldOffset(88)]
    public nint PInBiasCurrentColorMask;

    [FieldOffset(96)]
    public NGXCoordinatesNative InColorSubrectBase;

    [FieldOffset(104)]
    public NGXCoordinatesNative InDepthSubrectBase;

    [FieldOffset(112)]
    public NGXCoordinatesNative InMVSubrectBase;

    [FieldOffset(120)]
    public NGXCoordinatesNative InTranslucencySubrectBase;

    [FieldOffset(128)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase;

    [FieldOffset(136)]
    public NGXCoordinatesNative InOutputSubrectBase;

    [FieldOffset(144)]
    public float InPreExposure;

    [FieldOffset(148)]
    public float InExposureScale;

    [FieldOffset(152)]
    public int InIndicatorInvertXAxis;

    [FieldOffset(156)]
    public int InIndicatorInvertYAxis;

    [FieldOffset(160)]
    public NGXD3D11GBufferNative GBufferSurface;

    [FieldOffset(296)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(304)]
    public nint PInMotionVectors3D;

    [FieldOffset(312)]
    public nint PInIsParticleMask;

    [FieldOffset(320)]
    public nint PInAnimatedTextureMask;

    [FieldOffset(328)]
    public nint PInDepthHighRes;

    [FieldOffset(336)]
    public nint PInPositionViewSpace;

    [FieldOffset(344)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(352)]
    public nint PInRayTracingHitDistance;

    [FieldOffset(360)]
    public nint PInMotionVectorsReflections;

    public NGXD3D11DLSSEvalParamsNative(in NGXD3D11DLSSEvalParams value)
    {
        Feature = new(in value.Feature);
        PInDepth = value.Depth;
        PInMotionVectors = value.MotionVectors;
        InJitterOffsetX = value.JitterOffsetX;
        InJitterOffsetY = value.JitterOffsetY;
        InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);
        InReset = value.Reset;
        InMVScaleX = value.MVScaleX;
        InMVScaleY = value.MVScaleY;
        PInTransparencyMask = value.TransparencyMask;
        PInExposureTexture = value.ExposureTexture;
        PInBiasCurrentColorMask = value.BiasCurrentColorMask;
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
        GBufferSurface = new(in value.GBufferSurface);
        InToneMapperType = value.ToneMapperType;
        PInMotionVectors3D = value.MotionVectors3D;
        PInIsParticleMask = value.IsParticleMask;
        PInAnimatedTextureMask = value.AnimatedTextureMask;
        PInDepthHighRes = value.DepthHighResolution;
        PInPositionViewSpace = value.PositionViewSpace;
        InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;
        PInRayTracingHitDistance = value.RayTracingHitDistance;
        PInMotionVectorsReflections = value.MotionVectorsReflections;
    }
}
