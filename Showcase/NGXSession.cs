using System.Numerics;
using System.Runtime.InteropServices;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;
using Ngx = NGX.NET.NGX;
using NgxVersion = NGX.NET.Version;

namespace Showcase;

internal sealed unsafe class NGXSession : IDisposable
{
    public Dictionary<Feature, string> Unavailable { get; } = [];

    public bool IsVulkan
    {
        get; private set;
    }

    private nint device;
    private Parameter* capabilities;
    private Parameter* parameters;
    private Parameter* frameParameters;
    private Handle* reconstruction;
    private Handle* generation;
    private RenderSettings settings = new();
    private int inputWidth, inputHeight, outputWidth, outputHeight;
    private bool initialized;
    private readonly NativeWideString runtimePath = new(Ngx.RuntimeDirectory);
    private readonly NativeWideString dataPath = new(Path.Combine(AppContext.BaseDirectory, "Logs"));
    private readonly void** paths = (void**)NativeMemory.Alloc((nuint)sizeof(nint));

    public NGXSession()
    {
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
    }

    public bool Available(Feature feature) => !Unavailable.ContainsKey(feature);

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        IsVulkan = instance != 0;
        device = nativeDevice;
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
        *paths = runtimePath.Pointer;
        FeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = paths,
                Length = 1
            }
        };
        fixed (byte* project = "fc6ac847-10b0-48e1-842d-1bc819f8d2f4"u8)
        fixed (byte* engine = "NGX.NET.Showcase.1.0"u8)
        {
            Result result = IsVulkan
                ? Ngx.Vulkan.InitWithProjectID((sbyte*)project, EngineType.Custom, (sbyte*)engine, dataPath.Pointer, instance, physical, device,
                    (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getInstanceProcAddr,
                    (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getDeviceProcAddr, &common, (NgxVersion)Ngx.VersionAPI)
                : Ngx.D3D12.InitWithProjectID((sbyte*)project, EngineType.Custom, (sbyte*)engine, dataPath.Pointer, device, &common, (NgxVersion)Ngx.VersionAPI);
            if (result is Result.FailFeatureNotSupported or Result.FailPlatformError or Result.FailOutOfDate)
            {
                foreach (Feature feature in new[] { Feature.SuperSampling, Feature.RayReconstruction, Feature.FrameGeneration })
                {
                    Unavailable[feature] = $"NGX initialization: {result}";
                }

                Console.WriteLine($"NGX features unavailable: {result}. Native rendering remains available.");
                return;
            }

            Ngx.ThrowIfFailed(result);
        }

        initialized = true;
        capabilities = IsVulkan ? Ngx.Vulkan.GetCapabilityParameters() : Ngx.D3D12.GetCapabilityParameters();
        parameters = IsVulkan ? Ngx.Vulkan.AllocateParameters() : Ngx.D3D12.AllocateParameters();
        frameParameters = IsVulkan ? Ngx.Vulkan.AllocateParameters() : Ngx.D3D12.AllocateParameters();
        Query(Feature.SuperSampling, Ngx.ParameterSuperSamplingAvailable);
        Query(Feature.RayReconstruction, Ngx.ParameterSuperSamplingDenoisingAvailable);
        Query(Feature.FrameGeneration, Ngx.ParameterFrameGenerationAvailable);
    }

    public string[] VulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        *paths = runtimePath.Pointer;
        FeatureCommonInfo common = new()
        {
            PathListInfo = new()
            {
                Path = paths,
                Length = 1
            }
        };

        fixed (byte* project = "fc6ac847-10b0-48e1-842d-1bc819f8d2f4"u8)
        fixed (byte* engine = "NGX.NET.Showcase.1.0"u8)
        {
            foreach (Feature feature in new[] { Feature.SuperSampling, Feature.RayReconstruction, Feature.FrameGeneration })
            {
                FeatureDiscoveryInfo discovery = new()
                {
                    SDKVersion = (NgxVersion)Ngx.VersionAPI,
                    FeatureID = feature,
                    Identifier = new()
                    {
                        IdentifierType = ApplicationIdentifierType.ProjectId,
                        V = new()
                        {
                            ProjectDesc = new()
                            {
                                ProjectId = (sbyte*)project,
                                EngineType = EngineType.Custom,
                                EngineVersion = (sbyte*)engine
                            }
                        }
                    },
                    ApplicationDataPath = dataPath.Pointer,
                    FeatureInfo = &common
                };
                uint count = 0;
                NGX.NET.VkExtensionProperties* properties = null;
                Result result = instance == 0
                    ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(&discovery, &count, &properties)
                    : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, &discovery, &count, &properties);

                if (Ngx.Failed(result))
                {
                    Unavailable[feature] = $"Extension requirements: {result}";
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    extensions.Add(Marshal.PtrToStringUTF8((nint)properties[i].ExtensionName)!);
                }
            }
        }

        return [.. extensions];
    }

    private void Query(Feature feature, ReadOnlySpan<byte> name)
    {
        Result result = Ngx.Parameter.GetI(capabilities, name, out int available);

        if (Ngx.Failed(result) || available == 0)
        {
            Unavailable[feature] = Ngx.Failed(result) ? result.ToString() : "Not supported by this device/driver";
        }

        Console.WriteLine($"{feature}: {(Available(feature) ? "Available" : Unavailable[feature])}");
    }

    public (int Width, int Height) Configure(RenderSettings value, int width, int height)
    {
        settings = value with
        {
        };
        outputWidth = width;
        outputHeight = height;
        OptimalSettings optimal = value.Reconstruction switch
        {
            Reconstruction.DLSS => Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality),
            Reconstruction.RayReconstruction => Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality),
            _ => new((uint)width, (uint)height, (uint)width, (uint)height, (uint)width, (uint)height, 0)
        };
        inputWidth = (int)optimal.RenderWidth;
        inputHeight = (int)optimal.RenderHeight;

        return (inputWidth, inputHeight);
    }

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        int flags = (int)(DLSSFeatureFlags.IsHDR | DLSSFeatureFlags.MVLowRes | DLSSFeatureFlags.AutoExposure | DLSSFeatureFlags.DepthInverted);

        if (settings.Reconstruction == Reconstruction.RayReconstruction)
        {
            DLSSDCreateParams create = new()
            {
                InDenoiseMode = DLSSDenoiseMode.DLUnified,
                InRoughnessMode = DLSSRoughnessMode.Packed,
                InUseHWDepth = DLSSDepthType.Hw,
                InWidth = (uint)inputWidth,
                InHeight = (uint)inputHeight,
                InTargetWidth = (uint)outputWidth,
                InTargetHeight = (uint)outputHeight,
                InPerfQualityValue = settings.ReconstructionQuality,
                InFeatureCreateFlags = flags
            };
            reconstruction = IsVulkan ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1, 1, parameters, &create) : Ngx.D3D12.CreateDLSSDExt(command, 1, 1, parameters, &create);
        }
        else
        {
            DLSSCreateParams create = new()
            {
                Feature = new()
                {
                    InWidth = (uint)inputWidth,
                    InHeight = (uint)inputHeight,
                    InTargetWidth = (uint)outputWidth,
                    InTargetHeight = (uint)outputHeight,
                    InPerfQualityValue = settings.ReconstructionQuality
                },
                InFeatureCreateFlags = flags
            };
            reconstruction = IsVulkan ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1, 1, parameters, &create) : Ngx.D3D12.CreateDLSSExt(command, 1, 1, parameters, &create);
        }
    }

    public void Evaluate(nint command, GpuImage[] images, Camera camera, bool reset, float delta)
    {
        if (reconstruction == null)
        {
            CreateReconstruction(command);
        }

        NativeImage* descriptions = stackalloc NativeImage[images.Length];

        for (int i = 0; i < images.Length; i++)
        {
            descriptions[i] = images[i].Describe();
        }

        descriptions[(int)ImageSlot.Reconstructed].Vulkan.ReadWrite = true;
        EvaluateImages(command, descriptions, camera, reset, delta);
    }

    private void EvaluateImages(nint command, NativeImage* images, Camera camera, bool reset, float delta)
    {
        Matrix4x4 view = camera.View, projection = camera.Projection;
        Dimensions dimensions = new()
        {
            Width = (uint)inputWidth,
            Height = (uint)inputHeight
        };
        if (!IsVulkan)
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                D3D12DLSSDEvalParams evaluate = new()
                {
                    PInColor = images[(int)ImageSlot.Scene].DirectX,
                    PInOutput = images[(int)ImageSlot.Reconstructed].DirectX,
                    PInDiffuseAlbedo = images[(int)ImageSlot.Diffuse].DirectX,
                    PInSpecularAlbedo = images[(int)ImageSlot.Specular].DirectX,
                    PInNormals = images[(int)ImageSlot.Normal].DirectX,
                    PInMotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].DirectX,
                    PInWorldToViewMatrix = (float*)&view,
                    PInViewToClipMatrix = (float*)&projection,
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
                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate));
            }
            else
            {
                D3D12DLSSEvalParams evaluate = new()
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
                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate));
            }
        }
        else
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                VKDLSSDEvalParams evaluate = new()
                {
                    PInColor = &images[(int)ImageSlot.Scene].Vulkan,
                    PInOutput = &images[(int)ImageSlot.Reconstructed].Vulkan,
                    PInDiffuseAlbedo = &images[(int)ImageSlot.Diffuse].Vulkan,
                    PInSpecularAlbedo = &images[(int)ImageSlot.Specular].Vulkan,
                    PInNormals = &images[(int)ImageSlot.Normal].Vulkan,
                    PInMotionVectorsReflections = &images[(int)ImageSlot.SpecularMotion].Vulkan,
                    PInWorldToViewMatrix = (float*)&view,
                    PInViewToClipMatrix = (float*)&projection,
                    PInDepth = &images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
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
                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate));
            }
            else
            {
                VKDLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        PInColor = &images[(int)ImageSlot.Scene].Vulkan,
                        PInOutput = &images[(int)ImageSlot.Reconstructed].Vulkan
                    },
                    PInDepth = &images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
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
                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate));
            }
        }
    }

    public bool Generate(nint command, GpuImage[] images, GpuImage output, Camera camera, bool reset)
    {
        if (generation == null)
        {
            Ngx.Parameter.Reset(frameParameters);
            NativeImage color = images[(int)ImageSlot.Final].Describe();
            DLSSGCreateParams create = new()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                RenderWidth = (uint)inputWidth,
                RenderHeight = (uint)inputHeight,
                NativeBackbufferFormat = IsVulkan ? (uint)color.Vulkan.Resource.ImageViewInfo.Format : (uint)Vortice.DXGI.Format.R8G8B8A8_UNorm
            };
            generation = IsVulkan ? Ngx.Vulkan.CreateDLSSG(command, 1, 1, frameParameters, &create) : Ngx.D3D12.CreateDLSSG(command, 1, 1, frameParameters, &create);
            reset = true;
        }

        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 inverseViewProjection);
        Matrix4x4 clipToPrevious = inverseViewProjection * camera.PreviousViewProjection;
        Matrix4x4.Invert(clipToPrevious, out Matrix4x4 previousToClip);
        Vector3 forward = camera.Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        DLSSGOptEvalParams options = new()
        {
            CameraNear = camera.Near,
            CameraFar = camera.Far,
            CameraFOV = Camera.FieldOfView,
            CameraAspectRatio = (float)outputWidth / outputHeight,
            CameraMotionIncluded = true,
            DepthInverted = true,
            Reset = reset
        };
        *(Matrix4x4*)options.CameraViewToClip = camera.Projection;
        *(Matrix4x4*)options.ClipToCameraView = inverseProjection;
        *(Matrix4x4*)options.ClipToLensClip = Matrix4x4.Identity;
        *(Matrix4x4*)options.ClipToPrevClip = clipToPrevious;
        *(Matrix4x4*)options.PrevClipToClip = previousToClip;
        *(Vector3*)options.CameraPos = camera.Position;
        *(Vector3*)options.CameraFwd = forward;
        *(Vector3*)options.CameraRight = right;
        *(Vector3*)options.CameraUp = up;
        options.JitterOffset[0] = camera.Jitter.X;
        options.JitterOffset[1] = camera.Jitter.Y;
        options.MvecScale[0] = 1;
        options.MvecScale[1] = 1;
        NativeImage back = images[(int)ImageSlot.Final].Describe();
        NativeImage depth = images[(int)ImageSlot.Depth].Describe();
        NativeImage motion = images[(int)ImageSlot.Motion].Describe();
        NativeImage hudless = images[(int)ImageSlot.Hudless].Describe();
        NativeImage ui = images[(int)ImageSlot.UI].Describe();
        NativeImage generated = output.Describe();
        generated.Vulkan.ReadWrite = true;

        if (IsVulkan)
        {
            VKDLSSGEvalParams evaluate = new()
            {
                PBackbuffer = &back.Vulkan,
                PDepth = &depth.Vulkan,
                PMVecs = &motion.Vulkan,
                PHudless = &hudless.Vulkan,
                PUI = &ui.Vulkan,
                POutputInterpFrame = &generated.Vulkan
            };
            Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSG(command, generation, frameParameters, &evaluate, &options));
        }
        else
        {
            D3D12DLSSGEvalParams evaluate = new()
            {
                PBackbuffer = back.DirectX,
                PDepth = depth.DirectX,
                PMVecs = motion.DirectX,
                PHudless = hudless.DirectX,
                PUI = ui.DirectX,
                POutputInterpFrame = generated.DirectX
            };
            Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSG(command, generation, frameParameters, &evaluate, &options));
        }

        // A reset produces a copy of the real frame; present that real frame once.
        return !reset;
    }

    // The caller completes submitted GPU work before reconfiguration or disposal.
    public void ReleaseReconstruction()
    {
        Release(ref reconstruction);
        Release(ref generation);
    }

    private void Release(ref Handle* handle)
    {
        if (handle != null)
        {
            Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.ReleaseFeature(handle) : Ngx.D3D12.ReleaseFeature(handle));
            handle = null;
        }
    }

    public void Dispose()
    {
        if (initialized)
        {
            ReleaseReconstruction();

            foreach (nint value in new nint[] { (nint)frameParameters, (nint)parameters, (nint)capabilities })
            {
                if (value != 0)
                {
                    Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.DestroyParameters((Parameter*)value) : Ngx.D3D12.DestroyParameters((Parameter*)value));
                }
            }

            Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device));
            initialized = false;
        }

        NativeMemory.Free(paths);
        runtimePath.Dispose();
        dataPath.Dispose();
    }
}
