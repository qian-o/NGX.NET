namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXCUDADLSSDEvalParams
{
    public void* DiffuseAlbedo;

    public void* SpecularAlbedo;

    public void* Normals;

    public void* Roughness;

    public void* Color;

    public void* Output;

    public void* Depth;

    public void* MotionVectors;

    public float JitterOffsetX;

    public float JitterOffsetY;

    public NGXDimensions RenderSubrectDimensions;

    public int Reset;

    public float MVScaleX;

    public float MVScaleY;

    public void* TransparencyMask;

    public void* ExposureTexture;

    public void* BiasCurrentColorMask;

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

    public void* ReflectedAlbedo;

    public void* ColorBeforeParticles;

    public void* ColorBeforeTransparency;

    public void* ColorBeforeFog;

    public void* DiffuseHitDistance;

    public void* SpecularHitDistance;

    public void* DiffuseRayDirection;

    public void* SpecularRayDirection;

    public void* DiffuseRayDirectionHitDistance;

    public void* SpecularRayDirectionHitDistance;

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

    public Matrix4x4* WorldToViewMatrix;

    public Matrix4x4* ViewToClipMatrix;

    public float PreExposure;

    public float ExposureScale;

    public int IndicatorInvertXAxis;

    public int IndicatorInvertYAxis;

    public NGXCUDAGBuffer GBufferSurface;

    public NGXToneMapperType ToneMapperType;

    public void* MotionVectors3D;

    public void* IsParticleMask;

    public void* AnimatedTextureMask;

    public void* DepthHighResolution;

    public void* PositionViewSpace;

    public float FrameTimeDeltaInMsec;

    public void* RayTracingHitDistance;

    public void* MotionVectorsReflections;

    public void* TransparencyLayer;

    public NGXCoordinates TransparencyLayerSubrectBase;

    public void* TransparencyLayerOpacity;

    public NGXCoordinates TransparencyLayerOpacitySubrectBase;
}
