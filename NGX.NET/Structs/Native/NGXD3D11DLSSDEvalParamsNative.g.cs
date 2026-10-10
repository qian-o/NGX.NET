#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 904)]
internal unsafe struct NGXD3D11DLSSDEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public nint PInDiffuseAlbedo;

    [FieldOffset(8)]
    public nint PInSpecularAlbedo;

    [FieldOffset(16)]
    public nint PInNormals;

    [FieldOffset(24)]
    public nint PInRoughness;

    [FieldOffset(32)]
    public nint PInColor;

    [FieldOffset(40)]
    public nint PInAlpha;

    [FieldOffset(48)]
    public nint PInOutput;

    [FieldOffset(56)]
    public nint PInOutputAlpha;

    [FieldOffset(64)]
    public nint PInDepth;

    [FieldOffset(72)]
    public nint PInMotionVectors;

    [FieldOffset(80)]
    public float InJitterOffsetX;

    [FieldOffset(84)]
    public float InJitterOffsetY;

    [FieldOffset(88)]
    public NGXDimensionsNative InRenderSubrectDimensions;

    [FieldOffset(96)]
    public int InReset;

    [FieldOffset(100)]
    public float InMVScaleX;

    [FieldOffset(104)]
    public float InMVScaleY;

    [FieldOffset(112)]
    public nint PInTransparencyMask;

    [FieldOffset(120)]
    public nint PInExposureTexture;

    [FieldOffset(128)]
    public nint PInBiasCurrentColorMask;

    [FieldOffset(136)]
    public NGXCoordinatesNative InAlphaSubrectBase;

    [FieldOffset(144)]
    public NGXCoordinatesNative InOutputAlphaSubrectBase;

    [FieldOffset(152)]
    public NGXCoordinatesNative InDiffuseAlbedoSubrectBase;

    [FieldOffset(160)]
    public NGXCoordinatesNative InSpecularAlbedoSubrectBase;

    [FieldOffset(168)]
    public NGXCoordinatesNative InNormalsSubrectBase;

    [FieldOffset(176)]
    public NGXCoordinatesNative InRoughnessSubrectBase;

    [FieldOffset(184)]
    public NGXCoordinatesNative InColorSubrectBase;

    [FieldOffset(192)]
    public NGXCoordinatesNative InDepthSubrectBase;

    [FieldOffset(200)]
    public NGXCoordinatesNative InMVSubrectBase;

    [FieldOffset(208)]
    public NGXCoordinatesNative InTranslucencySubrectBase;

    [FieldOffset(216)]
    public NGXCoordinatesNative InBiasCurrentColorSubrectBase;

    [FieldOffset(224)]
    public NGXCoordinatesNative InOutputSubrectBase;

    [FieldOffset(232)]
    public nint PInReflectedAlbedo;

    [FieldOffset(240)]
    public nint PInColorBeforeParticles;

    [FieldOffset(248)]
    public nint PInColorAfterParticles;

    [FieldOffset(256)]
    public nint PInColorBeforeTransparency;

    [FieldOffset(264)]
    public nint PInColorAfterTransparency;

    [FieldOffset(272)]
    public nint PInColorBeforeFog;

    [FieldOffset(280)]
    public nint PInColorAfterFog;

    [FieldOffset(288)]
    public nint PInScreenSpaceSubsurfaceScatteringGuide;

    [FieldOffset(296)]
    public nint PInColorBeforeScreenSpaceSubsurfaceScattering;

    [FieldOffset(304)]
    public nint PInColorAfterScreenSpaceSubsurfaceScattering;

    [FieldOffset(312)]
    public nint PInScreenSpaceRefractionGuide;

    [FieldOffset(320)]
    public nint PInColorBeforeScreenSpaceRefraction;

    [FieldOffset(328)]
    public nint PInColorAfterScreenSpaceRefraction;

    [FieldOffset(336)]
    public nint PInDepthOfFieldGuide;

    [FieldOffset(344)]
    public nint PInColorBeforeDepthOfField;

    [FieldOffset(352)]
    public nint PInColorAfterDepthOfField;

    [FieldOffset(360)]
    public nint PInDiffuseHitDistance;

    [FieldOffset(368)]
    public nint PInSpecularHitDistance;

    [FieldOffset(376)]
    public nint PInDiffuseRayDirection;

    [FieldOffset(384)]
    public nint PInSpecularRayDirection;

    [FieldOffset(392)]
    public nint PInDiffuseRayDirectionHitDistance;

    [FieldOffset(400)]
    public nint PInSpecularRayDirectionHitDistance;

    [FieldOffset(408)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase;

    [FieldOffset(416)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase;

    [FieldOffset(424)]
    public NGXCoordinatesNative InColorAfterParticlesSubrectBase;

    [FieldOffset(432)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase;

    [FieldOffset(440)]
    public NGXCoordinatesNative InColorAfterTransparencySubrectBase;

    [FieldOffset(448)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase;

    [FieldOffset(456)]
    public NGXCoordinatesNative InColorAfterFogSubrectBase;

    [FieldOffset(464)]
    public NGXCoordinatesNative InScreenSpaceSubsurfaceScatteringGuideSubrectBase;

    [FieldOffset(472)]
    public NGXCoordinatesNative InScreenSpaceRefractionGuideSubrectBase;

    [FieldOffset(480)]
    public NGXCoordinatesNative InDepthOfFieldGuideSubrectBase;

    [FieldOffset(488)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase;

    [FieldOffset(496)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase;

    [FieldOffset(504)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase;

    [FieldOffset(512)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase;

    [FieldOffset(520)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase;

    [FieldOffset(528)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase;

    [FieldOffset(536)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(544)]
    public NGXCoordinatesNative InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(552)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceRefractionSubrectBase;

    [FieldOffset(560)]
    public NGXCoordinatesNative InColorAfterScreenSpaceRefractionSubrectBase;

    [FieldOffset(568)]
    public NGXCoordinatesNative InColorBeforeDepthOfFieldSubrectBase;

    [FieldOffset(576)]
    public NGXCoordinatesNative InColorAfterDepthOfFieldSubtectBase;

    [FieldOffset(584)]
    public Matrix4x4* PInWorldToViewMatrix;

    [FieldOffset(592)]
    public Matrix4x4* PInViewToClipMatrix;

    [FieldOffset(600)]
    public float InPreExposure;

    [FieldOffset(604)]
    public float InExposureScale;

    [FieldOffset(608)]
    public int InIndicatorInvertXAxis;

    [FieldOffset(612)]
    public int InIndicatorInvertYAxis;

    [FieldOffset(616)]
    public NGXD3D11GBufferNative GBufferSurface;

    [FieldOffset(752)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(760)]
    public nint PInMotionVectors3D;

    [FieldOffset(768)]
    public nint PInIsParticleMask;

    [FieldOffset(776)]
    public nint PInAnimatedTextureMask;

    [FieldOffset(784)]
    public nint PInDepthHighRes;

    [FieldOffset(792)]
    public nint PInPositionViewSpace;

    [FieldOffset(800)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(808)]
    public nint PInRayTracingHitDistance;

    [FieldOffset(816)]
    public nint PInMotionVectorsReflections;

    [FieldOffset(824)]
    public nint PInTransparencyLayer;

    [FieldOffset(832)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase;

    [FieldOffset(840)]
    public nint PInTransparencyLayerOpacity;

    [FieldOffset(848)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase;

    [FieldOffset(856)]
    public nint PInTransparencyLayerMvecs;

    [FieldOffset(864)]
    public NGXCoordinatesNative InTransparencyLayerMvecsSubrectBase;

    [FieldOffset(872)]
    public nint PInDisocclusionMask;

    [FieldOffset(880)]
    public NGXCoordinatesNative InDisocclusionMaskSubrectBase;

    [FieldOffset(888)]
    public nint PInResponsivityMask;

    [FieldOffset(896)]
    public NGXCoordinatesNative InResponsivityMaskSubrectBase;

    public NGXD3D11DLSSDEvalParamsNative(in NGXD3D11DLSSDEvalParams value)
    {
        try
        {
            PInDiffuseAlbedo = value.DiffuseAlbedo;
            PInSpecularAlbedo = value.SpecularAlbedo;
            PInNormals = value.Normals;
            PInRoughness = value.Roughness;
            PInColor = value.Color;
            PInAlpha = value.Alpha;
            PInOutput = value.Output;
            PInOutputAlpha = value.OutputAlpha;
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
            InAlphaSubrectBase = new(in value.AlphaSubrectBase);
            InOutputAlphaSubrectBase = new(in value.OutputAlphaSubrectBase);
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
            PInReflectedAlbedo = value.ReflectedAlbedo;
            PInColorBeforeParticles = value.ColorBeforeParticles;
            PInColorAfterParticles = value.ColorAfterParticles;
            PInColorBeforeTransparency = value.ColorBeforeTransparency;
            PInColorAfterTransparency = value.ColorAfterTransparency;
            PInColorBeforeFog = value.ColorBeforeFog;
            PInColorAfterFog = value.ColorAfterFog;
            PInScreenSpaceSubsurfaceScatteringGuide = value.ScreenSpaceSubsurfaceScatteringGuide;
            PInColorBeforeScreenSpaceSubsurfaceScattering = value.ColorBeforeScreenSpaceSubsurfaceScattering;
            PInColorAfterScreenSpaceSubsurfaceScattering = value.ColorAfterScreenSpaceSubsurfaceScattering;
            PInScreenSpaceRefractionGuide = value.ScreenSpaceRefractionGuide;
            PInColorBeforeScreenSpaceRefraction = value.ColorBeforeScreenSpaceRefraction;
            PInColorAfterScreenSpaceRefraction = value.ColorAfterScreenSpaceRefraction;
            PInDepthOfFieldGuide = value.DepthOfFieldGuide;
            PInColorBeforeDepthOfField = value.ColorBeforeDepthOfField;
            PInColorAfterDepthOfField = value.ColorAfterDepthOfField;
            PInDiffuseHitDistance = value.DiffuseHitDistance;
            PInSpecularHitDistance = value.SpecularHitDistance;
            PInDiffuseRayDirection = value.DiffuseRayDirection;
            PInSpecularRayDirection = value.SpecularRayDirection;
            PInDiffuseRayDirectionHitDistance = value.DiffuseRayDirectionHitDistance;
            PInSpecularRayDirectionHitDistance = value.SpecularRayDirectionHitDistance;
            InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);
            InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);
            InColorAfterParticlesSubrectBase = new(in value.ColorAfterParticlesSubrectBase);
            InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);
            InColorAfterTransparencySubrectBase = new(in value.ColorAfterTransparencySubrectBase);
            InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);
            InColorAfterFogSubrectBase = new(in value.ColorAfterFogSubrectBase);
            InScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in value.ScreenSpaceSubsurfaceScatteringGuideSubrectBase);
            InScreenSpaceRefractionGuideSubrectBase = new(in value.ScreenSpaceRefractionGuideSubrectBase);
            InDepthOfFieldGuideSubrectBase = new(in value.DepthOfFieldGuideSubrectBase);
            InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);
            InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);
            InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);
            InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);
            InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);
            InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);
            InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);
            InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);
            InColorBeforeScreenSpaceRefractionSubrectBase = new(in value.ColorBeforeScreenSpaceRefractionSubrectBase);
            InColorAfterScreenSpaceRefractionSubrectBase = new(in value.ColorAfterScreenSpaceRefractionSubrectBase);
            InColorBeforeDepthOfFieldSubrectBase = new(in value.ColorBeforeDepthOfFieldSubrectBase);
            InColorAfterDepthOfFieldSubtectBase = new(in value.ColorAfterDepthOfFieldSubtectBase);
            PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? NGXMarshal.AllocValue(value.WorldToViewMatrix.Value) : null;
            PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? NGXMarshal.AllocValue(value.ViewToClipMatrix.Value) : null;
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
            PInTransparencyLayer = value.TransparencyLayer;
            InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);
            PInTransparencyLayerOpacity = value.TransparencyLayerOpacity;
            InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);
            PInTransparencyLayerMvecs = value.TransparencyLayerMvecs;
            InTransparencyLayerMvecsSubrectBase = new(in value.TransparencyLayerMvecsSubrectBase);
            PInDisocclusionMask = value.DisocclusionMask;
            InDisocclusionMaskSubrectBase = new(in value.DisocclusionMaskSubrectBase);
            PInResponsivityMask = value.ResponsivityMask;
            InResponsivityMaskSubrectBase = new(in value.ResponsivityMaskSubrectBase);
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        InResponsivityMaskSubrectBase.Dispose();
        InDisocclusionMaskSubrectBase.Dispose();
        InTransparencyLayerMvecsSubrectBase.Dispose();
        InTransparencyLayerOpacitySubrectBase.Dispose();
        InTransparencyLayerSubrectBase.Dispose();
        GBufferSurface.Dispose();
        NGXMarshal.Free(PInViewToClipMatrix);
        NGXMarshal.Free(PInWorldToViewMatrix);
        InColorAfterDepthOfFieldSubtectBase.Dispose();
        InColorBeforeDepthOfFieldSubrectBase.Dispose();
        InColorAfterScreenSpaceRefractionSubrectBase.Dispose();
        InColorBeforeScreenSpaceRefractionSubrectBase.Dispose();
        InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase.Dispose();
        InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase.Dispose();
        InSpecularRayDirectionHitDistanceSubrectBase.Dispose();
        InDiffuseRayDirectionHitDistanceSubrectBase.Dispose();
        InSpecularRayDirectionSubrectBase.Dispose();
        InDiffuseRayDirectionSubrectBase.Dispose();
        InSpecularHitDistanceSubrectBase.Dispose();
        InDiffuseHitDistanceSubrectBase.Dispose();
        InDepthOfFieldGuideSubrectBase.Dispose();
        InScreenSpaceRefractionGuideSubrectBase.Dispose();
        InScreenSpaceSubsurfaceScatteringGuideSubrectBase.Dispose();
        InColorAfterFogSubrectBase.Dispose();
        InColorBeforeFogSubrectBase.Dispose();
        InColorAfterTransparencySubrectBase.Dispose();
        InColorBeforeTransparencySubrectBase.Dispose();
        InColorAfterParticlesSubrectBase.Dispose();
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
        InOutputAlphaSubrectBase.Dispose();
        InAlphaSubrectBase.Dispose();
        InRenderSubrectDimensions.Dispose();
        this = default;
    }
}
