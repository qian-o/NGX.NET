namespace NGX.NET;

public struct NGXCUDADLSSDEvalParams
{
    public nint DiffuseAlbedo;

    public nint SpecularAlbedo;

    public nint Normals;

    public nint Roughness;

    public nint Color;

    public nint Output;

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

    public NGXCoordinates DiffuseAlbedoSubrectBase;

    public NGXCoordinates SpecularAlbedoSubrectBase;

    public NGXCoordinates NormalsSubrectBase;

    public NGXCoordinates RoughnessSubrectBase;

    public NGXCoordinates ColorSubrectBase;

    public NGXCoordinates DepthSubrectBase;

    public NGXCoordinates MVSubrectBase;

    public NGXCoordinates TranslucencySubrectBase;

    public NGXCoordinates BiasCurrentColorSubrectBase;

    public NGXCoordinates OutputSubrectBase;

    public nint ReflectedAlbedo;

    public nint ColorBeforeParticles;

    public nint ColorBeforeTransparency;

    public nint ColorBeforeFog;

    public nint DiffuseHitDistance;

    public nint SpecularHitDistance;

    public nint DiffuseRayDirection;

    public nint SpecularRayDirection;

    public nint DiffuseRayDirectionHitDistance;

    public nint SpecularRayDirectionHitDistance;

    public NGXCoordinates ReflectedAlbedoSubrectBase;

    public NGXCoordinates ColorBeforeParticlesSubrectBase;

    public NGXCoordinates ColorBeforeTransparencySubrectBase;

    public NGXCoordinates ColorBeforeFogSubrectBase;

    public NGXCoordinates DiffuseHitDistanceSubrectBase;

    public NGXCoordinates SpecularHitDistanceSubrectBase;

    public NGXCoordinates DiffuseRayDirectionSubrectBase;

    public NGXCoordinates SpecularRayDirectionSubrectBase;

    public NGXCoordinates DiffuseRayDirectionHitDistanceSubrectBase;

    public NGXCoordinates SpecularRayDirectionHitDistanceSubrectBase;

    public Matrix4x4? WorldToViewMatrix;

    public Matrix4x4? ViewToClipMatrix;

    public float PreExposure;

    public float ExposureScale;

    public int IndicatorInvertXAxis;

    public int IndicatorInvertYAxis;

    public NGXCUDAGBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public nint MotionVectors3D;

    public nint IsParticleMask;

    public nint AnimatedTextureMask;

    public nint DepthHighResolution;

    public nint PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public nint RayTracingHitDistance;

    public nint MotionVectorsReflections;

    public nint TransparencyLayer;

    public NGXCoordinates TransparencyLayerSubrectBase;

    public nint TransparencyLayerOpacity;

    public NGXCoordinates TransparencyLayerOpacitySubrectBase;
}
