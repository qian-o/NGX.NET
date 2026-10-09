using System.Numerics;
using Hexa.NET.ImGui;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Showcase.Passes;

namespace Showcase;

internal class Renderer : IDisposable
{
    private const float SunIrradiance = 8;
    private const float SkyRadiance = 0.65f;
    private const float ContactShadowRadiusScale = 0.012f;

    private readonly RHI context;
    private readonly RenderResources resources;
    private readonly FramePresenter presenter;
    private readonly FrameStatistics statistics = new();
    private readonly Pass[] passes;
    private readonly FrameGenerationPass frameGeneration;

    // Simulation controls do not participate in GPU resource configuration.
    public bool AnimationPaused;

    public RenderSettings Settings = new();

    private RenderSettings? applied;
    private int width;
    private int height;
    private uint frameIndex;
    private float elapsed;
    private bool reset = true;
    private bool recreateSwapChain = true;
    private bool disposed;

    public Renderer(RHI context, CameraHandler camera, Scene scene)
    {
        this.context = context;
        camera.Reset(scene);
        resources = new(context, scene);
        presenter = new(context.WaitRenderedFrame, context.PresentImage, context.WaitPresentation);
        frameGeneration = new(context, resources);
        passes = [new GeometryPass(context, resources), new LightingPass(context, resources), new ReconstructionPass(context, resources), new ExposurePass(context, resources), new TonemapPass(context, resources), new ImGuiPass(context, resources), frameGeneration];
        Settings.Reset(Capabilities);

        context.InitializeRenderer(resources);
    }

    public RenderCapabilities Capabilities => context.Capabilities;

    public string AdapterName => context.AdapterName;

    public double? PresentedFps => statistics.PresentedFps;

    public float Update(float delta)
    {
        if (width <= 0 || height <= 0)
        {
            return 0;
        }

        if (ApplySettings())
        {
            delta = 0;
        }

        resources.Scene.Update(AnimationPaused ? 0 : delta);
        elapsed += delta;

        return delta;
    }

    public void Resize(int width, int height)
    {
        if (this.width == width && this.height == height)
        {
            return;
        }

        this.width = width;
        this.height = height;
        recreateSwapChain = true;
    }

    public void Suspend()
    {
        presenter.Drain();
        _ = presenter.ReadPresentedCount();
        ResetHistory();
    }

    public void UpdateFont()
    {
        if (applied is null)
        {
            Configure();
        }

        presenter.Drain();
        context.WaitIdle();
        context.UpdateFontTexture();
    }

    public void Render(CameraHandler camera, ImDrawDataPtr drawData)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        ApplySettings();

        int slot = (int)(frameIndex % RenderLayout.FramesInFlight);
        presenter.WaitSlot(slot);
        camera.Update(resources.InputWidth, resources.InputHeight, width, height, frameIndex, Settings.Reconstruction is not Reconstruction.Native, reset);
        PassArgs args = CreateArgs(slot, camera, drawData);
        context.BeginFrame(args);

        foreach (Pass pass in passes)
        {
            pass.Record(args);
        }

        GpuImage color = resources.Image(slot, ImageSlot.Final);
        context.Transition(color, ImageUse.CopySource);
        context.SubmitFrame();
        presenter.Enqueue(slot, color, frameGeneration.Generated, TimeSpan.FromSeconds(elapsed));
        statistics.RecordFrame(presenter.ReadPresentedCount());
        camera.CommitHistory();
        resources.Scene.CommitHistory();
        frameIndex++;
        elapsed = 0;
        reset = false;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        presenter.Dispose();
        context.WaitIdle();
        context.NGX.ReleaseFrameGeneration();
        context.NGX.ReleaseReconstruction();
        resources.Dispose();
    }

    private void ResetHistory()
    {
        reset = true;
        elapsed = 0;
        statistics.Reset();
    }

    private void Configure()
    {
        bool outputChanged = resources.OutputWidth != width || resources.OutputHeight != height;
        bool reconstructionChanged = applied is null || outputChanged || applied.Value.Quality != Settings.Quality || applied.Value.RayReconstruction != Settings.RayReconstruction;
        bool generationChanged = applied is null || reconstructionChanged || applied.Value.FrameGeneration != Settings.FrameGeneration;
        presenter.Drain();
        context.WaitIdle();

        if (reconstructionChanged)
        {
            context.NGX.ReleaseReconstruction();
        }

        if (generationChanged)
        {
            context.NGX.ReleaseFrameGeneration();
        }

        if (recreateSwapChain)
        {
            context.DestroySwapChain();
        }

        if (reconstructionChanged)
        {
            (int inputWidth, int inputHeight) = context.NGX.Configure(Settings, width, height);

            if (inputWidth <= 0 || inputHeight <= 0)
            {
                throw new InvalidOperationException("The SDK returned an invalid input resolution.");
            }

            resources.Resize(inputWidth, inputHeight, width, height);
        }

        if (recreateSwapChain)
        {
            context.CreateSwapChain();
        }

        _ = presenter.ReadPresentedCount();
        applied = Settings;
        recreateSwapChain = false;
        resources.Scene.CommitHistory();
        ResetHistory();
    }

    private bool ApplySettings()
    {
        recreateSwapChain |= presenter.NeedsRecreation;

        if (!recreateSwapChain && applied is RenderSettings previous && previous.Quality == Settings.Quality && previous.RayReconstruction == Settings.RayReconstruction && previous.FrameGeneration == Settings.FrameGeneration)
        {
            return false;
        }

        Configure();

        return true;
    }

    private PassArgs CreateArgs(int slot, CameraHandler camera, ImDrawDataPtr drawData)
    {
        Scene scene = resources.Scene;
        bool temporal = Settings.Reconstruction is not Reconstruction.Native;
        Matrix4x4.Invert(camera.JitteredViewProjection, out Matrix4x4 inverse);
        const float Elevation = 50 * MathF.PI / 180;
        const float Azimuth = 65 * MathF.PI / 180;
        Vector3 sun = new(MathF.Cos(Elevation) * MathF.Cos(Azimuth), MathF.Sin(Elevation), MathF.Cos(Elevation) * MathF.Sin(Azimuth));
        const float SolarAngularRadius = 0.2666f * MathF.PI / 180;
        FrameConstants constants = new()
        {
            ViewProjection = camera.JitteredViewProjection,
            CurrentViewProjection = camera.ViewProjection,
            PreviousViewProjection = camera.PreviousViewProjection,
            InverseViewProjection = inverse,
            Camera = new(camera.Position, MathF.Tan(CameraHandler.FieldOfView / 2)),
            Size = new(resources.InputWidth, resources.InputHeight, width, height),
            Sun = new(sun, SolarAngularRadius),
            Scene = new(scene.Objects.Length, frameIndex, scene.RayEpsilon, scene.Scale),
            Jitter = new(camera.Jitter, Settings.Reconstruction is Reconstruction.RayReconstruction ? 1 : 0, RenderLayout.TextureMipBias(resources.InputWidth, width, temporal)),
            SunViewProjection = scene.GetSunViewProjection(sun),
            Lighting = new(SunIrradiance, SkyRadiance, 0, scene.Scale * ContactShadowRadiusScale),
            Exposure = new(0, elapsed, reset ? 1 : 0, 0),
            EnvironmentMinimum = new(scene.Minimum, 0),
            EnvironmentMaximum = new(scene.Maximum, 0),
            PreviousCamera = new(camera.PreviousPosition, 0)
        };

        return new()
        {
            Slot = slot,
            Constants = constants,
            Camera = camera,
            Reconstruction = Settings.Reconstruction,
            FrameGeneration = Settings.FrameGeneration,
            Reset = reset,
            Delta = elapsed,
            DrawData = drawData
        };
    }
}
