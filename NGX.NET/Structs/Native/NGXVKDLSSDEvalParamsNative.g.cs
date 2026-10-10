#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 904)]
internal unsafe struct NGXVKDLSSDEvalParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXResourceVKNative* PInDiffuseAlbedo;

    [FieldOffset(8)]
    public NGXResourceVKNative* PInSpecularAlbedo;

    [FieldOffset(16)]
    public NGXResourceVKNative* PInNormals;

    [FieldOffset(24)]
    public NGXResourceVKNative* PInRoughness;

    [FieldOffset(32)]
    public NGXResourceVKNative* PInColor;

    [FieldOffset(40)]
    public NGXResourceVKNative* PInAlpha;

    [FieldOffset(48)]
    public NGXResourceVKNative* PInOutput;

    [FieldOffset(56)]
    public NGXResourceVKNative* PInOutputAlpha;

    [FieldOffset(64)]
    public NGXResourceVKNative* PInDepth;

    [FieldOffset(72)]
    public NGXResourceVKNative* PInMotionVectors;

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
    public NGXResourceVKNative* PInTransparencyMask;

    [FieldOffset(120)]
    public NGXResourceVKNative* PInExposureTexture;

    [FieldOffset(128)]
    public NGXResourceVKNative* PInBiasCurrentColorMask;

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
    public float InPreExposure;

    [FieldOffset(236)]
    public float InExposureScale;

    [FieldOffset(240)]
    public int InIndicatorInvertXAxis;

    [FieldOffset(244)]
    public int InIndicatorInvertYAxis;

    [FieldOffset(248)]
    public NGXResourceVKNative* PInReflectedAlbedo;

    [FieldOffset(256)]
    public NGXResourceVKNative* PInColorBeforeParticles;

    [FieldOffset(264)]
    public NGXResourceVKNative* PInColorAfterParticles;

    [FieldOffset(272)]
    public NGXResourceVKNative* PInColorBeforeTransparency;

    [FieldOffset(280)]
    public NGXResourceVKNative* PInColorAfterTransparency;

    [FieldOffset(288)]
    public NGXResourceVKNative* PInColorBeforeFog;

    [FieldOffset(296)]
    public NGXResourceVKNative* PInColorAfterFog;

    [FieldOffset(304)]
    public NGXResourceVKNative* PInScreenSpaceSubsurfaceScatteringGuide;

    [FieldOffset(312)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceSubsurfaceScattering;

    [FieldOffset(320)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceSubsurfaceScattering;

    [FieldOffset(328)]
    public NGXResourceVKNative* PInScreenSpaceRefractionGuide;

    [FieldOffset(336)]
    public NGXResourceVKNative* PInColorBeforeScreenSpaceRefraction;

    [FieldOffset(344)]
    public NGXResourceVKNative* PInColorAfterScreenSpaceRefraction;

    [FieldOffset(352)]
    public NGXResourceVKNative* PInDepthOfFieldGuide;

    [FieldOffset(360)]
    public NGXResourceVKNative* PInColorBeforeDepthOfField;

    [FieldOffset(368)]
    public NGXResourceVKNative* PInColorAfterDepthOfField;

    [FieldOffset(376)]
    public NGXResourceVKNative* PInDiffuseHitDistance;

    [FieldOffset(384)]
    public NGXResourceVKNative* PInSpecularHitDistance;

    [FieldOffset(392)]
    public NGXResourceVKNative* PInDiffuseRayDirection;

    [FieldOffset(400)]
    public NGXResourceVKNative* PInSpecularRayDirection;

    [FieldOffset(408)]
    public NGXResourceVKNative* PInDiffuseRayDirectionHitDistance;

    [FieldOffset(416)]
    public NGXResourceVKNative* PInSpecularRayDirectionHitDistance;

    [FieldOffset(424)]
    public NGXCoordinatesNative InReflectedAlbedoSubrectBase;

    [FieldOffset(432)]
    public NGXCoordinatesNative InColorBeforeParticlesSubrectBase;

    [FieldOffset(440)]
    public NGXCoordinatesNative InColorAfterParticlesSubrectBase;

    [FieldOffset(448)]
    public NGXCoordinatesNative InColorBeforeTransparencySubrectBase;

    [FieldOffset(456)]
    public NGXCoordinatesNative InColorAfterTransparencySubrectBase;

    [FieldOffset(464)]
    public NGXCoordinatesNative InColorBeforeFogSubrectBase;

    [FieldOffset(472)]
    public NGXCoordinatesNative InColorAfterFogSubrectBase;

    [FieldOffset(480)]
    public NGXCoordinatesNative InScreenSpaceSubsurfaceScatteringGuideSubrectBase;

    [FieldOffset(488)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(496)]
    public NGXCoordinatesNative InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase;

    [FieldOffset(504)]
    public NGXCoordinatesNative InScreenSpaceRefractionGuideSubrectBase;

    [FieldOffset(512)]
    public NGXCoordinatesNative InColorBeforeScreenSpaceRefractionSubrectBase;

    [FieldOffset(520)]
    public NGXCoordinatesNative InColorAfterScreenSpaceRefractionSubrectBase;

    [FieldOffset(528)]
    public NGXCoordinatesNative InDepthOfFieldGuideSubrectBase;

    [FieldOffset(536)]
    public NGXCoordinatesNative InColorBeforeDepthOfFieldSubrectBase;

    [FieldOffset(544)]
    public NGXCoordinatesNative InColorAfterDepthOfFieldSubrectBase;

    [FieldOffset(552)]
    public NGXCoordinatesNative InDiffuseHitDistanceSubrectBase;

    [FieldOffset(560)]
    public NGXCoordinatesNative InSpecularHitDistanceSubrectBase;

    [FieldOffset(568)]
    public NGXCoordinatesNative InDiffuseRayDirectionSubrectBase;

    [FieldOffset(576)]
    public NGXCoordinatesNative InSpecularRayDirectionSubrectBase;

    [FieldOffset(584)]
    public NGXCoordinatesNative InDiffuseRayDirectionHitDistanceSubrectBase;

    [FieldOffset(592)]
    public NGXCoordinatesNative InSpecularRayDirectionHitDistanceSubrectBase;

    [FieldOffset(600)]
    public Matrix4x4* PInWorldToViewMatrix;

    [FieldOffset(608)]
    public Matrix4x4* PInViewToClipMatrix;

    [FieldOffset(616)]
    public NGXVKGBufferNative GBufferSurface;

    [FieldOffset(752)]
    public NGXToneMapperType InToneMapperType;

    [FieldOffset(760)]
    public NGXResourceVKNative* PInMotionVectors3D;

    [FieldOffset(768)]
    public NGXResourceVKNative* PInIsParticleMask;

    [FieldOffset(776)]
    public NGXResourceVKNative* PInAnimatedTextureMask;

    [FieldOffset(784)]
    public NGXResourceVKNative* PInDepthHighRes;

    [FieldOffset(792)]
    public NGXResourceVKNative* PInPositionViewSpace;

    [FieldOffset(800)]
    public float InFrameTimeDeltaInMsec;

    [FieldOffset(808)]
    public NGXResourceVKNative* PInRayTracingHitDistance;

    [FieldOffset(816)]
    public NGXResourceVKNative* PInMotionVectorsReflections;

    [FieldOffset(824)]
    public NGXResourceVKNative* PInTransparencyLayer;

    [FieldOffset(832)]
    public NGXCoordinatesNative InTransparencyLayerSubrectBase;

    [FieldOffset(840)]
    public NGXResourceVKNative* PInTransparencyLayerOpacity;

    [FieldOffset(848)]
    public NGXCoordinatesNative InTransparencyLayerOpacitySubrectBase;

    [FieldOffset(856)]
    public NGXResourceVKNative* PInTransparencyLayerMvecs;

    [FieldOffset(864)]
    public NGXCoordinatesNative InTransparencyLayerMvecsSubrectBase;

    [FieldOffset(872)]
    public NGXResourceVKNative* PInDisocclusionMask;

    [FieldOffset(880)]
    public NGXCoordinatesNative InDisocclusionMaskSubrectBase;

    [FieldOffset(888)]
    public NGXResourceVKNative* PInResponsivityMask;

    [FieldOffset(896)]
    public NGXCoordinatesNative InResponsivityMaskSubrectBase;

    public NGXVKDLSSDEvalParamsNative(in NGXVKDLSSDEvalParams value)
    {
        this = default;

        try
        {
            if (value.DiffuseAlbedo is NGXResourceVK diffuseAlbedo)
            {
                PInDiffuseAlbedo = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in diffuseAlbedo));
            }

            if (value.SpecularAlbedo is NGXResourceVK specularAlbedo)
            {
                PInSpecularAlbedo = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in specularAlbedo));
            }

            if (value.Normals is NGXResourceVK normals)
            {
                PInNormals = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in normals));
            }

            if (value.Roughness is NGXResourceVK roughness)
            {
                PInRoughness = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in roughness));
            }

            if (value.Color is NGXResourceVK color)
            {
                PInColor = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in color));
            }

            if (value.Alpha is NGXResourceVK alpha)
            {
                PInAlpha = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in alpha));
            }

            if (value.Output is NGXResourceVK output)
            {
                PInOutput = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in output));
            }

            if (value.OutputAlpha is NGXResourceVK outputAlpha)
            {
                PInOutputAlpha = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in outputAlpha));
            }

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
            InPreExposure = value.PreExposure;
            InExposureScale = value.ExposureScale;
            InIndicatorInvertXAxis = value.IndicatorInvertXAxis;
            InIndicatorInvertYAxis = value.IndicatorInvertYAxis;

            if (value.ReflectedAlbedo is NGXResourceVK reflectedAlbedo)
            {
                PInReflectedAlbedo = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in reflectedAlbedo));
            }

            if (value.ColorBeforeParticles is NGXResourceVK colorBeforeParticles)
            {
                PInColorBeforeParticles = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeParticles));
            }

            if (value.ColorAfterParticles is NGXResourceVK colorAfterParticles)
            {
                PInColorAfterParticles = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterParticles));
            }

            if (value.ColorBeforeTransparency is NGXResourceVK colorBeforeTransparency)
            {
                PInColorBeforeTransparency = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeTransparency));
            }

            if (value.ColorAfterTransparency is NGXResourceVK colorAfterTransparency)
            {
                PInColorAfterTransparency = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterTransparency));
            }

            if (value.ColorBeforeFog is NGXResourceVK colorBeforeFog)
            {
                PInColorBeforeFog = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeFog));
            }

            if (value.ColorAfterFog is NGXResourceVK colorAfterFog)
            {
                PInColorAfterFog = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterFog));
            }

            if (value.ScreenSpaceSubsurfaceScatteringGuide is NGXResourceVK screenSpaceSubsurfaceScatteringGuide)
            {
                PInScreenSpaceSubsurfaceScatteringGuide = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in screenSpaceSubsurfaceScatteringGuide));
            }

            if (value.ColorBeforeScreenSpaceSubsurfaceScattering is NGXResourceVK colorBeforeScreenSpaceSubsurfaceScattering)
            {
                PInColorBeforeScreenSpaceSubsurfaceScattering = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeScreenSpaceSubsurfaceScattering));
            }

            if (value.ColorAfterScreenSpaceSubsurfaceScattering is NGXResourceVK colorAfterScreenSpaceSubsurfaceScattering)
            {
                PInColorAfterScreenSpaceSubsurfaceScattering = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterScreenSpaceSubsurfaceScattering));
            }

            if (value.ScreenSpaceRefractionGuide is NGXResourceVK screenSpaceRefractionGuide)
            {
                PInScreenSpaceRefractionGuide = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in screenSpaceRefractionGuide));
            }

            if (value.ColorBeforeScreenSpaceRefraction is NGXResourceVK colorBeforeScreenSpaceRefraction)
            {
                PInColorBeforeScreenSpaceRefraction = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeScreenSpaceRefraction));
            }

            if (value.ColorAfterScreenSpaceRefraction is NGXResourceVK colorAfterScreenSpaceRefraction)
            {
                PInColorAfterScreenSpaceRefraction = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterScreenSpaceRefraction));
            }

            if (value.DepthOfFieldGuide is NGXResourceVK depthOfFieldGuide)
            {
                PInDepthOfFieldGuide = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in depthOfFieldGuide));
            }

            if (value.ColorBeforeDepthOfField is NGXResourceVK colorBeforeDepthOfField)
            {
                PInColorBeforeDepthOfField = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorBeforeDepthOfField));
            }

            if (value.ColorAfterDepthOfField is NGXResourceVK colorAfterDepthOfField)
            {
                PInColorAfterDepthOfField = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in colorAfterDepthOfField));
            }

            if (value.DiffuseHitDistance is NGXResourceVK diffuseHitDistance)
            {
                PInDiffuseHitDistance = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in diffuseHitDistance));
            }

            if (value.SpecularHitDistance is NGXResourceVK specularHitDistance)
            {
                PInSpecularHitDistance = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in specularHitDistance));
            }

            if (value.DiffuseRayDirection is NGXResourceVK diffuseRayDirection)
            {
                PInDiffuseRayDirection = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in diffuseRayDirection));
            }

            if (value.SpecularRayDirection is NGXResourceVK specularRayDirection)
            {
                PInSpecularRayDirection = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in specularRayDirection));
            }

            if (value.DiffuseRayDirectionHitDistance is NGXResourceVK diffuseRayDirectionHitDistance)
            {
                PInDiffuseRayDirectionHitDistance = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in diffuseRayDirectionHitDistance));
            }

            if (value.SpecularRayDirectionHitDistance is NGXResourceVK specularRayDirectionHitDistance)
            {
                PInSpecularRayDirectionHitDistance = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in specularRayDirectionHitDistance));
            }

            InReflectedAlbedoSubrectBase = new(in value.ReflectedAlbedoSubrectBase);
            InColorBeforeParticlesSubrectBase = new(in value.ColorBeforeParticlesSubrectBase);
            InColorAfterParticlesSubrectBase = new(in value.ColorAfterParticlesSubrectBase);
            InColorBeforeTransparencySubrectBase = new(in value.ColorBeforeTransparencySubrectBase);
            InColorAfterTransparencySubrectBase = new(in value.ColorAfterTransparencySubrectBase);
            InColorBeforeFogSubrectBase = new(in value.ColorBeforeFogSubrectBase);
            InColorAfterFogSubrectBase = new(in value.ColorAfterFogSubrectBase);
            InScreenSpaceSubsurfaceScatteringGuideSubrectBase = new(in value.ScreenSpaceSubsurfaceScatteringGuideSubrectBase);
            InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase);
            InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase = new(in value.ColorAfterScreenSpaceSubsurfaceScatteringSubrectBase);
            InScreenSpaceRefractionGuideSubrectBase = new(in value.ScreenSpaceRefractionGuideSubrectBase);
            InColorBeforeScreenSpaceRefractionSubrectBase = new(in value.ColorBeforeScreenSpaceRefractionSubrectBase);
            InColorAfterScreenSpaceRefractionSubrectBase = new(in value.ColorAfterScreenSpaceRefractionSubrectBase);
            InDepthOfFieldGuideSubrectBase = new(in value.DepthOfFieldGuideSubrectBase);
            InColorBeforeDepthOfFieldSubrectBase = new(in value.ColorBeforeDepthOfFieldSubrectBase);
            InColorAfterDepthOfFieldSubrectBase = new(in value.ColorAfterDepthOfFieldSubrectBase);
            InDiffuseHitDistanceSubrectBase = new(in value.DiffuseHitDistanceSubrectBase);
            InSpecularHitDistanceSubrectBase = new(in value.SpecularHitDistanceSubrectBase);
            InDiffuseRayDirectionSubrectBase = new(in value.DiffuseRayDirectionSubrectBase);
            InSpecularRayDirectionSubrectBase = new(in value.SpecularRayDirectionSubrectBase);
            InDiffuseRayDirectionHitDistanceSubrectBase = new(in value.DiffuseRayDirectionHitDistanceSubrectBase);
            InSpecularRayDirectionHitDistanceSubrectBase = new(in value.SpecularRayDirectionHitDistanceSubrectBase);
            PInWorldToViewMatrix = value.WorldToViewMatrix.HasValue ? NGXMarshal.AllocValue(value.WorldToViewMatrix.Value) : null;
            PInViewToClipMatrix = value.ViewToClipMatrix.HasValue ? NGXMarshal.AllocValue(value.ViewToClipMatrix.Value) : null;
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

            if (value.TransparencyLayer is NGXResourceVK transparencyLayer)
            {
                PInTransparencyLayer = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in transparencyLayer));
            }

            InTransparencyLayerSubrectBase = new(in value.TransparencyLayerSubrectBase);

            if (value.TransparencyLayerOpacity is NGXResourceVK transparencyLayerOpacity)
            {
                PInTransparencyLayerOpacity = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in transparencyLayerOpacity));
            }

            InTransparencyLayerOpacitySubrectBase = new(in value.TransparencyLayerOpacitySubrectBase);

            if (value.TransparencyLayerMvecs is NGXResourceVK transparencyLayerMvecs)
            {
                PInTransparencyLayerMvecs = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in transparencyLayerMvecs));
            }

            InTransparencyLayerMvecsSubrectBase = new(in value.TransparencyLayerMvecsSubrectBase);

            if (value.DisocclusionMask is NGXResourceVK disocclusionMask)
            {
                PInDisocclusionMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in disocclusionMask));
            }

            InDisocclusionMaskSubrectBase = new(in value.DisocclusionMaskSubrectBase);

            if (value.ResponsivityMask is NGXResourceVK responsivityMask)
            {
                PInResponsivityMask = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in responsivityMask));
            }

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
        NGXMarshal.FreeNative(PInResponsivityMask);
        InDisocclusionMaskSubrectBase.Dispose();
        NGXMarshal.FreeNative(PInDisocclusionMask);
        InTransparencyLayerMvecsSubrectBase.Dispose();
        NGXMarshal.FreeNative(PInTransparencyLayerMvecs);
        InTransparencyLayerOpacitySubrectBase.Dispose();
        NGXMarshal.FreeNative(PInTransparencyLayerOpacity);
        InTransparencyLayerSubrectBase.Dispose();
        NGXMarshal.FreeNative(PInTransparencyLayer);
        NGXMarshal.FreeNative(PInMotionVectorsReflections);
        NGXMarshal.FreeNative(PInRayTracingHitDistance);
        NGXMarshal.FreeNative(PInPositionViewSpace);
        NGXMarshal.FreeNative(PInDepthHighRes);
        NGXMarshal.FreeNative(PInAnimatedTextureMask);
        NGXMarshal.FreeNative(PInIsParticleMask);
        NGXMarshal.FreeNative(PInMotionVectors3D);
        GBufferSurface.Dispose();
        NGXMarshal.Free(PInViewToClipMatrix);
        NGXMarshal.Free(PInWorldToViewMatrix);
        InSpecularRayDirectionHitDistanceSubrectBase.Dispose();
        InDiffuseRayDirectionHitDistanceSubrectBase.Dispose();
        InSpecularRayDirectionSubrectBase.Dispose();
        InDiffuseRayDirectionSubrectBase.Dispose();
        InSpecularHitDistanceSubrectBase.Dispose();
        InDiffuseHitDistanceSubrectBase.Dispose();
        InColorAfterDepthOfFieldSubrectBase.Dispose();
        InColorBeforeDepthOfFieldSubrectBase.Dispose();
        InDepthOfFieldGuideSubrectBase.Dispose();
        InColorAfterScreenSpaceRefractionSubrectBase.Dispose();
        InColorBeforeScreenSpaceRefractionSubrectBase.Dispose();
        InScreenSpaceRefractionGuideSubrectBase.Dispose();
        InColorAfterScreenSpaceSubsurfaceScatteringSubrectBase.Dispose();
        InColorBeforeScreenSpaceSubsurfaceScatteringSubrectBase.Dispose();
        InScreenSpaceSubsurfaceScatteringGuideSubrectBase.Dispose();
        InColorAfterFogSubrectBase.Dispose();
        InColorBeforeFogSubrectBase.Dispose();
        InColorAfterTransparencySubrectBase.Dispose();
        InColorBeforeTransparencySubrectBase.Dispose();
        InColorAfterParticlesSubrectBase.Dispose();
        InColorBeforeParticlesSubrectBase.Dispose();
        InReflectedAlbedoSubrectBase.Dispose();
        NGXMarshal.FreeNative(PInSpecularRayDirectionHitDistance);
        NGXMarshal.FreeNative(PInDiffuseRayDirectionHitDistance);
        NGXMarshal.FreeNative(PInSpecularRayDirection);
        NGXMarshal.FreeNative(PInDiffuseRayDirection);
        NGXMarshal.FreeNative(PInSpecularHitDistance);
        NGXMarshal.FreeNative(PInDiffuseHitDistance);
        NGXMarshal.FreeNative(PInColorAfterDepthOfField);
        NGXMarshal.FreeNative(PInColorBeforeDepthOfField);
        NGXMarshal.FreeNative(PInDepthOfFieldGuide);
        NGXMarshal.FreeNative(PInColorAfterScreenSpaceRefraction);
        NGXMarshal.FreeNative(PInColorBeforeScreenSpaceRefraction);
        NGXMarshal.FreeNative(PInScreenSpaceRefractionGuide);
        NGXMarshal.FreeNative(PInColorAfterScreenSpaceSubsurfaceScattering);
        NGXMarshal.FreeNative(PInColorBeforeScreenSpaceSubsurfaceScattering);
        NGXMarshal.FreeNative(PInScreenSpaceSubsurfaceScatteringGuide);
        NGXMarshal.FreeNative(PInColorAfterFog);
        NGXMarshal.FreeNative(PInColorBeforeFog);
        NGXMarshal.FreeNative(PInColorAfterTransparency);
        NGXMarshal.FreeNative(PInColorBeforeTransparency);
        NGXMarshal.FreeNative(PInColorAfterParticles);
        NGXMarshal.FreeNative(PInColorBeforeParticles);
        NGXMarshal.FreeNative(PInReflectedAlbedo);
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
        NGXMarshal.FreeNative(PInBiasCurrentColorMask);
        NGXMarshal.FreeNative(PInExposureTexture);
        NGXMarshal.FreeNative(PInTransparencyMask);
        InRenderSubrectDimensions.Dispose();
        NGXMarshal.FreeNative(PInMotionVectors);
        NGXMarshal.FreeNative(PInDepth);
        NGXMarshal.FreeNative(PInOutputAlpha);
        NGXMarshal.FreeNative(PInOutput);
        NGXMarshal.FreeNative(PInAlpha);
        NGXMarshal.FreeNative(PInColor);
        NGXMarshal.FreeNative(PInRoughness);
        NGXMarshal.FreeNative(PInNormals);
        NGXMarshal.FreeNative(PInSpecularAlbedo);
        NGXMarshal.FreeNative(PInDiffuseAlbedo);
        this = default;
    }
}
