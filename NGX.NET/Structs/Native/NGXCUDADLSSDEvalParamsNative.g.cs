#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 632)]
internal unsafe struct NGXCUDADLSSDEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public void* PInDiffuseAlbedo;

    [FieldOffset(8)]
    public void* PInSpecularAlbedo;

    [FieldOffset(16)]
    public void* PInNormals;

    [FieldOffset(24)]
    public void* PInRoughness;

    [FieldOffset(32)]
    public void* PInColor;

    [FieldOffset(40)]
    public void* PInOutput;

    [FieldOffset(48)]
    public void* PInDepth;

    [FieldOffset(56)]
    public void* PInMotionVectors;

    [FieldOffset(64)]
    public float InJitterOffsetX;

    [FieldOffset(68)]
    public float InJitterOffsetY;

    [FieldOffset(72)]
    public NGXDimensionsNative InRenderSubrectDimensions;

    [FieldOffset(80)]
    public int InReset;

    [FieldOffset(84)]
    public float InMVScaleX;

    [FieldOffset(88)]
    public float InMVScaleY;

    [FieldOffset(96)]
    public void* PInTransparencyMask;

    [FieldOffset(104)]
    public void* PInExposureTexture;

    [FieldOffset(112)]
    public void* PInBiasCurrentColorMask;

    [FieldOffset(120)]
    public NGXCoordinatesNative InDiffuseAlbedoSubrectBase;

    [FieldOffset(128)]
    public NGXCoordinatesNative InSpecularAlbedoSubrectBase;

    [FieldOffset(136)]
    public NGXCoordinatesNative InNormalsSubrectBase;

    [FieldOffset(144)]
    public NGXCoordinatesNative InRoughnessSubrectBase;

    [FieldOffset(152)]
    public NGXCoordinatesNative InColorSubrectBase;

    [FieldOffset(160)]
    public NGXCoordinatesNative InDepthSubrectBase;

    [FieldOffset(168)]
    public NGXCoordinatesNative InMVSubrectBase;

    [FieldOffset(176)]
    public NGXCoordinatesNative InTranslucencySubrectBase;

    [FieldOffset(184)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase;

    [FieldOffset(192)]
    public NGXCoordinatesNative InOutputSubrectBase;

    [FieldOffset(200)]
    public void* PInReflectedAlbedo;

    [FieldOffset(208)]
    public void* PInColorBeforeParticles;

    [FieldOffset(216)]
    public void* PInColorBeforeTransparency;

    [FieldOffset(224)]
    public void* PInColorBeforeFog;

    [FieldOffset(232)]
    public void* PInDiffuseHitDistance;

    [FieldOffset(240)]
    public void* PInSpecularHitDistance;

    [FieldOffset(248)]
    public void* PInDiffuseRayDirection;

    [FieldOffset(256)]
    public void* PInSpecularRayDirection;

    [FieldOffset(264)]
    public void* PInDiffuseRayDirectionHitDistance;

    [FieldOffset(272)]
    public void* PInSpecularRayDirectionHitDistance;

    [FieldOffset(280)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase;

    [FieldOffset(288)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase;

    [FieldOffset(296)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase;

    [FieldOffset(304)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase;

    [FieldOffset(312)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase;

    [FieldOffset(320)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase;

    [FieldOffset(328)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase;

    [FieldOffset(336)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase;

    [FieldOffset(344)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase;

    [FieldOffset(352)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase;

    [FieldOffset(360)]
    public Matrix4x4* PInWorldToViewMatrix;

    [FieldOffset(368)]
    public Matrix4x4* PInViewToClipMatrix;

    [FieldOffset(376)]
    public float InPreExposure;

    [FieldOffset(380)]
    public float InExposureScale;

    [FieldOffset(384)]
    public int InIndicatorInvertXAxis;

    [FieldOffset(388)]
    public int InIndicatorInvertYAxis;

    [FieldOffset(392)]
    public NGXCUDAGBufferNative GBufferSurface;

    [FieldOffset(528)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(536)]
    public void* PInMotionVectors3D;

    [FieldOffset(544)]
    public void* PInIsParticleMask;

    [FieldOffset(552)]
    public void* PInAnimatedTextureMask;

    [FieldOffset(560)]
    public void* PInDepthHighRes;

    [FieldOffset(568)]
    public void* PInPositionViewSpace;

    [FieldOffset(576)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(584)]
    public void* PInRayTracingHitDistance;

    [FieldOffset(592)]
    public void* PInMotionVectorsReflections;

    [FieldOffset(600)]
    public void* PInTransparencyLayer;

    [FieldOffset(608)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase;

    [FieldOffset(616)]
    public void* PInTransparencyLayerOpacity;

    [FieldOffset(624)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase;

    public NGXCUDADLSSDEvalParamsNative(in NGXCUDADLSSDEvalParams value)
    {
        try
        {
            PInDiffuseAlbedo = (void*)value.DiffuseAlbedo;
            PInSpecularAlbedo = (void*)value.SpecularAlbedo;
            PInNormals = (void*)value.Normals;
            PInRoughness = (void*)value.Roughness;
            PInColor = (void*)value.Color;
            PInOutput = (void*)value.Output;
            PInDepth = (void*)value.Depth;
            PInMotionVectors = (void*)value.MotionVectors;
            InJitterOffsetX = value.JitterOffsetX;
            InJitterOffsetY = value.JitterOffsetY;
            InRenderSubrectDimensions = new(in value.RenderSubrectDimensions);
            InReset = value.Reset;
            InMVScaleX = value.MVScaleX;
            InMVScaleY = value.MVScaleY;
            PInTransparencyMask = (void*)value.TransparencyMask;
            PInExposureTexture = (void*)value.ExposureTexture;
            PInBiasCurrentColorMask = (void*)value.BiasCurrentColorMask;
            InDiffuseAlbedoSubrectBase = new(in value.DiffuseAlbedoSubrectBase);
            InSpecularAlbedoSubrectBase = new(in value.SpecularAlbedoSubrectBase);
            InNormalsSubrectBase = new(in value.NormalsSubrectBase);
            InRoughnessSubrectBase = new(in value.RoughnessSubrectBase);
            InColorSubrectBase = new(in value.ColorSubrectBase);
            InDepthSubrectBase = new(in value.DepthSubrectBase);
            InMVSubrectBase = new(in value.MVSubrectBase);
            InTranslucencySubrectBase = new(in value.TranslucencySubrectBase);
            InBiasCurrentColorSubrectBase = new(in value.BiasCurrentColorSubrectBase);
            InOutputSubrectBase = new(in value.OutputSubrectBase);
            PInReflectedAlbedo = (void*)value.ReflectedAlbedo;
            PInColorBeforeParticles = (void*)value.ColorBeforeParticles;
            PInColorBeforeTransparency = (void*)value.ColorBeforeTransparency;
            PInColorBeforeFog = (void*)value.ColorBeforeFog;
            PInDiffuseHitDistance = (void*)value.DiffuseHitDistance;
            PInSpecularHitDistance = (void*)value.SpecularHitDistance;
            PInDiffuseRayDirection = (void*)value.DiffuseRayDirection;
            PInSpecularRayDirection = (void*)value.SpecularRayDirection;
            PInDiffuseRayDirectionHitDistance = (void*)value.DiffuseRayDirectionHitDistance;
            PInSpecularRayDirectionHitDistance = (void*)value.SpecularRayDirectionHitDistance;
            InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);
            InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);
            InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);
            InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);
            InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);
            InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);
            InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);
            InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);
            InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);
            InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);
            PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? NGXMarshal.AllocValue(value.WorldToViewMatrix.Value) : null;
            PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? NGXMarshal.AllocValue(value.ViewToClipMatrix.Value) : null;
            InPreExposure = value.PreExposure;
            InExposureScale = value.ExposureScale;
            InIndicatorInvertXAxis = value.IndicatorInvertXAxis;
            InIndicatorInvertYAxis = value.IndicatorInvertYAxis;
            GBufferSurface = new(in value.GBufferSurface);
            InToneMapperType = value.ToneMapperType;
            PInMotionVectors3D = (void*)value.MotionVectors3D;
            PInIsParticleMask = (void*)value.IsParticleMask;
            PInAnimatedTextureMask = (void*)value.AnimatedTextureMask;
            PInDepthHighRes = (void*)value.DepthHighResolution;
            PInPositionViewSpace = (void*)value.PositionViewSpace;
            InFrameTimeDeltaInMsec = value.FrameTimeDeltaInMsec;
            PInRayTracingHitDistance = (void*)value.RayTracingHitDistance;
            PInMotionVectorsReflections = (void*)value.MotionVectorsReflections;
            PInTransparencyLayer = (void*)value.TransparencyLayer;
            InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);
            PInTransparencyLayerOpacity = (void*)value.TransparencyLayerOpacity;
            InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        InTransparencyLayerOpacitySubrectBase.Dispose();
        InTransparencyLayerSubrectBase.Dispose();
        GBufferSurface.Dispose();
        NGXMarshal.Free(PInViewToClipMatrix);
        NGXMarshal.Free(PInWorldToViewMatrix);
        InSpecularRayDirectionHitDistanceSubrectBase.Dispose();
        InDiffuseRayDirectionHitDistanceSubrectBase.Dispose();
        InSpecularRayDirectionSubrectBase.Dispose();
        InDiffuseRayDirectionSubrectBase.Dispose();
        InSpecularHitDistanceSubrectBase.Dispose();
        InDiffuseHitDistanceSubrectBase.Dispose();
        InColorBeforeFogSubrectBase.Dispose();
        InColorBeforeTransparencySubrectBase.Dispose();
        InColorBeforeParticlesSubrectBase.Dispose();
        InReflectedAlbedoSubrectBase.Dispose();
        InOutputSubrectBase.Dispose();
        InBiasCurrentColorSubrectBase.Dispose();
        InTranslucencySubrectBase.Dispose();
        InMVSubrectBase.Dispose();
        InDepthSubrectBase.Dispose();
        InColorSubrectBase.Dispose();
        InRoughnessSubrectBase.Dispose();
        InNormalsSubrectBase.Dispose();
        InSpecularAlbedoSubrectBase.Dispose();
        InDiffuseAlbedoSubrectBase.Dispose();
        InRenderSubrectDimensions.Dispose();
        this = default;
    }
}
