using System.Numerics;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;

namespace Showcase;

internal class NGXSession : IDisposable
{
    private const string ProjectId = "fc6ac847-10b0-48e1-842d-1bc819f8d2f4";
    private const string EngineVersion = "NGX.NET.Showcase.1.0";

    private static readonly NGXFeature[] Features = [NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration];

    private readonly string dataPath;
    private readonly NGXFeatureCommonInfo common;

    private bool isVulkan;
    private nint device;
    private NGXParameter capabilities;
    private NGXParameter parameters;
    private NGXParameter frameParameters;
    private NGXHandle reconstruction;
    private NGXHandle generation;
    private RenderSettings settings = new();
    private int inputWidth;
    private int inputHeight;
    private int outputWidth;
    private int outputHeight;
    private bool initialized;

    public NGXSession()
    {
        dataPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(dataPath);
        common = new()
        {
            PathListInfo = new()
            {
                Paths = [Ngx.RuntimeDirectory]
            }
        };
    }

    public Dictionary<NGXFeature, string> Unavailable { get; } = [];

    public bool Available(NGXFeature feature)
    {
        return !Unavailable.ContainsKey(feature);
    }

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        isVulkan = instance is not 0;
        device = nativeDevice;
        NGXResult result = isVulkan ? Ngx.Vulkan.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, instance, physical, device, getInstanceProcAddr, getDeviceProcAddr, common, NGXVersion.Api) : Ngx.D3D12.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, device, common, NGXVersion.Api);
        if (result is NGXResult.FailFeatureNotSupported or NGXResult.FailPlatformError or NGXResult.FailOutOfDate)
        {
            foreach (NGXFeature feature in Features)
            {
                Unavailable[feature] = $"NGX initialization: {result}";
            }

            Console.WriteLine($"NGX features unavailable: {result}. Native rendering remains available.");

            return;
        }

        if (result is not NGXResult.Success)
        {
            throw new NGXException(result, nameof(Initialize));
        }

        initialized = true;
        capabilities = isVulkan ? Ngx.Vulkan.GetCapabilityParameters() : Ngx.D3D12.GetCapabilityParameters();
        parameters = isVulkan ? Ngx.Vulkan.AllocateParameters() : Ngx.D3D12.AllocateParameters();
        frameParameters = isVulkan ? Ngx.Vulkan.AllocateParameters() : Ngx.D3D12.AllocateParameters();

        Query(NGXFeature.SuperSampling, Ngx.ParameterSuperSamplingAvailable);
        Query(NGXFeature.RayReconstruction, Ngx.ParameterSuperSamplingDenoisingAvailable);
        Query(NGXFeature.FrameGeneration, Ngx.ParameterFrameGenerationAvailable);
    }

    public string[] VulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        foreach (NGXFeature feature in Features)
        {
            NGXFeatureDiscoveryInfo discovery = new()
            {
                SDKVersion = NGXVersion.Api,
                FeatureID = feature,
                Identifier = new()
                {
                    IdentifierType = NGXApplicationIdentifierType.ProjectId,
                    V = new()
                    {
                        ProjectDesc = new()
                        {
                            ProjectId = ProjectId,
                            EngineType = NGXEngineType.Custom,
                            EngineVersion = EngineVersion
                        }
                    }
                },
                ApplicationDataPath = dataPath,
                FeatureInfo = common
            };
            NGXVkExtensionProperties[] properties;
            try
            {
                properties = instance is 0 ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(discovery) : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, discovery);
            }
            catch (NGXException exception)
            {
                Unavailable[feature] = $"Extension requirements: {exception.Result}";

                continue;
            }

            foreach (NGXVkExtensionProperties property in properties)
            {
                extensions.Add(property.ExtensionName!);
            }
        }

        return [.. extensions];
    }

    // The caller completes submitted GPU work before reconfiguration or disposal.
    public void ReleaseReconstruction()
    {
        Release(ref reconstruction);
    }

    public void ReleaseFrameGeneration()
    {
        Release(ref generation);
    }

    public void Dispose()
    {
        if (initialized)
        {
            ReleaseReconstruction();
            ReleaseFrameGeneration();
            DestroyParameters(ref frameParameters);
            DestroyParameters(ref parameters);
            DestroyParameters(ref capabilities);
            NGXResult result = isVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, nameof(Ngx.Vulkan.Shutdown1));
            }

            initialized = false;
        }
    }

    public bool Generate(nint command, GpuImage[] images, GpuImage output, CameraHandler camera, bool reset)
    {
        if (generation.IsNull)
        {
            Ngx.Parameter.Reset(frameParameters);
            NativeImage color = images[(int)ImageSlot.Final].Describe();
            NGXDLSSGCreateParams create = new()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                RenderWidth = (uint)inputWidth,
                RenderHeight = (uint)inputHeight,
                NativeBackbufferFormat = isVulkan ? (uint)color.Vulkan.Resource.ImageViewInfo!.Value.Format : (uint)Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm
            };
            generation = isVulkan ? Ngx.Vulkan.CreateDLSSG(command, 1, 1, frameParameters, create) : Ngx.D3D12.CreateDLSSG(command, 1, 1, frameParameters, create);
            reset = true;
        }

        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 inverseViewProjection);
        Matrix4x4 clipToPrevious = inverseViewProjection * camera.PreviousViewProjection;
        Matrix4x4.Invert(clipToPrevious, out Matrix4x4 previousToClip);
        Vector3 forward = camera.Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        NGXDLSSGOptEvalParams options = new()
        {
            CameraViewToClip = camera.Projection,
            ClipToCameraView = inverseProjection,
            ClipToLensClip = Matrix4x4.Identity,
            ClipToPrevClip = clipToPrevious,
            PrevClipToClip = previousToClip,
            JitterOffset = camera.Jitter,
            MvecScale = Vector2.One,
            CameraPos = camera.Position,
            CameraUp = up,
            CameraRight = right,
            CameraFwd = forward,
            CameraNear = camera.Near,
            CameraFar = camera.Far,
            CameraFOV = CameraHandler.FieldOfView,
            CameraAspectRatio = (float)outputWidth / outputHeight,
            CameraMotionIncluded = true,
            DepthInverted = true,
            Reset = reset
        };

        NativeImage back = images[(int)ImageSlot.Final].Describe();
        NativeImage depth = images[(int)ImageSlot.Depth].Describe();
        NativeImage motion = images[(int)ImageSlot.Motion].Describe();
        NativeImage hudless = images[(int)ImageSlot.Hudless].Describe();
        NativeImage ui = images[(int)ImageSlot.UI].Describe();
        NativeImage generated = output.Describe();
        generated.Vulkan.ReadWrite = true;

        NGXResult result;
        if (isVulkan)
        {
            NGXVKDLSSGEvalParams evaluate = new()
            {
                Backbuffer = back.Vulkan,
                Depth = depth.Vulkan,
                MVecs = motion.Vulkan,
                Hudless = hudless.Vulkan,
                UI = ui.Vulkan,
                OutputInterpFrame = generated.Vulkan
            };

            result = Ngx.Vulkan.EvaluateDLSSG(command, generation, frameParameters, evaluate, options);
        }
        else
        {
            NGXD3D12DLSSGEvalParams evaluate = new()
            {
                Backbuffer = back.DirectX,
                Depth = depth.DirectX,
                MVecs = motion.DirectX,
                Hudless = hudless.DirectX,
                UI = ui.DirectX,
                OutputInterpFrame = generated.DirectX
            };

            result = Ngx.D3D12.EvaluateDLSSG(command, generation, frameParameters, evaluate, options);
        }

        if (result is not NGXResult.Success)
        {
            throw new NGXException(result, nameof(Generate));
        }

        // A reset produces a copy of the real frame; present that real frame once.
        return !reset;
    }

    public (int Width, int Height) Configure(RenderSettings value, int width, int height)
    {
        settings = value;
        outputWidth = width;
        outputHeight = height;
        uint renderWidth = (uint)width;
        uint renderHeight = (uint)height;
        if (value.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
        {
            OptimalSettings optimalSettings = value.Reconstruction is Reconstruction.DLSS ? Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality) : Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality);
            renderWidth = optimalSettings.RenderOptimalWidth;
            renderHeight = optimalSettings.RenderOptimalHeight;
        }

        inputWidth = (int)renderWidth;
        inputHeight = (int)renderHeight;

        return (inputWidth, inputHeight);
    }

    public void Evaluate(nint command, GpuImage[] images, CameraHandler camera, bool reset, float delta)
    {
        if (reconstruction.IsNull)
        {
            CreateReconstruction(command);
        }

        Span<NativeImage> descriptions = stackalloc NativeImage[images.Length];
        for (int i = 0; i < images.Length; i++)
        {
            descriptions[i] = images[i].Describe();
        }

        descriptions[(int)ImageSlot.Reconstructed].Vulkan.ReadWrite = true;
        EvaluateImages(command, descriptions, camera, reset, delta);
    }

    private void Query(NGXFeature feature, string name)
    {
        try
        {
            int available = Ngx.Parameter.GetI(capabilities, name);
            if (available is 0)
            {
                Unavailable[feature] = "Not supported by this device/driver";
            }
        }
        catch (NGXException exception)
        {
            Unavailable[feature] = exception.Result.ToString();
        }

        Console.WriteLine($"{feature}: {(Available(feature) ? "Available" : Unavailable[feature])}");
    }

    private void Release(ref NGXHandle handle)
    {
        if (!handle.IsNull)
        {
            NGXResult result = isVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, nameof(Ngx.Vulkan.ReleaseFeature));
            }

            handle = default;
        }
    }

    private void DestroyParameters(ref NGXParameter value)
    {
        if (!value.IsNull)
        {
            NGXResult result = isVulkan ? Ngx.Vulkan.DestroyParameters(value) : Ngx.D3D12.DestroyParameters(value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, nameof(Ngx.Vulkan.DestroyParameters));
            }

            value = default;
        }
    }

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        const int Flags = (int)(NGXDLSSFeatureFlags.IsHdr | NGXDLSSFeatureFlags.MvLowRes | NGXDLSSFeatureFlags.AutoExposure | NGXDLSSFeatureFlags.DepthInverted);
        if (settings.Reconstruction is Reconstruction.RayReconstruction)
        {
            NGXDLSSDCreateParams create = new()
            {
                DenoiseMode = NGXDLSSDenoiseMode.DlUnified,
                RoughnessMode = NGXDLSSRoughnessMode.Packed,
                UseHWDepth = NGXDLSSDepthType.Hw,
                Width = (uint)inputWidth,
                Height = (uint)inputHeight,
                TargetWidth = (uint)outputWidth,
                TargetHeight = (uint)outputHeight,
                PerfQualityValue = settings.ReconstructionQuality,
                FeatureCreateFlags = Flags
            };

            reconstruction = isVulkan ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1, 1, parameters, create) : Ngx.D3D12.CreateDLSSDExt(command, 1, 1, parameters, create);
        }
        else
        {
            NGXDLSSCreateParams create = new()
            {
                Feature = new()
                {
                    Width = (uint)inputWidth,
                    Height = (uint)inputHeight,
                    TargetWidth = (uint)outputWidth,
                    TargetHeight = (uint)outputHeight,
                    PerfQualityValue = settings.ReconstructionQuality
                },
                FeatureCreateFlags = Flags
            };

            reconstruction = isVulkan ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1, 1, parameters, create) : Ngx.D3D12.CreateDLSSExt(command, 1, 1, parameters, create);
        }
    }

    private void EvaluateImages(nint command, ReadOnlySpan<NativeImage> images, CameraHandler camera, bool reset, float delta)
    {
        Matrix4x4 view = camera.View;
        Matrix4x4 projection = camera.Projection;
        NGXDimensions dimensions = new()
        {
            Width = (uint)inputWidth,
            Height = (uint)inputHeight
        };
        NGXResult result;
        if (!isVulkan)
        {
            if (settings.Reconstruction is Reconstruction.RayReconstruction)
            {
                NGXD3D12DLSSDEvalParams evaluate = new()
                {
                    Color = images[(int)ImageSlot.Scene].DirectX,
                    Output = images[(int)ImageSlot.Reconstructed].DirectX,
                    DiffuseAlbedo = images[(int)ImageSlot.Diffuse].DirectX,
                    SpecularAlbedo = images[(int)ImageSlot.Specular].DirectX,
                    Normals = images[(int)ImageSlot.Normal].DirectX,
                    MotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].DirectX,
                    WorldToViewMatrix = view,
                    ViewToClipMatrix = projection,
                    Depth = images[(int)ImageSlot.Depth].DirectX,
                    MotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1,
                    MVScaleY = 1,
                    PreExposure = 1,
                    ExposureScale = 1,
                    FrameTimeDeltaInMsec = delta * 1000
                };

                result = Ngx.D3D12.EvaluateDLSSDExt(command, reconstruction, parameters, evaluate);
            }
            else
            {
                NGXD3D12DLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        Color = images[(int)ImageSlot.Scene].DirectX,
                        Output = images[(int)ImageSlot.Reconstructed].DirectX
                    },
                    Depth = images[(int)ImageSlot.Depth].DirectX,
                    MotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1,
                    MVScaleY = 1,
                    PreExposure = 1,
                    ExposureScale = 1,
                    FrameTimeDeltaInMsec = delta * 1000
                };

                result = Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, evaluate);
            }
        }
        else
        {
            if (settings.Reconstruction is Reconstruction.RayReconstruction)
            {
                NGXVKDLSSDEvalParams evaluate = new()
                {
                    Color = images[(int)ImageSlot.Scene].Vulkan,
                    Output = images[(int)ImageSlot.Reconstructed].Vulkan,
                    DiffuseAlbedo = images[(int)ImageSlot.Diffuse].Vulkan,
                    SpecularAlbedo = images[(int)ImageSlot.Specular].Vulkan,
                    Normals = images[(int)ImageSlot.Normal].Vulkan,
                    MotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].Vulkan,
                    WorldToViewMatrix = view,
                    ViewToClipMatrix = projection,
                    Depth = images[(int)ImageSlot.Depth].Vulkan,
                    MotionVectors = images[(int)ImageSlot.Motion].Vulkan,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1,
                    MVScaleY = 1,
                    PreExposure = 1,
                    ExposureScale = 1,
                    FrameTimeDeltaInMsec = delta * 1000
                };

                result = Ngx.Vulkan.EvaluateDLSSDExt(command, reconstruction, parameters, evaluate);
            }
            else
            {
                NGXVKDLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        Color = images[(int)ImageSlot.Scene].Vulkan,
                        Output = images[(int)ImageSlot.Reconstructed].Vulkan
                    },
                    Depth = images[(int)ImageSlot.Depth].Vulkan,
                    MotionVectors = images[(int)ImageSlot.Motion].Vulkan,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1,
                    MVScaleY = 1,
                    PreExposure = 1,
                    ExposureScale = 1,
                    FrameTimeDeltaInMsec = delta * 1000
                };

                result = Ngx.Vulkan.EvaluateDLSSExt(command, reconstruction, parameters, evaluate);
            }
        }

        if (result is not NGXResult.Success)
        {
            throw new NGXException(result, nameof(EvaluateImages));
        }
    }
}
