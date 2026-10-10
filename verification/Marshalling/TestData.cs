namespace Marshalling;

internal static unsafe class TestData
{
    internal static NGXFeatureDiscoveryInfo Discovery()
    {
        return new()
        {
            SDKVersion = NGXVersion.Api,
            FeatureID = NGXFeature.RayReconstruction,
            Identifier = new()
            {
                IdentifierType = NGXApplicationIdentifierType.ProjectId,
                V = new()
                {
                    ProjectDesc = new()
                    {
                        ProjectId = "project-\u6D4B\u8BD5",
                        EngineType = NGXEngineType.Custom,
                        EngineVersion = "1.0\U0001F680"
                    }
                }
            },
            ApplicationDataPath = "/tmp/\u4E2D\u6587\U0001F680",
            FeatureInfo = new()
            {
                PathListInfo = new()
                {
                    Paths = ["/runtime/\u5E93", "", new string('\u6587', 100_000)]
                }
            }
        };
    }

    internal static NGXResourceVK Resource(int index)
    {
        return new()
        {
            Type = NGXResourceVKType.VkImageView,
            ReadWrite = index is 5,
            Resource = new()
            {
                ImageViewInfo = new()
                {
                    Image = 0x1200 + index,
                    ImageView = 0x3400 + index,
                    Width = 128,
                    Height = 64,
                    Format = NGXVkFormat.R16G16B16A16Sfloat,
                    SubresourceRange = new()
                    {
                        AspectMask = 1,
                        BaseMipLevel = 3,
                        LevelCount = 1,
                        BaseArrayLayer = 2,
                        LayerCount = 1
                    }
                }
            }
        };
    }

    internal static NGXVKDLSSDEvalParams Frame()
    {
        return new()
        {
            DiffuseAlbedo = Resource(0),
            SpecularAlbedo = Resource(1),
            Normals = Resource(2),
            Roughness = Resource(3),
            Color = Resource(4),
            Output = Resource(5),
            Depth = Resource(6),
            MotionVectors = Resource(7),
            ExposureTexture = Resource(8),
            BiasCurrentColorMask = Resource(9),
            ColorBeforeTransparency = Resource(10),
            ScreenSpaceSubsurfaceScatteringGuide = Resource(11),
            DepthOfFieldGuide = Resource(12),
            SpecularHitDistance = Resource(13),
            MotionVectorsReflections = Resource(14),
            TransparencyLayer = Resource(15),
            TransparencyLayerOpacity = Resource(16),
            WorldToViewMatrix = Matrix4x4.CreateTranslation(3, 5, 7),
            ViewToClipMatrix = Matrix4x4.Identity,
            RenderSubrectDimensions = new()
            {
                Width = 128,
                Height = 64
            },
            Reset = 1
        };
    }

    internal static TrackingScope Storage(Action? destroyed = null)
    {
        TrackingScope scope = new(destroyed);
        NGXFeatureCommonInfo value = new()
        {
            PathListInfo = new()
            {
                Paths = ["/test"]
            },
            LoggingInfo = new()
            {
                LoggingCallback = static (_, _, _) =>
                {
                }
            }
        };
        scope.Alloc(new NGXFeatureCommonInfoNative(in value, scope));

        return scope;
    }
}
