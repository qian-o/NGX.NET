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

        Ngx.ThrowIfFailed(result);
        initialized = true;
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.GetCapabilityParameters(out capabilities) : Ngx.D3D12.GetCapabilityParameters(out capabilities));
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.AllocateParameters(out parameters) : Ngx.D3D12.AllocateParameters(out parameters));
        Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.AllocateParameters(out frameParameters) : Ngx.D3D12.AllocateParameters(out frameParameters));

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
            NGXResult result = instance is 0 ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(discovery, out NGXVkExtensionProperties[] properties) : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, discovery, out properties);
            if (Ngx.Failed(result))
            {
                Unavailable[feature] = $"Extension requirements: {result}";

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
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device));
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
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.CreateDLSSG(command, 1, 1, out NGXHandle created, frameParameters, create) : Ngx.D3D12.CreateDLSSG(command, 1, 1, out created, frameParameters, create));
            generation = created;
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

        if (isVulkan)
        {
            NGXVKDLSSGEvalParams evaluate = new()
            {
                PBackbuffer = back.Vulkan,
                PDepth = depth.Vulkan,
                PMVecs = motion.Vulkan,
                PHudless = hudless.Vulkan,
                PUI = ui.Vulkan,
                POutputInterpFrame = generated.Vulkan
            };

            Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSG(command, generation, frameParameters, evaluate, options));
        }
        else
        {
            NGXD3D12DLSSGEvalParams evaluate = new()
            {
                PBackbuffer = back.DirectX,
                PDepth = depth.DirectX,
                PMVecs = motion.DirectX,
                PHudless = hudless.DirectX,
                PUI = ui.DirectX,
                POutputInterpFrame = generated.DirectX
            };

            Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSG(command, generation, frameParameters, evaluate, options));
        }

        // A reset produces a copy of the real frame; present that real frame once.
        return !reset;
    }

    public (int Width, int Height) Configure(RenderSettings value, int width, int height)
    {
        settings = value;
        outputWidth = width;
        outputHeight = height;
        uint renderWidth = 0;
        uint renderHeight = 0;
        if (value.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
        {
            NGXResult result = value.Reconstruction is Reconstruction.DLSS ? Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, out renderWidth, out renderHeight, out uint maxWidth, out uint maxHeight, out uint minWidth, out uint minHeight, out float sharpness) : Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, out renderWidth, out renderHeight, out maxWidth, out maxHeight, out minWidth, out minHeight, out sharpness);
            Ngx.ThrowIfFailed(result);
        }
        else
        {
            renderWidth = (uint)width;
            renderHeight = (uint)height;
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
        NGXResult result = Ngx.Parameter.GetI(capabilities, name, out int available);
        if (Ngx.Failed(result) || available is 0)
        {
            Unavailable[feature] = Ngx.Failed(result) ? result.ToString() : "Not supported by this device/driver";
        }

        Console.WriteLine($"{feature}: {(Available(feature) ? "Available" : Unavailable[feature])}");
    }

    private void Release(ref NGXHandle handle)
    {
        if (!handle.IsNull)
        {
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle));
            handle = default;
        }
    }

    private void DestroyParameters(ref NGXParameter value)
    {
        if (!value.IsNull)
        {
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.DestroyParameters(value) : Ngx.D3D12.DestroyParameters(value));
            value = default;
        }
    }

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        const int Flags = (int)(NGXDLSSFeatureFlags.IsHdr | NGXDLSSFeatureFlags.MvLowRes | NGXDLSSFeatureFlags.AutoExposure | NGXDLSSFeatureFlags.DepthInverted);
        NGXHandle created = default;
        NGXResult result;
        if (settings.Reconstruction is Reconstruction.RayReconstruction)
        {
            NGXDLSSDCreateParams create = new()
            {
                InDenoiseMode = NGXDLSSDenoiseMode.DlUnified,
                InRoughnessMode = NGXDLSSRoughnessMode.Packed,
                InUseHWDepth = NGXDLSSDepthType.Hw,
                InWidth = (uint)inputWidth,
                InHeight = (uint)inputHeight,
                InTargetWidth = (uint)outputWidth,
                InTargetHeight = (uint)outputHeight,
                InPerfQualityValue = settings.ReconstructionQuality,
                InFeatureCreateFlags = Flags
            };

            result = isVulkan ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1, 1, out created, parameters, create) : Ngx.D3D12.CreateDLSSDExt(command, 1, 1, out created, parameters, create);
        }
        else
        {
            NGXDLSSCreateParams create = new()
            {
                Feature = new()
                {
                    InWidth = (uint)inputWidth,
                    InHeight = (uint)inputHeight,
                    InTargetWidth = (uint)outputWidth,
                    InTargetHeight = (uint)outputHeight,
                    InPerfQualityValue = settings.ReconstructionQuality
                },
                InFeatureCreateFlags = Flags
            };

            result = isVulkan ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1, 1, out created, parameters, create) : Ngx.D3D12.CreateDLSSExt(command, 1, 1, out created, parameters, create);
        }

        Ngx.ThrowIfFailed(result);
        reconstruction = created;
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
        if (!isVulkan)
        {
            if (settings.Reconstruction is Reconstruction.RayReconstruction)
            {
                NGXD3D12DLSSDEvalParams evaluate = new()
                {
                    PInColor = images[(int)ImageSlot.Scene].DirectX,
                    PInOutput = images[(int)ImageSlot.Reconstructed].DirectX,
                    PInDiffuseAlbedo = images[(int)ImageSlot.Diffuse].DirectX,
                    PInSpecularAlbedo = images[(int)ImageSlot.Specular].DirectX,
                    PInNormals = images[(int)ImageSlot.Normal].DirectX,
                    PInMotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].DirectX,
                    PInWorldToViewMatrix = view,
                    PInViewToClipMatrix = projection,
                    PInDepth = images[(int)ImageSlot.Depth].DirectX,
                    PInMotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSDExt(command, reconstruction, parameters, evaluate));
            }
            else
            {
                NGXD3D12DLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        PInColor = images[(int)ImageSlot.Scene].DirectX,
                        PInOutput = images[(int)ImageSlot.Reconstructed].DirectX
                    },
                    PInDepth = images[(int)ImageSlot.Depth].DirectX,
                    PInMotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, evaluate));
            }
        }
        else
        {
            if (settings.Reconstruction is Reconstruction.RayReconstruction)
            {
                NGXVKDLSSDEvalParams evaluate = new()
                {
                    PInColor = images[(int)ImageSlot.Scene].Vulkan,
                    PInOutput = images[(int)ImageSlot.Reconstructed].Vulkan,
                    PInDiffuseAlbedo = images[(int)ImageSlot.Diffuse].Vulkan,
                    PInSpecularAlbedo = images[(int)ImageSlot.Specular].Vulkan,
                    PInNormals = images[(int)ImageSlot.Normal].Vulkan,
                    PInMotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].Vulkan,
                    PInWorldToViewMatrix = view,
                    PInViewToClipMatrix = projection,
                    PInDepth = images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = images[(int)ImageSlot.Motion].Vulkan,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSDExt(command, reconstruction, parameters, evaluate));
            }
            else
            {
                NGXVKDLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        PInColor = images[(int)ImageSlot.Scene].Vulkan,
                        PInOutput = images[(int)ImageSlot.Reconstructed].Vulkan
                    },
                    PInDepth = images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = images[(int)ImageSlot.Motion].Vulkan,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSExt(command, reconstruction, parameters, evaluate));
            }
        }
    }
}
