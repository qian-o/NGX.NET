#nullable enable

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

    internal unsafe NGXCUDADLSSDEvalParams(in NGXCUDADLSSDEvalParamsNative native)
    {
        DiffuseAlbedo = (nint)native.PInDiffuseAlbedo;
        SpecularAlbedo = (nint)native.PInSpecularAlbedo;
        Normals = (nint)native.PInNormals;
        Roughness = (nint)native.PInRoughness;
        Color = (nint)native.PInColor;
        Output = (nint)native.PInOutput;
        Depth = (nint)native.PInDepth;
        MotionVectors = (nint)native.PInMotionVectors;
        JitterOffsetX = native.InJitterOffsetX;
        JitterOffsetY = native.InJitterOffsetY;
        RenderSubrectDimensions = new(in native.InRenderSubrectDimensions);
        Reset = native.InReset;
        MVScaleX = native.InMVScaleX;
        MVScaleY = native.InMVScaleY;
        TransparencyMask = (nint)native.PInTransparencyMask;
        ExposureTexture = (nint)native.PInExposureTexture;
        BiasCurrentColorMask = (nint)native.PInBiasCurrentColorMask;
        DiffuseAlbedoSubrectBase = new(in native.InDiffuseAlbedoSubrectBase);
        SpecularAlbedoSubrectBase = new(in native.InSpecularAlbedoSubrectBase);
        NormalsSubrectBase = new(in native.InNormalsSubrectBase);
        RoughnessSubrectBase = new(in native.InRoughnessSubrectBase);
        ColorSubrectBase = new(in native.InColorSubrectBase);
        DepthSubrectBase = new(in native.InDepthSubrectBase);
        MVSubrectBase = new(in native.InMVSubrectBase);
        TranslucencySubrectBase = new(in native.InTranslucencySubrectBase);
        BiasCurrentColorSubrectBase = new(in native.InBiasCurrentColorSubrectBase);
        OutputSubrectBase = new(in native.InOutputSubrectBase);
        ReflectedAlbedo = (nint)native.PInReflectedAlbedo;
        ColorBeforeParticles = (nint)native.PInColorBeforeParticles;
        ColorBeforeTransparency = (nint)native.PInColorBeforeTransparency;
        ColorBeforeFog = (nint)native.PInColorBeforeFog;
        DiffuseHitDistance = (nint)native.PInDiffuseHitDistance;
        SpecularHitDistance = (nint)native.PInSpecularHitDistance;
        DiffuseRayDirection = (nint)native.PInDiffuseRayDirection;
        SpecularRayDirection = (nint)native.PInSpecularRayDirection;
        DiffuseRayDirectionHitDistance = (nint)native.PInDiffuseRayDirectionHitDistance;
        SpecularRayDirectionHitDistance = (nint)native.PInSpecularRayDirectionHitDistance;
        ReflectedAlbedoSubrectBase = new(in native.InReflectedAlbedoSubrectBase);
        ColorBeforeParticlesSubrectBase = new(in native.InColorBeforeParticlesSubrectBase);
        ColorBeforeTransparencySubrectBase = new(in native.InColorBeforeTransparencySubrectBase);
        ColorBeforeFogSubrectBase = new(in native.InColorBeforeFogSubrectBase);
        DiffuseHitDistanceSubrectBase = new(in native.InDiffuseHitDistanceSubrectBase);
        SpecularHitDistanceSubrectBase = new(in native.InSpecularHitDistanceSubrectBase);
        DiffuseRayDirectionSubrectBase = new(in native.InDiffuseRayDirectionSubrectBase);
        SpecularRayDirectionSubrectBase = new(in native.InSpecularRayDirectionSubrectBase);
        DiffuseRayDirectionHitDistanceSubrectBase = new(in native.InDiffuseRayDirectionHitDistanceSubrectBase);
        SpecularRayDirectionHitDistanceSubrectBase = new(in native.InSpecularRayDirectionHitDistanceSubrectBase);
        WorldToViewMatrix = native.PInWorldToViewMatrix is null ? null : *native.PInWorldToViewMatrix;
        ViewToClipMatrix = native.PInViewToClipMatrix is null ? null : *native.PInViewToClipMatrix;
        PreExposure = native.InPreExposure;
        ExposureScale = native.InExposureScale;
        IndicatorInvertXAxis = native.InIndicatorInvertXAxis;
        IndicatorInvertYAxis = native.InIndicatorInvertYAxis;
        GBufferSurface = new(in native.GBufferSurface);
        ToneMapperType = native.InToneMapperType;
        MotionVectors3D = (nint)native.PInMotionVectors3D;
        IsParticleMask = (nint)native.PInIsParticleMask;
        AnimatedTextureMask = (nint)native.PInAnimatedTextureMask;
        DepthHighResolution = (nint)native.PInDepthHighRes;
        PositionViewSpace = (nint)native.PInPositionViewSpace;
        FrameTimeDeltaInMsec = native.InFrameTimeDeltaInMsec;
        RayTracingHitDistance = (nint)native.PInRayTracingHitDistance;
        MotionVectorsReflections = (nint)native.PInMotionVectorsReflections;
        TransparencyLayer = (nint)native.PInTransparencyLayer;
        TransparencyLayerSubrectBase = new(in native.InTransparencyLayerSubrectBase);
        TransparencyLayerOpacity = (nint)native.PInTransparencyLayerOpacity;
        TransparencyLayerOpacitySubrectBase = new(in native.InTransparencyLayerOpacitySubrectBase);
    }
}
