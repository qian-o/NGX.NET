using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Streamline.NET;

namespace Showcase;

internal sealed unsafe class StreamlineSession : IDisposable
{
    private static readonly (uint Id, string Name)[] Features =
    [
        (SL.FeatureDLSS, "DLSS Super Resolution / Deep Learning Anti-Aliasing (DLAA)"),
        (SL.FeatureDLSSRR, "DLSS Ray Reconstruction"), (SL.FeatureDLSSG, "DLSS Frame Generation"),
        (SL.FeatureReflex, "Reflex"), (SL.FeaturePCL, "Latency markers")
    ];
    public Dictionary<uint, string> Unavailable { get; } = [];
    public nint Module
    {
        get; private set;
    }
    public uint LatencyPingMessage
    {
        get; private set;
    }
    public uint MaximumGeneratedFrames
    {
        get; private set;
    }
    public uint MinimumFGDimension
    {
        get; private set;
    }
    public bool FrameGenerationFailed => lastStateResult != SLResult.Ok || frameGenerationIssue is not null;
    private readonly ViewportHandle viewport = new(0);
    private FrameToken frame;
    private DLSSGStatus? frameGenerationIssue;
    private SLResult lastStateResult = SLResult.Ok;
    private bool frameGenerationLoaded;
    private bool initialized;
    private readonly List<nint> allocations = [];
    private readonly Resource* descriptions = (Resource*)NativeMemory.AllocZeroed(RenderLayout.FramesInFlight * (uint)ImageSlot.Count, (nuint)sizeof(Resource));
    private readonly HashSet<uint> taggedTypes = [];
    private Reconstruction reconstruction;
    private uint? evaluatedFeature;
    private DLSSOptions dlssOptions = new();
    private DLSSDOptions rayOptions = new();
    private static readonly ConcurrentQueue<string> messages = new();
    private static int apiError;

    public bool Available(uint feature) => !Unavailable.ContainsKey(feature);
    public static void Check(SLResult result, string operation)
    {
        if (result != SLResult.Ok)
        {
            throw new SLException(result, operation);
        }
    }

    private void* Keep(nint pointer)
    {
        allocations.Add(pointer);
        return (void*)pointer;
    }
    private sbyte* Utf8(string value) => (sbyte*)Keep(Marshal.StringToCoTaskMemUTF8(value));
    private char* Utf16(string value) => (char*)Keep(Marshal.StringToCoTaskMemUni(value));

    public void Initialize(RenderAPI api)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "sl.interposer.dll");
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Run Showcase/Assets/UpdateAssets.ps1, then rebuild Showcase.", path);
        }

        if (!SL.VerifyEmbeddedSignature(path))
        {
            throw new InvalidDataException($"The NVIDIA production interposer signature could not be verified: {path}");
        }

        SL.SetLibraryPath(path);
        char** plugins = (char**)Keep(Marshal.AllocCoTaskMem(sizeof(nint)));
        *plugins = Utf16(AppContext.BaseDirectory);
        string logs = Path.Combine(AppContext.BaseDirectory, "Logs");
        Directory.CreateDirectory(logs);
        uint[] features = Features.Select(x => x.Id).ToArray();
        uint* requested = (uint*)Keep(Marshal.AllocCoTaskMem(features.Length * sizeof(uint)));
        features.CopyTo(new Span<uint>(requested, features.Length));
        Preferences preferences = new()
        {
            RenderAPI = api,
            Engine = EngineType.Custom,
            EngineVersion = Utf8("Streamline.NET.Showcase.1.0"),
            ProjectId = Utf8("fc6ac847-10b0-48e1-842d-1bc819f8d2f4"),
            PathsToPlugins = plugins,
            NumPathsToPlugins = 1,
            PathToLogsAndData = Utf16(logs),
            LogMessageCallback = &Log,
            LogLevel = LogLevel.Default,
            Flags = PreferenceFlags.DisableCLStateTracking | PreferenceFlags.UseFrameBasedResourceTagging,
            FeaturesToLoad = requested,
            NumFeaturesToLoad = (uint)features.Length
        };
        Check(SL.Init(in preferences), "slInit");
        initialized = true;
        // This reference belongs to the sample; the wrapper retains its separate reference.
        Module = NativeLibrary.Load(path);
        Console.WriteLine($"Streamline runtime: {FileVersionInfo.GetVersionInfo(path).FileVersion ?? "Unknown"}");
    }

    public void QueryFeatures(AdapterInfo adapter)
    {
        foreach ((uint id, string name) in Features)
        {
            SLResult support = SL.IsFeatureSupported(id, in adapter);
            SLResult loadedResult = SL.IsFeatureLoaded(id, out Bool8 loaded);
            if (support != SLResult.Ok)
            {
                Unavailable[id] = support.ToString();
            }
            else if (loadedResult != SLResult.Ok || !loaded)
            {
                Unavailable[id] = $"Plugin initialization: {loadedResult}, loaded={loaded}";
            }

            Console.WriteLine($"{name}: {(Available(id) ? "Available" : Unavailable[id])}");
        }
        if (Available(SL.FeatureDLSSG) && !Available(SL.FeatureReflex))
        {
            Unavailable[SL.FeatureDLSSG] = "Reflex is required";
        }

        frameGenerationLoaded = Available(SL.FeatureDLSSG);
        if (frameGenerationLoaded)
        {
            DLSSGState state = SL.DLSSG.GetState(in viewport, null);
            MaximumGeneratedFrames = state.NumFramesToGenerateMax;
            MinimumFGDimension = state.MinWidthOrHeight;
            SetFrameGeneration(0);
        }
        if (Available(SL.FeaturePCL))
        {
            PCLOptions options = new();
            Check(SL.PCL.SetOptions(in options), "slPCLSetOptions");
            LatencyPingMessage = SL.PCL.GetState().StatsWindowMessage;
        }
        SetLowLatency();
    }

    private void SetLowLatency()
    {
        if (Available(SL.FeatureReflex))
        {
            ReflexOptions reflex = new()
            {
                Mode = ReflexMode.LowLatency,
                UseMarkersToOptimize = true
            };
            Check(SL.Reflex.SetOptions(in reflex), "slReflexSetOptions");
        }
    }

    public (int Width, int Height) Configure(RenderSettings settings, int width, int height)
    {
        uint w = (uint)width, h = (uint)height;
        switch (settings.Reconstruction)
        {
            case Reconstruction.DLSS:
                dlssOptions = new()
                {
                    Mode = settings.ReconstructionQuality,
                    OutputWidth = w,
                    OutputHeight = h,
                    ColorBuffersHDR = SLBoolean.True,
                    PreExposure = 1,
                    ExposureScale = 1
                };
                Check(SL.DLSS.SetOptions(in viewport, in dlssOptions), "slDLSSSetOptions");
                reconstruction = Reconstruction.DLSS;
                DLSSOptimalSettings optimal = SL.DLSS.GetOptimalSettings(in dlssOptions);
                return ((int)optimal.OptimalRenderWidth, (int)optimal.OptimalRenderHeight);
            case Reconstruction.RayReconstruction:
                rayOptions = RayOptions(settings, width, height, null);
                Check(SL.DLSSD.SetOptions(in viewport, in rayOptions), "slDLSSDSetOptions");
                reconstruction = Reconstruction.RayReconstruction;
                DLSSDOptimalSettings rrOptimal = SL.DLSSD.GetOptimalSettings(in rayOptions);
                return ((int)rrOptimal.OptimalRenderWidth, (int)rrOptimal.OptimalRenderHeight);
            default:
                return (width, height);
        }
    }

    public void Begin(uint frameIndex)
    {
        DrainMessages();
        Check(SL.GetNewFrameToken(out FrameToken token, &frameIndex), "slGetNewFrameToken");
        frame = token;
        if (Available(SL.FeatureReflex))
        {
            Check(SL.Reflex.Sleep(frame), "slReflexSleep");
        }

        Marker(PCLMarker.SimulationStart);
        Marker(PCLMarker.ControllerInputSample);
    }

    public void Marker(PCLMarker marker)
    {
        if (Available(SL.FeaturePCL))
        {
            Check(SL.PCL.SetMarker(marker, frame), $"slPCLSetMarker({marker})");
        }
    }

    public void SetConstants(Camera camera, RenderSettings settings, int width, int height, int outputWidth, int outputHeight, bool reset)
    {
        Constants constants = CameraConstants(camera, width, height, outputWidth, outputHeight, reset);
        Check(SL.SetConstants(in constants, frame, in viewport), "slSetConstants");
        if (settings.Reconstruction == Reconstruction.RayReconstruction)
        {
            rayOptions = RayOptions(settings, outputWidth, outputHeight, camera);
            Check(SL.DLSSD.SetOptions(in viewport, in rayOptions), "slDLSSDSetOptions(camera)");
        }
    }

    private static Constants CameraConstants(Camera camera, int width, int height, int outputWidth, int outputHeight, bool reset)
    {
        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 inverseViewProjection);
        Matrix4x4 clipToPrevious = inverseViewProjection * camera.PreviousViewProjection;
        Matrix4x4.Invert(clipToPrevious, out Matrix4x4 previousToClip);
        Vector3 forward = camera.Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        return new()
        {
            CameraViewToClip = Matrix(camera.Projection),
            ClipToCameraView = Matrix(inverseProjection),
            ClipToLensClip = Matrix(Matrix4x4.Identity),
            ClipToPrevClip = Matrix(clipToPrevious),
            PrevClipToClip = Matrix(previousToClip),
            JitterOffset = new(camera.Jitter.X, camera.Jitter.Y),
            MvecScale = new(1f / width, 1f / height),
            CameraPinholeOffset = new(0, 0),
            CameraPos = new(camera.Position.X, camera.Position.Y, camera.Position.Z),
            CameraFwd = new(forward.X, forward.Y, forward.Z),
            CameraUp = new(up.X, up.Y, up.Z),
            CameraRight = new(right.X, right.Y, right.Z),
            CameraNear = camera.Near,
            CameraFar = camera.Far,
            CameraFOV = Camera.FieldOfView,
            CameraAspectRatio = (float)outputWidth / outputHeight,
            DepthInverted = SLBoolean.False,
            CameraMotionIncluded = SLBoolean.True,
            MotionVectors3D = SLBoolean.False,
            Reset = reset ? SLBoolean.True : SLBoolean.False,
            OrthographicProjection = SLBoolean.False,
            MotionVectorsDilated = SLBoolean.False,
            MotionVectorsJittered = SLBoolean.False
        };
    }

    private static DLSSDOptions RayOptions(RenderSettings settings, int width, int height, Camera? camera)
    {
        Matrix4x4 view = camera?.View ?? Matrix4x4.Identity;
        Matrix4x4.Invert(view, out Matrix4x4 inverse);
        return new()
        {
            Mode = settings.ReconstructionQuality,
            OutputWidth = (uint)width,
            OutputHeight = (uint)height,
            ColorBuffersHDR = SLBoolean.True,
            PreExposure = 1,
            ExposureScale = 1,
            NormalRoughnessMode = DLSSDNormalRoughnessMode.Packed,
            WorldToCameraView = Matrix(view),
            CameraViewToWorld = Matrix(inverse)
        };
    }

    public void Tags(int frameSlot, nint command, ReadOnlySpan<(uint Type, ImageSlot Slot)> resources, ReadOnlySpan<GpuImage> images, bool present = false)
    {
        Span<ResourceTag> tags = stackalloc ResourceTag[resources.Length];
        for (int i = 0; i < resources.Length; i++)
        {
            (uint type, ImageSlot slot) = resources[i];
            GpuImage image = images[(int)slot];
            Resource* description = descriptions + frameSlot * (int)ImageSlot.Count + (int)slot;
            *description = image.Describe();
            tags[i] = new()
            {
                Resource = description,
                Type = type,
                Lifecycle = present ? ResourceLifecycle.ValidUntilPresent : ResourceLifecycle.OnlyValidNow,
                Extent = new()
                {
                    Width = (uint)image.Width,
                    Height = (uint)image.Height
                }
            };
            taggedTypes.Add(type);
        }
        Check(SL.SetTagForFrame(frame, in viewport, tags, (void*)command), "slSetTagForFrame");
    }

    public void Evaluate(uint feature, nint command)
    {
        ViewportHandle inputViewport = viewport;
        BaseStructure* input = (BaseStructure*)&inputViewport;
        Check(SL.EvaluateFeature(feature, frame, &input, 1, (void*)command), $"slEvaluateFeature({feature})");
        evaluatedFeature = feature;
    }

    public void SetFrameGeneration(uint frames)
    {
        if (!frameGenerationLoaded)
        {
            return;
        }

        DLSSGOptions options = new()
        {
            Mode = frames == 0 ? DLSSGMode.Off : DLSSGMode.On,
            NumFramesToGenerate = Math.Max(frames, 1),
            OnErrorCallback = &OnApiError,
            EnableUserInterfaceRecomposition = SLBoolean.True
        };
        Check(SL.DLSSG.SetOptions(in viewport, in options), "slDLSSGSetOptions");
        lastStateResult = SLResult.Ok;
        frameGenerationIssue = null;
    }

    public void LoadFrameGeneration(bool load)
    {
        if (!Available(SL.FeatureDLSSG) || frameGenerationLoaded == load)
        {
            return;
        }

        Check(SL.SetFeatureLoaded(SL.FeatureDLSSG, load), "slSetFeatureLoaded(DLSS-G)");
        frameGenerationLoaded = load;
    }

    // Call once after each successful Present on the presenting thread. The SDK
    // counter is consumed by GetState, so periodic statistics must not query it again.
    public uint? ReadPresentedFrameCount()
    {
        if (!frameGenerationLoaded)
        {
            return 1;
        }
        DLSSGState state = new();
        // Null options avoid the expensive optional VRAM estimate.
        SLResult result = SL.DLSSG.GetState(in viewport, ref state, null);
        if (result != SLResult.Ok)
        {
            if (lastStateResult != result)
            {
                Console.Error.WriteLine($"DLSS Frame Generation state query failed: {result}");
            }
            lastStateResult = result;
            return null;
        }
        lastStateResult = SLResult.Ok;
        DLSSGStatus? previousIssue = frameGenerationIssue;
        frameGenerationIssue = state.Status == DLSSGStatus.Ok ? null : state.Status;
        if (previousIssue != frameGenerationIssue)
        {
            Console.WriteLine($"DLSS Frame Generation: {state.Status}");
        }
        return state.NumFramesActuallyPresented;
    }

    // The caller must finish GPU work before releasing feature resources or tags.
    // Off stops the feature; slFreeResources separately releases its viewport.
    public void ReleaseReconstruction()
    {
        switch (reconstruction)
        {
            case Reconstruction.DLSS:
                dlssOptions.Mode = DLSSMode.Off;
                Check(SL.DLSS.SetOptions(in viewport, in dlssOptions), "slDLSSSetOptions(Off)");
                break;
            case Reconstruction.RayReconstruction:
                rayOptions.Mode = DLSSMode.Off;
                Check(SL.DLSSD.SetOptions(in viewport, in rayOptions), "slDLSSDSetOptions(Off)");
                break;
        }
        if (evaluatedFeature is uint feature)
        {
            Check(SL.FreeResources(feature, in viewport), $"slFreeResources({feature})");
            evaluatedFeature = null;
        }
        reconstruction = Reconstruction.Native;
    }

    public void ClearResourceTags()
    {
        if (frame.Handle != 0 && taggedTypes.Count > 0)
        {
            ResourceTag[] tags = taggedTypes.Select(type => new ResourceTag() { Type = type }).ToArray();
            Check(SL.SetTagForFrame(frame, in viewport, tags, null), "slSetTagForFrame(clear)");
            taggedTypes.Clear();
        }
    }

    private static Float4x4 Matrix(Matrix4x4 matrix) => Unsafe.BitCast<Matrix4x4, Float4x4>(matrix);

    private static void DrainMessages()
    {
        while (messages.TryDequeue(out string? message))
        {
            Console.WriteLine(message);
        }

        int error = Interlocked.Exchange(ref apiError, 0);
        if (error != 0)
        {
            throw new InvalidOperationException($"Asynchronous Streamline presentation error: 0x{error:X8}");
        }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void Log(LogType type, sbyte* message)
    {
        try
        {
            messages.Enqueue($"[Streamline/{type}] {Marshal.PtrToStringUTF8((nint)message)}");
        }
        catch { }
    }
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void OnApiError(APIError* error)
    {
        if (error != null)
        {
            Interlocked.Exchange(ref apiError, error->Hres != 0 ? error->Hres : error->VkRes);
        }
    }
    public void Dispose()
    {
        if (initialized)
        {
            SLResult result = SL.Shutdown();
            Check(result, "slShutdown");
            initialized = false;
            Console.WriteLine($"slShutdown: {result}");
        }
        if (Module != 0)
        {
            NativeLibrary.Free(Module);
            Module = 0;
        }
        foreach (nint pointer in allocations)
        {
            Marshal.FreeCoTaskMem(pointer);
        }

        allocations.Clear();
        NativeMemory.Free(descriptions);
    }
}
