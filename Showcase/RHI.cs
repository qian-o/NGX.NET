using System.Diagnostics;
using System.Numerics;
using ImGuiNET;
using Streamline.NET;

namespace Showcase;

internal abstract class RHI(Window window, UserInterface ui) : IDisposable
{
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
    public double CpuMilliseconds
    {
        get; private set;
    }
    public double RenderFps
    {
        get; private set;
    }
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
    protected abstract nint Queue
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
    private long statsTick = Stopwatch.GetTimestamp();
    private uint statsFrames;
    private double cpuTotal;

    public void Initialize()
    {
        Streamline.Initialize(BackendName == "Vulkan");
        InitializeDevice();
        Window.LatencyPingMessage = Streamline.LatencyPingMessage;
        Scene = Scene.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "Scenes", "Sponza.gltf"));
        Camera.Reset(Scene);
        if (!Streamline.Available(SL.FeatureDLSS))
        {
            Settings.Reconstruction = Reconstruction.Native;
        }

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
            Settings.GeneratedFrames = 0;
        }

        Streamline.LoadFrameGeneration(Settings.GeneratedFrames > 0);
        (InputWidth, InputHeight) = Streamline.Configure(Settings, outputWidth, outputHeight, Queue);
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
                bool output = slot is ImageSlot.Reconstructed or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final || slot == ImageSlot.DisplayInput && Settings.Reconstruction != Reconstruction.NIS;
                Frames[frame][(int)slot] = CreateImage(output ? outputWidth : InputWidth, output ? outputHeight : InputHeight, RenderLayout.Format(slot));
            }
        }
        UpdateDescriptors();
        Streamline.SetFrameGeneration(Settings.GeneratedFrames);
        applied = Settings with
        {
        };
        reset = true;
        rebuild = false;
        Scene.CommitHistory();
        Console.WriteLine($"{BackendName}: {InputWidth}x{InputHeight} -> {outputWidth}x{outputHeight}, {Settings.Reconstruction}, FG={Settings.GeneratedFrames + 1}x");
    }

    private void SuspendFrameGeneration()
    {
        if (ready && !disposed)
        {
            Streamline.SetFrameGeneration(0);
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
        }
        else if (applied != Settings)
        {
            Streamline.UpdateOptions(Settings);
            applied = Settings with
            {
            };
        }
        if (UI.ResetCamera)
        {
            Camera.Reset(Scene);
            reset = true;
            UI.ResetCamera = false;
        }
        if (!Settings.FixedCamera)
        {
            Camera.Move(Window, delta);
        }

        Scene.Update(delta, Settings.PauseAnimation);
        bool temporal = Settings.Reconstruction is Reconstruction.DLSS or Reconstruction.DLAA or Reconstruction.RayReconstruction or Reconstruction.DirectSR;
        Camera.Update(InputWidth, InputHeight, outputWidth, outputHeight, frameNumber, temporal, reset);
        Matrix4x4.Invert(Camera.JitteredViewProjection, out Matrix4x4 inverse);
        Vector3 center = (Scene.Minimum + Scene.Maximum) * 0.5f;
        Constants = new()
        {
            ViewProjection = Camera.JitteredViewProjection,
            CurrentViewProjection = Camera.ViewProjection,
            PreviousViewProjection = Camera.PreviousViewProjection,
            InverseViewProjection = inverse,
            Camera = new(Camera.Position, 1),
            Size = new(InputWidth, InputHeight, outputWidth, outputHeight),
            Sun = new(Vector3.Normalize(new(0.4f, 0.8f, 0.25f)), 1),
            Scene = new(Scene.Objects.Length, frameNumber, Scene.RayEpsilon, Scene.Scale),
            Parameters = new(Settings.RayTracing ? 1 : 0, Settings.Exposure, 0, Settings.Reconstruction == Reconstruction.Native ? 1 : Settings.Reconstruction == Reconstruction.NIS ? 2 : 0),
            Jitter = new(Camera.Jitter, Settings.Reconstruction == Reconstruction.RayReconstruction ? 1 : 0, 0),
            Center = new(center.X, Scene.GroundHeight, center.Z, 0)
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
        bool hdrReconstruction = reconstruction.HasValue && Settings.Reconstruction != Reconstruction.NIS;
        if (hdrReconstruction)
        {
            Transition(Image(ImageSlot.Reconstructed), ImageUse.Storage);
            if (Settings.Reconstruction == Reconstruction.DirectSR)
            {
                PrepareDirectSRDepth();
            }

            List<(uint, ImageSlot, GpuImage)> tags =
            [
                Tag(SL.BufferTypeScalingInputColor, ImageSlot.Scene), Tag(SL.BufferTypeScalingOutputColor, ImageSlot.Reconstructed),
                Tag(SL.BufferTypeDepth, Settings.Reconstruction == Reconstruction.DirectSR ? ImageSlot.DepthCopy : ImageSlot.Depth), Tag(SL.BufferTypeMotionVectors, ImageSlot.Motion)
            ];
            if (Settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                tags.AddRange([Tag(SL.BufferTypeAlbedo, ImageSlot.Diffuse), Tag(SL.BufferTypeSpecularAlbedo, ImageSlot.Specular), Tag(SL.BufferTypeNormalRoughness, ImageSlot.Normal), Tag(SL.BufferTypeSpecularHitDistance, ImageSlot.HitDistance)]);
            }
            Streamline.Tags(FrameSlot, Command, tags.ToArray());
            if (Settings.Reconstruction == Reconstruction.DirectSR)
            {
                SubmitBeforeDirectSR();
            }

            Streamline.Evaluate(reconstruction!.Value, Command);
            if (Settings.Reconstruction == Reconstruction.DirectSR)
            {
                ResumeAfterDirectSR();
            }

            Transition(Image(ImageSlot.Reconstructed), ImageUse.ShaderRead);
        }
        FrameConstants post = Constants;
        post.Parameters.Z = hdrReconstruction ? 1 : 0;
        Transition(Image(ImageSlot.DisplayInput), ImageUse.Storage);
        Dispatch(ComputePass.ToneMap, Image(ImageSlot.DisplayInput).Width, Image(ImageSlot.DisplayInput).Height, post);
        Transition(Image(ImageSlot.DisplayInput), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Hudless), ImageUse.Storage);
        if (Settings.Reconstruction == Reconstruction.NIS)
        {
            Streamline.Tags(FrameSlot, Command, [Tag(SL.BufferTypeScalingInputColor, ImageSlot.DisplayInput), Tag(SL.BufferTypeScalingOutputColor, ImageSlot.Hudless)]);
            Streamline.Evaluate(SL.FeatureNIS, Command);
        }
        else
        {
            Dispatch(Settings.Reconstruction == Reconstruction.Native ? ComputePass.NativeResolve : ComputePass.CopyDisplay, outputWidth, outputHeight, post);
        }

        if (Settings.DeepDVC)
        {
            StorageBarrier(Image(ImageSlot.Hudless));
            Streamline.Tags(FrameSlot, Command, [Tag(SL.BufferTypeScalingOutputColor, ImageSlot.Hudless)]);
            Streamline.Evaluate(SL.FeatureDeepDVC, Command);
        }
        Transition(Image(ImageSlot.Hudless), ImageUse.ShaderRead);
        DrawUI(ImGui.GetDrawData());
        Transition(Image(ImageSlot.UI), ImageUse.ShaderRead);
        Transition(Image(ImageSlot.Final), ImageUse.Storage);
        Dispatch(ComputePass.Composite, outputWidth, outputHeight, Constants);
        if (Settings.GeneratedFrames > 0)
        {
            Streamline.Tags(FrameSlot, Command,
                [Tag(SL.BufferTypeDepth, ImageSlot.Depth), Tag(SL.BufferTypeMotionVectors, ImageSlot.Motion), Tag(SL.BufferTypeHUDLessColor, ImageSlot.Hudless), Tag(SL.BufferTypeUIColorAndAlpha, ImageSlot.UI)], true);
        }
        SubmitFrame();
        Streamline.Marker(PCLMarker.RenderSubmitEnd);
        Streamline.Marker(PCLMarker.PresentStart);
        if (!Present())
        {
            rebuild = true;
        }

        Streamline.Marker(PCLMarker.PresentEnd);
        FinishFrame();
        Camera.CommitHistory();
        Scene.CommitHistory();
        reset = false;
        frameNumber++;
        statsFrames++;
        cpuTotal += Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        double interval = Stopwatch.GetElapsedTime(statsTick).TotalSeconds;
        if (interval >= 0.5)
        {
            RenderFps = statsFrames / interval;
            CpuMilliseconds = cpuTotal / statsFrames;
            statsFrames = 0;
            cpuTotal = 0;
            statsTick = Stopwatch.GetTimestamp();
            Streamline.ReadStatistics();
        }
    }

    private bool ResourcesChanged() => applied is null || applied.Reconstruction != Settings.Reconstruction || applied.Quality != Settings.Quality || applied.Scale != Settings.Scale || applied.GeneratedFrames != Settings.GeneratedFrames || applied.DirectSRVariant != Settings.DirectSRVariant || applied.RayTracing != Settings.RayTracing;
    protected GpuImage Image(ImageSlot slot) => Frames[FrameSlot][(int)slot];
    private (uint, ImageSlot, GpuImage) Tag(uint type, ImageSlot slot) => (type, slot, Image(slot));
    public static uint? Feature(Reconstruction method) => method switch
    {
        Reconstruction.DLSS or Reconstruction.DLAA => SL.FeatureDLSS,
        Reconstruction.RayReconstruction => SL.FeatureDLSSRR,
        Reconstruction.NIS => SL.FeatureNIS,
        Reconstruction.DirectSR => SL.FeatureDirectSR,
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
    protected abstract void DrawScene();
    protected abstract void DrawUI(ImDrawDataPtr data);
    protected abstract void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants);
    protected abstract void Transition(GpuImage image, ImageUse use);
    protected abstract void StorageBarrier(GpuImage image);
    protected abstract void SubmitFrame();
    protected abstract bool Present();
    protected abstract void FinishFrame();
    protected virtual void PrepareDirectSRDepth() => throw new NotSupportedException("DirectSR requires DirectX 12.");
    protected virtual void SubmitBeforeDirectSR() => throw new NotSupportedException("DirectSR requires DirectX 12.");
    protected virtual void ResumeAfterDirectSR() => throw new NotSupportedException("DirectSR requires DirectX 12.");
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
