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
    public abstract string BackendName
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
    public double CpuMilliseconds => statistics.CpuMilliseconds;
    public double? RenderFps => statistics.RenderFps;
    public double? PresentedFps => statistics.PresentedFps;
    public double? GpuMilliseconds
    {
        get; protected set;
    }
    protected Scene Scene = null!;
    protected Camera Camera { get; } = new();
    protected readonly GpuImage[][] Frames = new GpuImage[RenderLayout.FramesInFlight][];
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
    private bool rebuild;
    private bool ready;
    private bool disposed;
    private long previousTick = Stopwatch.GetTimestamp();
    private readonly FrameStatistics statistics = new();

    public void Initialize()
    {
        Streamline.Initialize(BackendName == "Vulkan");
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
        Resize();
        previousTick = Stopwatch.GetTimestamp();
    }

    public void Resize()
    {
        if (Window.Width <= 0 || Window.Height <= 0)
        {
            return;
        }

        WaitIdle();
        Streamline.SetFrameGeneration(0);
        Streamline.ReleaseFeatureResources();
        ReleaseTargets();
        DestroySwapChain();
        outputWidth = Window.Width;
        outputHeight = Window.Height;
        if (Math.Min(outputWidth, outputHeight) < Streamline.MinimumFGDimension)
        {
            Settings.FrameGeneration = false;
        }

        Streamline.LoadFrameGeneration(Settings.FrameGeneration);
        (InputWidth, InputHeight) = Streamline.Configure(Settings, outputWidth, outputHeight);
        if (InputWidth <= 0 || InputHeight <= 0)
        {
            throw new InvalidOperationException("The SDK returned an invalid input resolution.");
        }

        CreateSwapChain();
        for (int frame = 0; frame < Frames.Length; frame++)
        {
            Frames[frame] = new GpuImage[(int)ImageSlot.Count];
            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                bool output = slot is ImageSlot.Reconstructed or ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final;
                int width = slot == ImageSlot.Exposure ? 1 : slot == ImageSlot.Shadow ? RenderLayout.ShadowMapSize : output ? outputWidth : InputWidth;
                int height = slot == ImageSlot.Exposure ? 1 : slot == ImageSlot.Shadow ? RenderLayout.ShadowMapSize : output ? outputHeight : InputHeight;
                if (slot is ImageSlot.Luminance or ImageSlot.FilteredLuminance)
                {
                    width = (outputWidth + RenderLayout.LuminanceTileSize - 1) / RenderLayout.LuminanceTileSize;
                    height = (outputHeight + RenderLayout.LuminanceTileSize - 1) / RenderLayout.LuminanceTileSize;
                }
                Frames[frame][(int)slot] = CreateImage(width, height, RenderLayout.Format(slot));
            }
        }
        UpdateDescriptors();
        Streamline.SetFrameGeneration(Settings.FrameGeneration ? 1u : 0);
        // Discard counts from initialization or the old swap chain. Mode changes
        // begin a fresh measurement interval after resource recreation has finished.
        _ = Streamline.ReadPresentedFrameCount();
        applied = Settings with
        {
        };
        reset = true;
        rebuild = false;
        Scene.CommitHistory();
        Console.WriteLine($"{BackendName}: {InputWidth}x{InputHeight} -> {outputWidth}x{outputHeight}, {Settings.Reconstruction}, FG={(Settings.FrameGeneration ? "On" : "Off")}");
        statistics.Reset(Stopwatch.GetTimestamp());
    }

    private void SuspendFrameGeneration()
    {
        if (ready && !disposed)
        {
            Streamline.SetFrameGeneration(0);
            statistics.Reset(Stopwatch.GetTimestamp());
            rebuild = true;
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
        if (rebuild || outputWidth != Window.Width || outputHeight != Window.Height || ResourcesChanged())
        {
            Resize();
            // A modal window resize or pipeline rebuild is not simulation time.
            delta = 0;
            previousTick = Stopwatch.GetTimestamp();
            start = previousTick;
        }
        Camera.Move(Window, delta);

        Scene.Update(delta, false);
        bool temporal = Settings.Reconstruction != Reconstruction.Native;
        Camera.Update(InputWidth, InputHeight, outputWidth, outputHeight, frameNumber, temporal, reset);
        Matrix4x4.Invert(Camera.JitteredViewProjection, out Matrix4x4 inverse);
        Vector3 center = (Scene.Minimum + Scene.Maximum) * 0.5f;
        const float elevation = 50 * MathF.PI / 180;
        const float azimuth = 65 * MathF.PI / 180;
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
        Streamline.SetConstants(Camera, Settings, InputWidth, InputHeight, outputWidth, outputHeight, reset);
        Streamline.Marker(PCLMarker.SimulationEnd);
        FrameSlot = (int)(frameNumber % RenderLayout.FramesInFlight);
        if (!BeginCommands())
        {
            rebuild = true;
            return;
        }
        Streamline.Marker(PCLMarker.RenderSubmitStart);
        if (RayQuerySupported)
        {
            UpdateRayTracingScene();
        }
        if (!RayQuerySupported)
        {
            DrawShadow();
        }
        Transition(Image(ImageSlot.Shadow), ImageUse.ShaderRead);
        DrawScene();
        foreach (ImageSlot slot in new[] { ImageSlot.Albedo, ImageSlot.Normal, ImageSlot.Emissive, ImageSlot.Depth })
        {
            Transition(Image(slot), ImageUse.ShaderRead);
        }

        foreach (ImageSlot slot in new[] { ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Motion, ImageSlot.Diffuse })
        {
            Transition(Image(slot), ImageUse.Storage);
        }

        Dispatch(ComputePass.Lighting, InputWidth, InputHeight, Constants);
        foreach (ImageSlot slot in new[] { ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Motion, ImageSlot.Diffuse })
        {
            Transition(Image(slot), ImageUse.ShaderRead);
        }

        uint? reconstruction = Feature(Settings.Reconstruction);
        bool hdrReconstruction = reconstruction.HasValue;
        if (hdrReconstruction)
        {
            Transition(Image(ImageSlot.Reconstructed), ImageUse.Storage);
            List<(uint, ImageSlot, GpuImage)> tags =
            [
                Tag(SL.BufferTypeScalingInputColor, ImageSlot.Scene), Tag(SL.BufferTypeScalingOutputColor, ImageSlot.Reconstructed),
                Tag(SL.BufferTypeDepth, ImageSlot.Depth), Tag(SL.BufferTypeMotionVectors, ImageSlot.Motion)
            ];
            if (Settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                tags.AddRange([Tag(SL.BufferTypeAlbedo, ImageSlot.Diffuse), Tag(SL.BufferTypeSpecularAlbedo, ImageSlot.Specular), Tag(SL.BufferTypeNormalRoughness, ImageSlot.Normal), Tag(SL.BufferTypeSpecularHitDistance, ImageSlot.HitDistance)]);
            }
            Streamline.Tags(FrameSlot, Command, tags.ToArray());
            Streamline.Evaluate(reconstruction!.Value, Command);
            Transition(Image(ImageSlot.Reconstructed), ImageUse.ShaderRead);
        }
        FrameConstants post = Constants;
        post.Parameters.Z = hdrReconstruction ? 1 : 0;
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
        DrawUI(ImGui.GetDrawData());
        Transition(Image(ImageSlot.UI), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Final), ImageUse.Storage);
        Dispatch(ComputePass.Composite, outputWidth, outputHeight, Constants);
        if (Settings.FrameGeneration)
        {
            Streamline.Tags(FrameSlot, Command,
                [Tag(SL.BufferTypeDepth, ImageSlot.Depth), Tag(SL.BufferTypeMotionVectors, ImageSlot.Motion), Tag(SL.BufferTypeHUDLessColor, ImageSlot.Hudless), Tag(SL.BufferTypeUIColorAndAlpha, ImageSlot.UI)], true);
        }
        SubmitFrame();
        Streamline.Marker(PCLMarker.RenderSubmitEnd);
        Streamline.Marker(PCLMarker.PresentStart);
        bool presented = Present();
        if (!presented)
        {
            rebuild = true;
        }

        Streamline.Marker(PCLMarker.PresentEnd);
        FinishFrame();
        uint? presentedFrames = presented ? Streamline.ReadPresentedFrameCount() : null;
        if (Settings.FrameGeneration && Streamline.FrameGenerationFailed)
        {
            // Do not leave a failed FG mode enabled and silently pay its overhead.
            // The SDK error is recorded by the presentation-state query above.
            Settings.FrameGeneration = false;
            rebuild = true;
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
        else if (statistics.RecordFrame(timestamp, presentedFrames, Stopwatch.GetElapsedTime(start, timestamp).TotalMilliseconds))
        {
            Streamline.ReadLatencyStatistics();
        }
    }

    private bool ResourcesChanged() => applied is null || applied.Quality != Settings.Quality ||
        applied.FrameGeneration != Settings.FrameGeneration || applied.RayReconstruction != Settings.RayReconstruction;
    protected GpuImage Image(ImageSlot slot) => Frames[FrameSlot][(int)slot];
    private (uint, ImageSlot, GpuImage) Tag(uint type, ImageSlot slot) => (type, slot, Image(slot));
    public static uint? Feature(Reconstruction method) => method switch
    {
        Reconstruction.DLSS => SL.FeatureDLSS,
        Reconstruction.RayReconstruction => SL.FeatureDLSSRR,
        _ => null
    };
    private void ReleaseTargets()
    {
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
    protected abstract GpuImage CreateImage(int width, int height, ImageFormat format);
    protected abstract void UpdateDescriptors();
    protected abstract bool BeginCommands();
    protected abstract void UpdateRayTracingScene();
    protected abstract void DrawShadow();
    protected abstract void DrawScene();
    protected abstract void DrawUI(ImDrawDataPtr data);
    protected abstract void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants);
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
                Streamline.ReleaseFeatureResources(reload: false);
            }
        }
        finally
        {
            // Keep device, proxy swap chain and callbacks alive through shutdown.
            Streamline.Dispose();
            ReleaseTargets();
            DisposeDevice();
        }
    }
}
