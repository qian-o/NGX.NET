#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 368)]
internal unsafe struct NGXVKDLSSEvalParamsNative
{
    [FieldOffset(0)]
    public NGXVKFeatureEvalParamsNative Feature;

    [FieldOffset(24)]
    public NGXResourceVKNative* PInDepth;

    [FieldOffset(32)]
    public NGXResourceVKNative* PInMotionVectors;

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
    public NGXResourceVKNative* PInTransparencyMask;

    [FieldOffset(80)]
    public NGXResourceVKNative* PInExposureTexture;

    [FieldOffset(88)]
    public NGXResourceVKNative* PInBiasCurrentColorMask;

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
    public NGXVKGBufferNative GBufferSurface;

    [FieldOffset(296)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(304)]
    public NGXResourceVKNative* PInMotionVectors3D;

    [FieldOffset(312)]
    public NGXResourceVKNative* PInIsParticleMask;

    [FieldOffset(320)]
    public NGXResourceVKNative* PInAnimatedTextureMask;

    [FieldOffset(328)]
    public NGXResourceVKNative* PInDepthHighRes;

    [FieldOffset(336)]
    public NGXResourceVKNative* PInPositionViewSpace;

    [FieldOffset(344)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(352)]
    public NGXResourceVKNative* PInRayTracingHitDistance;

    [FieldOffset(360)]
    public NGXResourceVKNative* PInMotionVectorsReflections;

    public NGXVKDLSSEvalParamsNative(in NGXVKDLSSEvalParams value, NativeScope scope)
    {
        Feature = new(in value.Feature, scope);
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
    }
}
