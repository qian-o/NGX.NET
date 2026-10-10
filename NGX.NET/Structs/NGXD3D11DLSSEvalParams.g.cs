#nullable enable

namespace NGX.NET;

public struct NGXD3D11DLSSEvalParams
{
    public NGXD3D11FeatureEvalParams Feature;

    public nint Depth;

    public nint MotionVectors;

    public float JitterOffsetX;

    public float JitterOffsetY;

    public NGXDimensions RenderSubrectDimensions;

    public int Reset;

    public float MVScaleX;

    public float MVScaleY;

    public nint TransparencyMask;

    public nint ExposureTexture;

    public nint BiasCurrentColorMask;

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

    public NGXD3D11GBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public nint MotionVectors3D;

    public nint IsParticleMask;

    public nint AnimatedTextureMask;

    public nint DepthHighResolution;

    public nint PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public nint RayTracingHitDistance;

    public nint MotionVectorsReflections;

    internal unsafe NGXD3D11DLSSEvalParams(in NGXD3D11DLSSEvalParamsNative native)
    {
        Feature = new(in native.Feature);
        Depth = native.PInDepth;
        MotionVectors = native.PInMotionVectors;
        JitterOffsetX = native.InJitterOffsetX;
        JitterOffsetY = native.InJitterOffsetY;
        RenderSubrectDimensions = new(in native.InRenderSubrectDimensions);
        Reset = native.InReset;
        MVScaleX = native.InMVScaleX;
        MVScaleY = native.InMVScaleY;
        TransparencyMask = native.PInTransparencyMask;
        ExposureTexture = native.PInExposureTexture;
        BiasCurrentColorMask = native.PInBiasCurrentColorMask;
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
        GBufferSurface = new(in native.GBufferSurface);
        ToneMapperType = native.InToneMapperType;
        MotionVectors3D = native.PInMotionVectors3D;
        IsParticleMask = native.PInIsParticleMask;
        AnimatedTextureMask = native.PInAnimatedTextureMask;
        DepthHighResolution = native.PInDepthHighRes;
        PositionViewSpace = native.PInPositionViewSpace;
        FrameTimeDeltaInMsec = native.InFrameTimeDeltaInMsec;
        RayTracingHitDistance = native.PInRayTracingHitDistance;
        MotionVectorsReflections = native.PInMotionVectorsReflections;
    }
}
