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
}
