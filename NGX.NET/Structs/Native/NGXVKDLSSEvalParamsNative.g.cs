#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 368)]
internal unsafe struct NGXVKDLSSEvalParamsNative : IDisposable
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

    public NGXVKDLSSEvalParamsNative(in NGXVKDLSSEvalParams value)
    {
        this = default;

        try
        {
            Feature = new(in value.Feature);

            if (value.Depth is NGXResourceVK depth)
            {
                PInDepth = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in depth));
            }

            if (value.MotionVectors is NGXResourceVK motionVectors)
            {
                PInMotionVectors = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in motionVectors));
            }

            InJitterOffsetX = value.JitterOffsetX;
            InJitterOffsetY = value.JitterOffsetY;
            InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);
            InReset = value.Reset;
            InMVScaleX = value.MVScaleX;
            InMVScaleY = value.MVScaleY;

            if (value.TransparencyMask is NGXResourceVK transparencyMask)
            {
                PInTransparencyMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in transparencyMask));
            }

            if (value.ExposureTexture is NGXResourceVK exposureTexture)
            {
                PInExposureTexture = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in exposureTexture));
            }

            if (value.BiasCurrentColorMask is NGXResourceVK biasCurrentColorMask)
            {
                PInBiasCurrentColorMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in biasCurrentColorMask));
            }

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

            if (value.MotionVectors3D is NGXResourceVK motionVectors3D)
            {
                PInMotionVectors3D = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in motionVectors3D));
            }

            if (value.IsParticleMask is NGXResourceVK isParticleMask)
            {
                PInIsParticleMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in isParticleMask));
            }

            if (value.AnimatedTextureMask is NGXResourceVK animatedTextureMask)
            {
                PInAnimatedTextureMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in animatedTextureMask));
            }

            if (value.DepthHighResolution is NGXResourceVK depthHighResolution)
            {
                PInDepthHighRes = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in depthHighResolution));
            }

            if (value.PositionViewSpace is NGXResourceVK positionViewSpace)
            {
                PInPositionViewSpace = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in positionViewSpace));
            }

            InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;

            if (value.RayTracingHitDistance is NGXResourceVK rayTracingHitDistance)
            {
                PInRayTracingHitDistance = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in rayTracingHitDistance));
            }

            if (value.MotionVectorsReflections is NGXResourceVK motionVectorsReflections)
            {
                PInMotionVectorsReflections = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in motionVectorsReflections));
            }
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NGXMarshal.FreeNative(PInMotionVectorsReflections);
        NGXMarshal.FreeNative(PInRayTracingHitDistance);
        NGXMarshal.FreeNative(PInPositionViewSpace);
        NGXMarshal.FreeNative(PInDepthHighRes);
        NGXMarshal.FreeNative(PInAnimatedTextureMask);
        NGXMarshal.FreeNative(PInIsParticleMask);
        NGXMarshal.FreeNative(PInMotionVectors3D);
        GBufferSurface.Dispose();
        InOutputSubrectBase.Dispose();
        InBiasCurrentColorSubrectBase.Dispose();
        InTranslucencySubrectBase.Dispose();
        InMVSubrectBase.Dispose();
        InDepthSubrectBase.Dispose();
        InColorSubrectBase.Dispose();
        NGXMarshal.FreeNative(PInBiasCurrentColorMask);
        NGXMarshal.FreeNative(PInExposureTexture);
        NGXMarshal.FreeNative(PInTransparencyMask);
        InRenderSubrectDimensions.Dispose();
        NGXMarshal.FreeNative(PInMotionVectors);
        NGXMarshal.FreeNative(PInDepth);
        Feature.Dispose();
        this = default;
    }
}
