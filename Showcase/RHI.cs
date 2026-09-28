using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Streamline.NET;

namespace Showcase;

internal abstract class RHI(Window window, UserInterface ui) : IDisposable
{
    private const float SunIrradiance = 8;
    private const float SkyRadiance = 0.65f;
    private const float ContactShadowRadiusScale = 0.012f;
    public bool RayQuerySupported
    {
        get; protected set;
    }
    public string RayQueryStatus { get; protected set; } = "Unavailable";
    public RenderCapabilities Capabilities => new(
        Streamline.Available(SL.FeatureDLSS),
        RayQuerySupported && Streamline.Available(SL.FeatureDLSSRR),
        Streamline.Available(SL.FeatureDLSSG) && Streamline.MaximumGeneratedFrames >= 1
            && Math.Min(Window.Width, Window.Height) >= Streamline.MinimumFGDimension);
    public Window Window { get; } = window;
    public UserInterface UI { get; } = ui;
    public StreamlineSession Streamline { get; } = new();
    public RenderSettings Settings { get; } = new();
    protected abstract RenderAPI API
    {
        get;
    }
    public string AdapterName { get; protected set; } = "Unknown";
    public int InputWidth
    {
        get; private set;
    }
    public int InputHeight
    {
        get; private set;
    }
    public double? PresentedFps => statistics.PresentedFps;
    protected Scene Scene = null!;
    protected Camera Camera { get; } = new();
    protected readonly GpuImage[][] Frames = new GpuImage[RenderLayout.FramesInFlight][];
    // Scratch data is consumed entirely on the graphics queue before the next
    // frame writes it. One shared allocation avoids multiplying it by frame slots.
    protected GpuImage LightingSamples = null!;
    protected int FrameSlot;
    protected FrameConstants Constants;
    protected abstract nint Command
    {
        get;
    }
    private RenderSettings? applied;
    private int outputWidth, outputHeight;
    private uint frameNumber;
    private bool reset = true;
    private bool recreateSwapChain;
    private bool ready;
    private bool disposed;
    private long previousTick = Stopwatch.GetTimestamp();
    private readonly FrameStatistics statistics = new();

    public void Initialize()
    {
        Streamline.Initialize(API);
        InitializeDevice();
        Console.WriteLine($"Hardware Ray Query: {RayQueryStatus}");
        if (!RayQuerySupported)
        {
            Streamline.Unavailable[SL.FeatureDLSSRR] = RayQueryStatus;
        }
        Window.LatencyPingMessage = Streamline.LatencyPingMessage;
        Scene = Scene.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "Scenes", "Sponza.gltf"));
        Camera.Reset(Scene);
        Settings.Reset(Capabilities);
        InitializeRenderer();
        ready = true;
        Window.BeforeWindowChange = SuspendFrameGeneration;
        ApplySettings();
        previousTick = Stopwatch.GetTimestamp();
    }

    private void ApplySettings()
    {
        if (Math.Min(Window.Width, Window.Height) < Streamline.MinimumFGDimension)
        {
            Settings.FrameGeneration = false;
        }
        bool outputChanged = outputWidth != Window.Width || outputHeight != Window.Height;
        bool swapChainChanged = applied is null || recreateSwapChain || outputChanged || applied.FrameGeneration != Settings.FrameGeneration;
        bool reconstructionChanged = applied is null || outputChanged || applied.Quality != Settings.Quality || applied.RayReconstruction != Settings.RayReconstruction;

        WaitIdle();
        if (swapChainChanged)
        {
            Streamline.SetFrameGeneration(0);
        }
        if (reconstructionChanged)
        {
            Streamline.ReleaseReconstruction();
        }
        Streamline.ClearResourceTags();
        if (swapChainChanged)
        {
            DestroySwapChain();
            Streamline.LoadFrameGeneration(Settings.FrameGeneration);
        }
        outputWidth = Window.Width;
        outputHeight = Window.Height;
        if (reconstructionChanged)
        {
            (InputWidth, InputHeight) = Streamline.Configure(Settings, outputWidth, outputHeight);
            if (InputWidth <= 0 || InputHeight <= 0)
            {
                throw new InvalidOperationException("The SDK returned an invalid input resolution.");
            }
        }
        if (swapChainChanged)
        {
            CreateSwapChain();
        }
        EnsureTargets();
        if (swapChainChanged)
        {
            Streamline.SetFrameGeneration(Settings.FrameGeneration ? 1u : 0);
        }
        // Discard presentation counts from before the configuration change.
        _ = Streamline.ReadPresentedFrameCount();
        applied = Settings with
        {
        };
        reset = true;
        recreateSwapChain = false;
        Scene.CommitHistory();
        statistics.Reset(Stopwatch.GetTimestamp());
    }

    private void EnsureTargets()
    {
        bool changed = false;
        for (int frame = 0; frame < Frames.Length; frame++)
        {
            Frames[frame] ??= new GpuImage[(int)ImageSlot.Count];
            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                (int width, int height) = RenderLayout.Size(slot, InputWidth, InputHeight, outputWidth, outputHeight);
                changed |= ResizeImage(ref Frames[frame][(int)slot], width, height, RenderLayout.Format(slot));
            }
        }
        changed |= ResizeImage(ref LightingSamples, RayQuerySupported ? InputWidth : 1, RayQuerySupported ? InputHeight : 1,
            ImageFormat.Rgba32, RenderLayout.LightingPaths);
        if (changed)
        {
            UpdateDescriptors();
        }
    }

    private bool ResizeImage(ref GpuImage image, int width, int height, ImageFormat format, int layers = 1)
    {
        if (image is not null && image.Width == width && image.Height == height && image.Format == format && image.Layers == layers)
        {
            return false;
        }
        image?.Dispose();
        image = null!;
        image = CreateImage(width, height, format, layers);
        return true;
    }

    private void SuspendFrameGeneration()
    {
        if (ready && !disposed)
        {
            Streamline.SetFrameGeneration(0);
            statistics.Reset(Stopwatch.GetTimestamp());
            recreateSwapChain = true;
        }
    }

    public void RenderFrame()
    {
        if (Window.Width == 0 || Window.Height == 0)
        {
            Window.Wait();
            Window.Pump();
            previousTick = Stopwatch.GetTimestamp();
            statistics.Reset(previousTick);
            reset = true;
            return;
        }
        Streamline.Begin(frameNumber);
        Window.Pump();
        if (Window.LatencyPing)
        {
            Streamline.Marker(PCLMarker.PCLatencyPing);
            Window.LatencyPing = false;
        }
        if (Window.Closed || Window.Width == 0 || Window.Height == 0)
        {
            statistics.Reset(Stopwatch.GetTimestamp());
            Streamline.Marker(PCLMarker.SimulationEnd);
            return;
        }
        long start = Stopwatch.GetTimestamp();
        float delta = (float)Stopwatch.GetElapsedTime(previousTick, start).TotalSeconds;
        previousTick = start;
        UI.Build(this, delta);
        if (recreateSwapChain || outputWidth != Window.Width || outputHeight != Window.Height || applied != Settings)
        {
            ApplySettings();
            // A modal window resize or settings change is not simulation time.
            delta = 0;
            previousTick = Stopwatch.GetTimestamp();
        }
        UpdateScene(delta);
        Streamline.SetConstants(Camera, Settings, InputWidth, InputHeight, outputWidth, outputHeight, reset);

        Streamline.Marker(PCLMarker.SimulationEnd);
        FrameSlot = (int)(frameNumber % RenderLayout.FramesInFlight);
        if (!BeginCommands())
        {
            recreateSwapChain = true;
            return;
        }
        Streamline.Marker(PCLMarker.RenderSubmitStart);
        RenderLighting();
        Reconstruct();
        PostProcess();
        DrawUI(ImGui.GetDrawData());
        Transition(Image(ImageSlot.UI), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Final), ImageUse.Storage);
        Dispatch(ComputePass.Composite, outputWidth, outputHeight, Constants);
        if (Settings.FrameGeneration)
        {
            Streamline.Tags(FrameSlot, Command,
                [(SL.BufferTypeDepth, ImageSlot.Depth), (SL.BufferTypeMotionVectors, ImageSlot.Motion), (SL.BufferTypeHUDLessColor, ImageSlot.Hudless), (SL.BufferTypeUIColorAndAlpha, ImageSlot.UI)], Frames[FrameSlot], true);
        }
        SubmitFrame();
        Streamline.Marker(PCLMarker.RenderSubmitEnd);
        Streamline.Marker(PCLMarker.PresentStart);
        bool presented = Present();
        if (!presented)
        {
            recreateSwapChain = true;
        }

        Streamline.Marker(PCLMarker.PresentEnd);
        FinishFrame();
        uint? presentedFrames = presented ? Streamline.ReadPresentedFrameCount() : null;
        if (Settings.FrameGeneration && Streamline.FrameGenerationFailed)
        {
            // Do not leave a failed FG mode enabled and silently pay its overhead.
            // The SDK error is recorded by the presentation-state query above.
            Settings.FrameGeneration = false;
            recreateSwapChain = true;
        }
        Camera.CommitHistory();
        Scene.CommitHistory();
        reset = false;
        frameNumber++;
        long timestamp = Stopwatch.GetTimestamp();
        if (!presented)
        {
            statistics.Reset(timestamp);
        }
        else
        {
            statistics.RecordFrame(timestamp, presentedFrames);
        }
    }

    private void UpdateScene(float delta)
    {
        Camera.Move(Window, delta);

        Scene.Update(delta);
        bool temporal = Settings.Reconstruction != Reconstruction.Native;
        Camera.Update(InputWidth, InputHeight, outputWidth, outputHeight, frameNumber, temporal, reset);
        Matrix4x4.Invert(Camera.JitteredViewProjection, out Matrix4x4 inverse);
        Vector3 center = (Scene.Minimum + Scene.Maximum) * 0.5f;
        // Aim daylight along the atrium opening. Cross-courtyard sunlight mostly
        // hits the upper walls, leaving the floor dependent on exposure lift.
        const float elevation = 60 * MathF.PI / 180;
        const float azimuth = 15 * MathF.PI / 180;
        Vector3 sun = new(MathF.Cos(elevation) * MathF.Cos(azimuth), MathF.Sin(elevation), MathF.Cos(elevation) * MathF.Sin(azimuth));
        const float solarAngularRadius = 0.2666f * MathF.PI / 180;
        Constants = new()
        {
            ViewProjection = Camera.JitteredViewProjection,
            CurrentViewProjection = Camera.ViewProjection,
            PreviousViewProjection = Camera.PreviousViewProjection,
            InverseViewProjection = inverse,
            Camera = new(Camera.Position, MathF.Tan(Camera.FieldOfView / 2)),
            Size = new(InputWidth, InputHeight, outputWidth, outputHeight),
            Sun = new(sun, solarAngularRadius),
            Scene = new(Scene.Objects.Length, frameNumber, Scene.RayEpsilon, Scene.Scale),
            Parameters = new(RayQuerySupported ? 1 : 0, 0, 0, 0),
            Jitter = new(Camera.Jitter, Settings.Reconstruction == Reconstruction.RayReconstruction ? 1 : 0,
                RenderLayout.TextureMipBias(InputWidth, outputWidth, temporal)),
            Center = new(center.X, Scene.GroundHeight, center.Z, 0),
            SunViewProjection = Scene.GetSunViewProjection(sun),
            Lighting = new(SunIrradiance, SkyRadiance, 0, Scene.Scale * ContactShadowRadiusScale),
            Exposure = new(1, delta, reset ? 1 : 0, 0),
            EnvironmentMinimum = new(Scene.Minimum, 0),
            EnvironmentMaximum = new(Scene.Maximum, 0)
        };
    }

    private void RenderLighting()
    {
        if (RayQuerySupported)
        {
            UpdateRayTracingScene();
        }
        else
        {
            DrawShadow();
        }
        Transition(Image(ImageSlot.Shadow), ImageUse.ShaderRead);
        DrawScene();
        foreach (ImageSlot slot in RenderLayout.GeometryOutputs)
        {
            Transition(Image(slot), ImageUse.ShaderRead);
        }

        foreach (ImageSlot slot in RenderLayout.LightingOutputs)
        {
            Transition(Image(slot), ImageUse.Storage);
        }

        if (RayQuerySupported)
        {
            Transition(LightingSamples, ImageUse.Storage);
            Dispatch(ComputePass.TraceLighting, InputWidth, InputHeight, Constants, RenderLayout.LightingPaths);
        }
        Transition(LightingSamples, ImageUse.ShaderRead);
        Dispatch(ComputePass.Lighting, InputWidth, InputHeight, Constants);
        foreach (ImageSlot slot in RenderLayout.LightingOutputs)
        {
            Transition(Image(slot), ImageUse.ShaderRead);
        }
    }

    private void Reconstruct()
    {
        uint? reconstruction = Feature(Settings.Reconstruction);
        if (reconstruction is uint feature)
        {
            Transition(Image(ImageSlot.Reconstructed), ImageUse.Storage);
            ReadOnlySpan<(uint, ImageSlot)> tags =
            [
                (SL.BufferTypeScalingInputColor, ImageSlot.Scene), (SL.BufferTypeScalingOutputColor, ImageSlot.Reconstructed),
                (SL.BufferTypeDepth, ImageSlot.Depth), (SL.BufferTypeMotionVectors, ImageSlot.Motion),
                (SL.BufferTypeAlbedo, ImageSlot.Diffuse), (SL.BufferTypeSpecularAlbedo, ImageSlot.Specular),
                (SL.BufferTypeNormalRoughness, ImageSlot.Normal), (SL.BufferTypeSpecularHitDistance, ImageSlot.HitDistance)
            ];
            Streamline.Tags(FrameSlot, Command, feature == SL.FeatureDLSSRR ? tags : tags[..4], Frames[FrameSlot]);
            Streamline.Evaluate(feature, Command);
            Transition(Image(ImageSlot.Reconstructed), ImageUse.ShaderRead);
        }
    }

    private void PostProcess()
    {
        FrameConstants post = Constants;
        post.Parameters.Z = Settings.Reconstruction != Reconstruction.Native ? 1 : 0;
        Transition(Image(ImageSlot.Luminance), ImageUse.Storage);
        // PrepareLuminance uses one 8x8 group per tile, rather than per 8x8 tiles.
        Dispatch(ComputePass.PrepareLuminance, Image(ImageSlot.Luminance).Width * 8, Image(ImageSlot.Luminance).Height * 8, post);
        Transition(Image(ImageSlot.Luminance), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.FilteredLuminance), ImageUse.Storage);
        Dispatch(ComputePass.FilterLuminance, Image(ImageSlot.FilteredLuminance).Width, Image(ImageSlot.FilteredLuminance).Height, post);
        Transition(Image(ImageSlot.FilteredLuminance), ImageUse.ShaderRead);
        // Meter reconstructed HDR when available, before tone mapping and UI. Each
        // frame reads the preceding submitted frame's result, not its slot's old value.
        int previousFrame = (FrameSlot + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
        Transition(Frames[previousFrame][(int)ImageSlot.Exposure], ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Exposure), ImageUse.Storage);
        Dispatch(ComputePass.MeterExposure, 1, 1, post);
        Transition(Image(ImageSlot.Exposure), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.DisplayInput), ImageUse.Storage);
        Dispatch(ComputePass.ToneMap, Image(ImageSlot.DisplayInput).Width, Image(ImageSlot.DisplayInput).Height, post);
        Transition(Image(ImageSlot.DisplayInput), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Hudless), ImageUse.Storage);
        // Keep native, un-reconstructed RT samples intact for the RR comparison.
        Dispatch(Settings.Reconstruction == Reconstruction.Native && !RayQuerySupported ? ComputePass.NativeResolve : ComputePass.CopyDisplay, outputWidth, outputHeight, post);
        Transition(Image(ImageSlot.Hudless), ImageUse.ShaderRead);
    }

    protected GpuImage Image(ImageSlot slot) => Frames[FrameSlot][(int)slot];
    private static uint? Feature(Reconstruction method) => method switch
    {
        Reconstruction.DLSS => SL.FeatureDLSS,
        Reconstruction.RayReconstruction => SL.FeatureDLSSRR,
        _ => null
    };
    private void ReleaseTargets()
    {
        LightingSamples?.Dispose();
        LightingSamples = null!;
        foreach (GpuImage[]? frame in Frames)
        {
            if (frame is not null)
            {
                foreach (GpuImage image in frame)
                {
                    image?.Dispose();
                }
            }
        }

        Array.Clear(Frames);
    }
    protected abstract void InitializeDevice();
    protected abstract void InitializeRenderer();
    protected abstract void CreateSwapChain();
    protected abstract void DestroySwapChain();
    protected abstract GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1);
    protected abstract void UpdateDescriptors();
    protected abstract bool BeginCommands();
    protected abstract void UpdateRayTracingScene();
    protected abstract void DrawShadow();
    protected abstract void DrawScene();
    protected abstract void DrawUI(ImDrawDataPtr data);
    protected abstract void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1);
    protected abstract void Transition(GpuImage image, ImageUse use);
    protected abstract void SubmitFrame();
    protected abstract bool Present();
    protected abstract void FinishFrame();
    protected abstract void WaitIdle();
    protected abstract void DisposeDevice();
    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        Window.BeforeWindowChange = null;
        try
        {
            WaitIdle();
            if (ready)
            {
                Streamline.SetFrameGeneration(0);
                Streamline.ReleaseReconstruction();
                Streamline.ClearResourceTags();
            }
        }
        finally
        {
            // Keep device, proxy swap chain and callbacks alive through shutdown.
            try
            {
                Streamline.Dispose();
            }
            finally
            {
                ReleaseTargets();
                DisposeDevice();
            }
        }
    }
}
