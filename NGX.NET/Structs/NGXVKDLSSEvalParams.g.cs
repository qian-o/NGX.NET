#nullable enable

namespace NGX.NET;

public struct NGXVKDLSSEvalParams
{
    public NGXVKFeatureEvalParams Feature;

    public NGXResourceVK? Depth;

    public NGXResourceVK? MotionVectors;

    public float JitterOffsetX;

    public float JitterOffsetY;

    public NGXDimensions RenderSubrectDimensions;

    public int Reset;

    public float MVScaleX;

    public float MVScaleY;

    public NGXResourceVK? TransparencyMask;

    public NGXResourceVK? ExposureTexture;

    public NGXResourceVK? BiasCurrentColorMask;

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

    public NGXVKGBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public NGXResourceVK? MotionVectors3D;

    public NGXResourceVK? IsParticleMask;

    public NGXResourceVK? AnimatedTextureMask;

    public NGXResourceVK? DepthHighResolution;

    public NGXResourceVK? PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public NGXResourceVK? RayTracingHitDistance;

    public NGXResourceVK? MotionVectorsReflections;

    internal unsafe NGXVKDLSSEvalParams(in NGXVKDLSSEvalParamsNative native)
    {
        Feature = new(in native.Feature);
        Depth = native.PInDepth is null ? null : new NGXResourceVK(in *native.PInDepth);
        MotionVectors = native.PInMotionVectors is null ? null : new NGXResourceVK(in *native.PInMotionVectors);
        JitterOffsetX = native.InJitterOffsetX;
        JitterOffsetY = native.InJitterOffsetY;
        RenderSubrectDimensions = new(in native.InRenderSubrectDimensions);
        Reset = native.InReset;
        MVScaleX = native.InMVScaleX;
        MVScaleY = native.InMVScaleY;
        TransparencyMask = native.PInTransparencyMask is null ? null : new NGXResourceVK(in *native.PInTransparencyMask);
        ExposureTexture = native.PInExposureTexture is null ? null : new NGXResourceVK(in *native.PInExposureTexture);
        BiasCurrentColorMask = native.PInBiasCurrentColorMask is null ? null : new NGXResourceVK(in *native.PInBiasCurrentColorMask);
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
        MotionVectors3D = native.PInMotionVectors3D is null ? null : new NGXResourceVK(in *native.PInMotionVectors3D);
        IsParticleMask = native.PInIsParticleMask is null ? null : new NGXResourceVK(in *native.PInIsParticleMask);
        AnimatedTextureMask = native.PInAnimatedTextureMask is null ? null : new NGXResourceVK(in *native.PInAnimatedTextureMask);
        DepthHighResolution = native.PInDepthHighRes is null ? null : new NGXResourceVK(in *native.PInDepthHighRes);
        PositionViewSpace = native.PInPositionViewSpace is null ? null : new NGXResourceVK(in *native.PInPositionViewSpace);
        FrameTimeDeltaInMsec = native.InFrameTimeDeltaInMsec;
        RayTracingHitDistance = native.PInRayTracingHitDistance is null ? null : new NGXResourceVK(in *native.PInRayTracingHitDistance);
        MotionVectorsReflections = native.PInMotionVectorsReflections is null ? null : new NGXResourceVK(in *native.PInMotionVectorsReflections);
    }
}
