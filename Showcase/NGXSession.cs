using System.Numerics;
using System.Runtime.InteropServices;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;
using Ngx = NGX.NET.NGX;

namespace Showcase;

internal sealed unsafe class NGXSession : IDisposable
{
    public Dictionary<NGXFeature, string> Unavailable { get; } = [];

    public bool IsVulkan
    {
        get; private set;
    }

    private nint device;
    private NGXParameter* capabilities;
    private NGXParameter* parameters;
    private NGXParameter* frameParameters;
    private NGXHandle* reconstruction;
    private NGXHandle* generation;
    private RenderSettings settings = new();
    private int inputWidth, inputHeight, outputWidth, outputHeight;
    private bool initialized;
    private void* runtimePath;
    private void* dataPath;
    private void** paths;

    public NGXSession()
    {
        string logDirectory = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logDirectory);

        try
        {
            runtimePath = NGXMarshal.StringToPtr(Ngx.RuntimeDirectory, NGXEncoding.NativeWide);
            dataPath = NGXMarshal.StringToPtr(logDirectory, NGXEncoding.NativeWide);
            paths = (void**)NativeMemory.Alloc((nuint)sizeof(nint));
        }
        catch
        {
            NativeMemory.Free(paths);
            NGXMarshal.Free(dataPath);
            NGXMarshal.Free(runtimePath);
            throw;
        }
    }

    public bool Available(NGXFeature feature) => !Unavailable.ContainsKey(feature);

    public void Initialize(nint nativeDevice, nint instance = 0, nint physical = 0, nint getInstanceProcAddr = 0, nint getDeviceProcAddr = 0)
    {
        IsVulkan = instance != 0;
        device = nativeDevice;
        Directory.CreateDirectory(Path.Combine(AppContext.BaseDirectory, "Logs"));
        *paths = runtimePath;
        NGXFeatureCommonInfo common = new()
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
            NGXResult result = IsVulkan
                ? Ngx.Vulkan.InitWithProjectID((sbyte*)project, NGXEngineType.CUSTOM, (sbyte*)engine, dataPath, instance, physical, device,
                    (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getInstanceProcAddr,
                    (delegate* unmanaged[Cdecl]<nint, sbyte*, delegate* unmanaged[Cdecl]<void>>)getDeviceProcAddr, &common, (NGXVersion)Ngx.VersionAPI)
                : Ngx.D3D12.InitWithProjectID((sbyte*)project, NGXEngineType.CUSTOM, (sbyte*)engine, dataPath, device, &common, (NGXVersion)Ngx.VersionAPI);
            if (result is NGXResult.FAILFeatureNotSupported or NGXResult.FAILPlatformError or NGXResult.FAILOutOfDate)
            {
                foreach (NGXFeature feature in new[] { NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration })
                {
                    Unavailable[feature] = $"NGX initialization: {result}";
                }

                Console.WriteLine($"NGX features unavailable: {result}. Native rendering remains available.");
                return;
            }

            Ngx.ThrowIfFailed(result);
        }

        initialized = true;
        NGXParameter* allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.GetCapabilityParameters(&allocated) : Ngx.D3D12.GetCapabilityParameters(&allocated));
        capabilities = allocated;
        allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.AllocateParameters(&allocated) : Ngx.D3D12.AllocateParameters(&allocated));
        parameters = allocated;
        allocated = null;
        Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.AllocateParameters(&allocated) : Ngx.D3D12.AllocateParameters(&allocated));
        frameParameters = allocated;

        fixed (byte* superSampling = Ngx.ParameterSuperSamplingAvailable)
        fixed (byte* rayReconstruction = Ngx.ParameterSuperSamplingDenoisingAvailable)
        fixed (byte* frameGeneration = Ngx.ParameterFrameGenerationAvailable)
        {
            Query(NGXFeature.SuperSampling, (sbyte*)superSampling);
            Query(NGXFeature.RayReconstruction, (sbyte*)rayReconstruction);
            Query(NGXFeature.FrameGeneration, (sbyte*)frameGeneration);
        }
    }

    public string[] VulkanExtensions(nint instance = 0, nint physical = 0)
    {
        HashSet<string> extensions = [];
        *paths = runtimePath;
        NGXFeatureCommonInfo common = new()
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
            foreach (NGXFeature feature in new[] { NGXFeature.SuperSampling, NGXFeature.RayReconstruction, NGXFeature.FrameGeneration })
            {
                NGXFeatureDiscoveryInfo discovery = new()
                {
                    SDKVersion = (NGXVersion)Ngx.VersionAPI,
                    FeatureID = feature,
                    Identifier = new()
                    {
                        IdentifierType = NGXApplicationIdentifierType.ProjectId,
                        V = new()
                        {
                            ProjectDesc = new()
                            {
                                ProjectId = (sbyte*)project,
                                EngineType = NGXEngineType.CUSTOM,
                                EngineVersion = (sbyte*)engine
                            }
                        }
                    },
                    ApplicationDataPath = dataPath,
                    FeatureInfo = &common
                };
                uint count = 0;
                NGXVkExtensionProperties* properties = null;
                NGXResult result = instance == 0
                    ? Ngx.Vulkan.GetFeatureInstanceExtensionRequirements(&discovery, &count, &properties)
                    : Ngx.Vulkan.GetFeatureDeviceExtensionRequirements(instance, physical, &discovery, &count, &properties);

                if (Ngx.Failed(result))
                {
                    Unavailable[feature] = $"Extension requirements: {result}";
                    continue;
                }

                for (int i = 0; i < count; i++)
                {
                    extensions.Add(NGXMarshal.PtrToString(properties[i].ExtensionName, NGXEncoding.Utf8)!);
                }
            }
        }

        return [.. extensions];
    }

    private void Query(NGXFeature feature, sbyte* name)
    {
        int available = 0;
        NGXResult result = Ngx.Parameter.GetI(capabilities, name, &available);

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
        uint renderWidth = 0, renderHeight = 0;

        if (value.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
        {
            uint maxWidth = 0, maxHeight = 0, minWidth = 0, minHeight = 0;
            float sharpness = 0;
            NGXResult result = value.Reconstruction == Reconstruction.DLSS
                ? Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality,
                    &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness)
                : Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality,
                    &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness);
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

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        int flags = (int)(NGXDLSSFeatureFlags.IsHDR | NGXDLSSFeatureFlags.MVLowRes | NGXDLSSFeatureFlags.AutoExposure | NGXDLSSFeatureFlags.DepthInverted);
        NGXHandle* created = null;
        NGXResult result;

        if (settings.Reconstruction == Reconstruction.RayReconstruction)
        {
            NGXDLSSDCreateParams create = new()
            {
                InDenoiseMode = NGXDLSSDenoiseMode.DLUnified,
                InRoughnessMode = NGXDLSSRoughnessMode.Packed,
                InUseHWDepth = NGXDLSSDepthType.HW,
                InWidth = (uint)inputWidth,
                InHeight = (uint)inputHeight,
                InTargetWidth = (uint)outputWidth,
                InTargetHeight = (uint)outputHeight,
                InPerfQualityValue = settings.ReconstructionQuality,
                InFeatureCreateFlags = flags
            };
            result = IsVulkan
                ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1, 1, &created, parameters, &create)
                : Ngx.D3D12.CreateDLSSDExt(command, 1, 1, &created, parameters, &create);
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
                InFeatureCreateFlags = flags
            };
            result = IsVulkan
                ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1, 1, &created, parameters, &create)
                : Ngx.D3D12.CreateDLSSExt(command, 1, 1, &created, parameters, &create);
        }

        Ngx.ThrowIfFailed(result);
        reconstruction = created;
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
        NGXDimensions dimensions = new()
        {
            Width = (uint)inputWidth,
            Height = (uint)inputHeight
        };
        if (!IsVulkan)
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                NGXD3D12DLSSDEvalParams evaluate = new()
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
                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate));
            }
        }
        else
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                NGXVKDLSSDEvalParams evaluate = new()
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
                NGXVKDLSSEvalParams evaluate = new()
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
            NGXDLSSGCreateParams create = new()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                RenderWidth = (uint)inputWidth,
                RenderHeight = (uint)inputHeight,
                NativeBackbufferFormat = IsVulkan ? (uint)color.Vulkan.Resource.ImageViewInfo.Format : (uint)Vortice.DXGI.Format.R8G8B8A8_UNorm
            };
            NGXHandle* created = null;
            Ngx.ThrowIfFailed(IsVulkan
                ? Ngx.Vulkan.CreateDLSSG(command, 1, 1, &created, frameParameters, &create)
                : Ngx.D3D12.CreateDLSSG(command, 1, 1, &created, frameParameters, &create));
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
            NGXVKDLSSGEvalParams evaluate = new()
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
            NGXD3D12DLSSGEvalParams evaluate = new()
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

    private void Release(ref NGXHandle* handle)
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
                    Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.DestroyParameters((NGXParameter*)value) : Ngx.D3D12.DestroyParameters((NGXParameter*)value));
                }
            }

            Ngx.ThrowIfFailed(IsVulkan ? Ngx.Vulkan.Shutdown1(device) : Ngx.D3D12.Shutdown1(device));
            initialized = false;
        }

        NativeMemory.Free(paths);
        paths = null;
        NGXMarshal.Free(runtimePath);
        runtimePath = null;
        NGXMarshal.Free(dataPath);
        dataPath = null;
    }
}
