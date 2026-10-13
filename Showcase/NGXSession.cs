using System.Numerics;
using System.Text;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal unsafe class NGXSession : DisposableObject
{
    private const string ProjectId = "fc6ac847-10b0-48e1-842d-1bc819f8d2f4";
    private const string EngineVersion = "NGX.NET.Showcase.1.0";

    private static readonly NGXFeature[] Features = [NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration];

    private readonly string dataPath;
    // Each frame slot remains owned until rendering and presentation complete.
    private readonly NativeImage[] images = GC.AllocateArray<NativeImage>(RenderLayout.FramesInFlight * ((int)ImageSlot.Count + 1), pinned: true);
    private readonly NGXDLSSGOptEvalParams[] options = GC.AllocateArray<NGXDLSSGOptEvalParams>(RenderLayout.FramesInFlight, pinned: true);
    private readonly Matrix4x4[] matrices = GC.AllocateArray<Matrix4x4>(RenderLayout.FramesInFlight * 2, pinned: true);

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
    private bool isInitialized;

    public NGXSession()
    {
        dataPath = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(dataPath);
    }

    public Dictionary<NGXFeature, string> Unavailable { get; } = [];

    public bool IsAvailable(NGXFeature feature)
    {
        return !Unavailable.ContainsKey(feature);
    }

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        isVulkan = instance is not 0;
        device = nativeDevice;
        using NGXStrings strings = new();
        void* path = strings.AllocWide(Ngx.RuntimeDirectory);
        NGXFeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = &path,
                Length = 1u
            }
        };
        NGXResult result = isVulkan ? Ngx.Vulkan.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, instance, physical, device, getInstanceProcAddr, getDeviceProcAddr, &common, NGXVersion.Api) : Ngx.D3D12.InitWithProjectID(ProjectId, NGXEngineType.Custom, EngineVersion, dataPath, device, &common, NGXVersion.Api);
        if (result is NGXResult.FailFeatureNotSupported or NGXResult.FailPlatformError or NGXResult.FailOutOfDate)
        {
            foreach (NGXFeature feature in Features)
            {
                Unavailable[feature] = $"NGX initialization: {result}";
            }

            Console.WriteLine($"NGX features unavailable: {result}. Native rendering remains available.");

            return;
        }

        Check(result, "NGX initialization");

        isInitialized = true;
        result = isVulkan ? Ngx.Vulkan.GetCapabilityParameters(out NGXParameter allocated) : Ngx.D3D12.GetCapabilityParameters(out allocated);
        Check(result, "NGX capability allocation");

        capabilities = allocated;
        result = isVulkan ? Ngx.Vulkan.AllocateParameters(out allocated) : Ngx.D3D12.AllocateParameters(out allocated);
        Check(result, "NGX reconstruction parameter allocation");

        parameters = allocated;
        result = isVulkan ? Ngx.Vulkan.AllocateParameters(out allocated) : Ngx.D3D12.AllocateParameters(out allocated);
        Check(result, "NGX frame generation parameter allocation");

        frameParameters = allocated;
        Query(NGXFeature.SuperSampling, Ngx.ParameterSuperSamplingAvailable);
        Query(NGXFeature.RayReconstruction, Ngx.ParameterSuperSamplingDenoisingAvailable);
        Query(NGXFeature.FrameGeneration, Ngx.ParameterFrameGenerationAvailable);
    }

    public string[] GetVulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        using NGXStrings strings = new();
        byte* projectId = strings.AllocUtf8(ProjectId);
        byte* engineVersion = strings.AllocUtf8(EngineVersion);
        void* applicationDataPath = strings.AllocWide(dataPath);
        void* path = strings.AllocWide(Ngx.RuntimeDirectory);
        NGXFeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = &path,
                Length = 1u
            }
        };
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
                            ProjectId = projectId,
                            EngineType = NGXEngineType.Custom,
                            EngineVersion = engineVersion
                        }
                    }
                },
                ApplicationDataPath = applicationDataPath,
                FeatureInfo = &common
            };
            uint count;
            NGXVkExtensionProperties* properties;
            NGXResult result = instance is 0 ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(&discovery, &count, &properties) : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, &discovery, &count, &properties);
            if (Ngx.NVSDK_NGX_FAILED(result))
            {
                Unavailable[feature] = $"Extension requirements: {result}";

                continue;
            }

            for (uint i = 0u; i < count; i++)
            {
                ReadOnlySpan<byte> name = properties[i].ExtensionName;
                int terminator = name.IndexOf((byte)0);
                extensions.Add(Encoding.UTF8.GetString(terminator < 0 ? name : name[..terminator]));
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

    public bool Generate(int slot, nint command, ReadOnlySpan<GpuImage> source, GpuImage output, CameraHandler camera, bool reset)
    {
        if (generation.IsNull)
        {
            CreateFrameGeneration(command, source[(int)ImageSlot.Final].Describe());
            reset = true;
        }

        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 inverseViewProjection);
        Matrix4x4 clipToPrevious = inverseViewProjection * camera.PreviousViewProjection;
        Matrix4x4.Invert(clipToPrevious, out Matrix4x4 previousToClip);
        Vector3 forward = camera.Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        options[slot] = new()
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
            DepthInverted = true,
            CameraMotionIncluded = true,
            Reset = reset
        };

        int offset = slot * ((int)ImageSlot.Count + 1);
        for (int i = 0; i < source.Length; i++)
        {
            images[offset + i] = source[i].Describe();
        }

        images[offset + (int)ImageSlot.Count] = output.Describe();
        images[offset + (int)ImageSlot.Count].Vulkan.ReadWrite = true;
        NGXResult result;
        fixed (NativeImage* frame = &images[offset])
        {
            fixed (NGXDLSSGOptEvalParams* frameOptions = &options[slot])
            {
                if (isVulkan)
                {
                    NGXVKDLSSGEvalParams evaluate = new()
                    {
                        Backbuffer = &frame[(int)ImageSlot.Final].Vulkan,
                        Depth = &frame[(int)ImageSlot.Depth].Vulkan,
                        MVecs = &frame[(int)ImageSlot.Motion].Vulkan,
                        Hudless = &frame[(int)ImageSlot.Hudless].Vulkan,
                        UI = &frame[(int)ImageSlot.UI].Vulkan,
                        OutputInterpFrame = &frame[(int)ImageSlot.Count].Vulkan
                    };

                    result = Ngx.Vulkan.EvaluateDLSSG(command, generation, frameParameters, &evaluate, frameOptions);
                }
                else
                {
                    NGXD3D12DLSSGEvalParams evaluate = new()
                    {
                        Backbuffer = frame[(int)ImageSlot.Final].DirectX,
                        Depth = frame[(int)ImageSlot.Depth].DirectX,
                        MVecs = frame[(int)ImageSlot.Motion].DirectX,
                        Hudless = frame[(int)ImageSlot.Hudless].DirectX,
                        UI = frame[(int)ImageSlot.UI].DirectX,
                        OutputInterpFrame = frame[(int)ImageSlot.Count].DirectX
                    };

                    result = Ngx.D3D12.EvaluateDLSSG(command, generation, frameParameters, &evaluate, frameOptions);
                }
            }
        }

        Check(result, "NGX frame generation evaluation");

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
            NGXResult result = value.Reconstruction is Reconstruction.DLSS ? Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, out renderWidth, out renderHeight, out _, out _, out _, out _, out _) : Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, out renderWidth, out renderHeight, out _, out _, out _, out _, out _);
            Check(result, "NGX optimal settings query");
        }

        inputWidth = (int)renderWidth;
        inputHeight = (int)renderHeight;

        return (inputWidth, inputHeight);
    }

    public void Evaluate(int slot, nint command, ReadOnlySpan<GpuImage> source, CameraHandler camera, bool reset, float deltaSeconds)
    {
        if (reconstruction.IsNull)
        {
            CreateReconstruction(command);
        }

        int offset = slot * ((int)ImageSlot.Count + 1);
        for (int i = 0; i < source.Length; i++)
        {
            images[offset + i] = source[i].Describe();
        }

        images[offset + (int)ImageSlot.Reconstructed].Vulkan.ReadWrite = true;
        matrices[slot * 2] = camera.View;
        matrices[(slot * 2) + 1] = camera.Projection;
        fixed (NativeImage* frame = &images[offset])
        {
            fixed (Matrix4x4* transforms = &matrices[slot * 2])
            {
                EvaluateImages(command, frame, transforms, camera, reset, deltaSeconds);
            }
        }
    }

    protected override void Destroy()
    {
        if (!isInitialized)
        {
            return;
        }

        ReleaseFrameGeneration();
        ReleaseReconstruction();
        DestroyParameters(ref frameParameters);
        DestroyParameters(ref parameters);
        DestroyParameters(ref capabilities);
        NGXResult result = isVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device);
        if (Ngx.NVSDK_NGX_FAILED(result))
        {
            Console.Error.WriteLine($"NGX shutdown failed: {result}.");
        }

        isInitialized = false;
        GC.KeepAlive(images);
        GC.KeepAlive(options);
        GC.KeepAlive(matrices);
    }

    private void Query(NGXFeature feature, ReadOnlySpan<byte> name)
    {
        NGXResult result;
        int available;
        fixed (byte* pName = name)
        {
            result = Ngx.Parameter.GetI(capabilities, pName, &available);
        }

        if (Ngx.NVSDK_NGX_FAILED(result))
        {
            Unavailable[feature] = result.ToString();
        }
        else if (available is 0)
        {
            Unavailable[feature] = "Not supported by this device/driver";
        }

        Console.WriteLine($"{feature}: {(IsAvailable(feature) ? "Available" : Unavailable[feature])}");
    }

    private void Release(ref NGXHandle handle)
    {
        if (handle.IsNull)
        {
            return;
        }

        NGXResult result = isVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle);
        if (Ngx.NVSDK_NGX_FAILED(result))
        {
            Console.Error.WriteLine($"NGX feature release failed: {result}.");
        }

        handle = default;
    }

    private void DestroyParameters(ref NGXParameter value)
    {
        if (value.IsNull)
        {
            return;
        }

        NGXResult result = isVulkan ? Ngx.Vulkan.DestroyParameters(value) : Ngx.D3D12.DestroyParameters(value);
        if (Ngx.NVSDK_NGX_FAILED(result))
        {
            Console.Error.WriteLine($"NGX parameter destruction failed: {result}.");
        }

        value = default;
    }

    private void CreateFrameGeneration(nint command, NativeImage color)
    {
        Ngx.Parameter.Reset(frameParameters);
        NGXDLSSGCreateParams create = new()
        {
            Width = (uint)outputWidth,
            Height = (uint)outputHeight,
            NativeBackbufferFormat = isVulkan ? (uint)color.Vulkan.Resource.ImageViewInfo.Format : (uint)Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm,
            RenderWidth = (uint)inputWidth,
            RenderHeight = (uint)inputHeight
        };
        NGXResult createResult = isVulkan ? Ngx.Vulkan.CreateDLSSG(command, 1u, 1u, out NGXHandle handle, frameParameters, in create) : Ngx.D3D12.CreateDLSSG(command, 1u, 1u, out handle, frameParameters, in create);
        Check(createResult, "NGX frame generation creation");

        generation = handle;
    }

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        const int Flags = (int)(NGXDLSSFeatureFlags.IsHdr | NGXDLSSFeatureFlags.MvLowRes | NGXDLSSFeatureFlags.AutoExposure | NGXDLSSFeatureFlags.DepthInverted);
        NGXHandle handle;
        NGXResult result;
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

            result = isVulkan ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1u, 1u, out handle, parameters, in create) : Ngx.D3D12.CreateDLSSDExt(command, 1u, 1u, out handle, parameters, in create);
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

            result = isVulkan ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1u, 1u, out handle, parameters, in create) : Ngx.D3D12.CreateDLSSExt(command, 1u, 1u, out handle, parameters, in create);
        }

        Check(result, "NGX reconstruction creation");

        reconstruction = handle;
    }

    private void EvaluateImages(nint command, NativeImage* images, Matrix4x4* transforms, CameraHandler camera, bool reset, float deltaSeconds)
    {
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
                    DiffuseAlbedo = images[(int)ImageSlot.Diffuse].DirectX,
                    SpecularAlbedo = images[(int)ImageSlot.Specular].DirectX,
                    Normals = images[(int)ImageSlot.Normal].DirectX,
                    Color = images[(int)ImageSlot.Scene].DirectX,
                    Output = images[(int)ImageSlot.Reconstructed].DirectX,
                    Depth = images[(int)ImageSlot.Depth].DirectX,
                    MotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1.0f,
                    MVScaleY = 1.0f,
                    SpecularHitDistance = images[(int)ImageSlot.SpecularHitDistance].DirectX,
                    WorldToViewMatrix = transforms,
                    ViewToClipMatrix = transforms + 1,
                    PreExposure = 1.0f,
                    ExposureScale = 1.0f,
                    FrameTimeDeltaInMsec = deltaSeconds * 1000.0f
                };

                result = Ngx.D3D12.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate);
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
                    MVScaleX = 1.0f,
                    MVScaleY = 1.0f,
                    PreExposure = 1.0f,
                    ExposureScale = 1.0f,
                    FrameTimeDeltaInMsec = deltaSeconds * 1000.0f
                };

                result = Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate);
            }
        }
        else
        {
            if (settings.Reconstruction is Reconstruction.RayReconstruction)
            {
                NGXVKDLSSDEvalParams evaluate = new()
                {
                    DiffuseAlbedo = &images[(int)ImageSlot.Diffuse].Vulkan,
                    SpecularAlbedo = &images[(int)ImageSlot.Specular].Vulkan,
                    Normals = &images[(int)ImageSlot.Normal].Vulkan,
                    Color = &images[(int)ImageSlot.Scene].Vulkan,
                    Output = &images[(int)ImageSlot.Reconstructed].Vulkan,
                    Depth = &images[(int)ImageSlot.Depth].Vulkan,
                    MotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1.0f,
                    MVScaleY = 1.0f,
                    PreExposure = 1.0f,
                    ExposureScale = 1.0f,
                    SpecularHitDistance = &images[(int)ImageSlot.SpecularHitDistance].Vulkan,
                    WorldToViewMatrix = transforms,
                    ViewToClipMatrix = transforms + 1,
                    FrameTimeDeltaInMsec = deltaSeconds * 1000.0f
                };

                result = Ngx.Vulkan.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate);
            }
            else
            {
                NGXVKDLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        Color = &images[(int)ImageSlot.Scene].Vulkan,
                        Output = &images[(int)ImageSlot.Reconstructed].Vulkan
                    },
                    Depth = &images[(int)ImageSlot.Depth].Vulkan,
                    MotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
                    JitterOffsetX = camera.Jitter.X,
                    JitterOffsetY = camera.Jitter.Y,
                    RenderSubrectDimensions = dimensions,
                    Reset = reset ? 1 : 0,
                    MVScaleX = 1.0f,
                    MVScaleY = 1.0f,
                    PreExposure = 1.0f,
                    ExposureScale = 1.0f,
                    FrameTimeDeltaInMsec = deltaSeconds * 1000.0f
                };

                result = Ngx.Vulkan.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate);
            }
        }

        Check(result, "NGX reconstruction evaluation");
    }

    private static void Check(NGXResult result, string operation)
    {
        if (Ngx.NVSDK_NGX_FAILED(result))
        {
            throw new InvalidOperationException($"{operation} failed: {result}.");
        }
    }
}
