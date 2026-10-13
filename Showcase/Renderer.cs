using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal class Renderer : DisposableObject
{
    private const float SunIrradiance = 8.0f;
    private const uint SamplesPerPixel = 2;
    private const uint MaxPathDepth = 5;
    // Display-only compensation on top of the metered exposure.
    private const float ExposureCompensationEv = 0.0f;

    private readonly RHI context;
    private readonly RenderResources resources;
    private readonly FramePresenter presenter;
    private readonly FrameStatistics statistics = new();

    // Simulation controls do not participate in GPU resource configuration.
    public bool IsAnimationPaused = true;

    public RenderSettings Settings = new();

    private RenderSettings? applied;
    private int width;
    private int height;
    private uint frameIndex;
    private uint filmSamples;
    private bool isExposureInitialized;
    private bool isAtmosphereInitialized;
    private Vector3 atmosphereSun;
    private float atmosphereAltitude;
    private Matrix4x4 aerialViewProjection;
    private float elapsedSeconds;
    private bool needsReset = true;
    private bool needsSwapChainRecreation = true;

    public Renderer(RHI context, CameraHandler camera, Scene scene)
    {
        this.context = context;
        camera.Reset(scene);
        resources = new(context, scene);
        presenter = new(context.WaitRenderedFrame, context.PresentImage, context.WaitPresentation);
        Settings.Reset(Capabilities);

        context.InitializeRenderer(resources);
    }

    public uint AccumulatedSamples => filmSamples;

    public RenderCapabilities Capabilities => context.Capabilities;

    public string AdapterName => context.AdapterName;

    public double? PresentedFps => statistics.PresentedFps;

    public float Update(float deltaSeconds)
    {
        if (width <= 0 || height <= 0)
        {
            return 0.0f;
        }

        if (ApplySettings())
        {
            deltaSeconds = 0.0f;
        }

        resources.Scene.Update(IsAnimationPaused ? 0.0f : deltaSeconds);
        elapsedSeconds += deltaSeconds;

        return deltaSeconds;
    }

    public void Resize(int width, int height)
    {
        if (this.width == width && this.height == height)
        {
            return;
        }

        this.width = width;
        this.height = height;
        needsSwapChainRecreation = true;
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
        camera.Update(resources.InputWidth, resources.InputHeight, width, height, frameIndex, Settings.Reconstruction is not Reconstruction.Native, needsReset);

        if (needsReset || camera.ViewProjection != camera.PreviousViewProjection || resources.Scene.HasMotion || Settings.Reconstruction is not Reconstruction.Native
            || filmSamples > uint.MaxValue - SamplesPerPixel)
        {
            filmSamples = 0;
        }

        FrameArgs args = CreateArgs(slot, camera, drawData);
        context.BeginFrame(args);

        // Aerial perspective samples local scene visibility as well as the atmosphere, so its rays need the current frame slot's TLAS first.
        context.UpdateRayTracingScene();
        RecordAtmosphere(in args);
        RecordPaths(in args);
        RecordReconstruction(in args);
        RecordTonemap(in args);
        RecordUI(in args);
        GpuImage? generated = GenerateFrame(in args);

        GpuImage color = resources.Image(slot, ImageSlot.Final);
        context.Transition(color, ImageUse.CopySource);
        context.SubmitFrame();
        presenter.Enqueue(slot, color, generated, TimeSpan.FromSeconds(elapsedSeconds));
        statistics.RecordFrame(presenter.ReadPresentedCount());
        camera.CommitHistory();
        resources.Scene.CommitHistory();
        filmSamples = Settings.Reconstruction is Reconstruction.Native ? filmSamples + SamplesPerPixel : 0;
        frameIndex++;
        elapsedSeconds = 0.0f;
        needsReset = false;
        isExposureInitialized = true;
    }

    protected override void Destroy()
    {
        presenter?.Dispose();

        if (context is not null)
        {
            try
            {
                context.WaitIdle();
            }
            catch (InvalidOperationException exception)
            {
                Console.Error.WriteLine($"Renderer shutdown wait failed: {exception.Message}");
            }
            catch (COMException exception)
            {
                Console.Error.WriteLine($"Renderer shutdown wait failed: {exception.Message}");
            }

            context.NGX.ReleaseFrameGeneration();
            context.NGX.ReleaseReconstruction();
        }

        resources?.Dispose();
    }

    private void RecordAtmosphere(in FrameArgs args)
    {
        FrameConstants constants = args.Constants;

        if (!isAtmosphereInitialized)
        {
            Build(ComputePass.AtmosphereTransmittance, AtmosphereImage.Transmittance);
            Build(ComputePass.AtmosphereMultipleScattering, AtmosphereImage.MultipleScattering);
        }

        Vector3 sun = new(constants.Sun.X, constants.Sun.Y, constants.Sun.Z);
        bool skyChanged = !isAtmosphereInitialized || sun != atmosphereSun || constants.Atmosphere.Z != atmosphereAltitude;
        if (skyChanged)
        {
            Build(ComputePass.AtmosphereGroundIrradiance, AtmosphereImage.GroundIrradiance);
            Build(ComputePass.AtmosphereSky, AtmosphereImage.SkyView);
            GpuImage distribution = resources.AtmosphereTexture(AtmosphereImage.SkyDistribution);
            context.Transition(distribution, ImageUse.Storage);
            // The distribution pass uses one 256-thread group for its prefix sums.
            context.Dispatch(ComputePass.AtmosphereDistribution, 8, 8, constants);
            context.Transition(distribution, ImageUse.ShaderRead);
        }

        if (skyChanged || aerialViewProjection != args.Camera.ViewProjection || resources.Scene.HasMotion)
        {
            GpuImage scattering = resources.AtmosphereTexture(AtmosphereImage.AerialScattering);
            GpuImage transmittance = resources.AtmosphereTexture(AtmosphereImage.AerialTransmittance);
            context.Transition(scattering, ImageUse.Storage);
            context.Transition(transmittance, ImageUse.Storage);
            // One invocation accumulates all sixteen depth slices for a column.
            context.Dispatch(ComputePass.AtmosphereAerial, scattering.Width, scattering.Height / 16, constants);
            context.Transition(scattering, ImageUse.ShaderRead);
            context.Transition(transmittance, ImageUse.ShaderRead);
        }

        atmosphereSun = sun;
        atmosphereAltitude = constants.Atmosphere.Z;
        aerialViewProjection = args.Camera.ViewProjection;
        isAtmosphereInitialized = true;

        void Build(ComputePass pass, AtmosphereImage slot)
        {
            GpuImage image = resources.AtmosphereTexture(slot);
            context.Transition(image, ImageUse.Storage);
            context.Dispatch(pass, image.Width, image.Height, constants);
            context.Transition(image, ImageUse.ShaderRead);
        }
    }

    private void RecordPaths(in FrameArgs args)
    {
        foreach (ImageSlot slot in RenderLayout.PathOutputs)
        {
            context.Transition(resources.Image(args.Slot, slot), ImageUse.Storage);
        }

        context.Transition(resources.Film, ImageUse.Storage);
        context.Dispatch(ComputePass.PathTrace, resources.InputWidth, resources.InputHeight, args.Constants);

        foreach (ImageSlot slot in RenderLayout.PathOutputs)
        {
            context.Transition(resources.Image(args.Slot, slot), ImageUse.ShaderRead);
        }
    }

    private void RecordReconstruction(in FrameArgs args)
    {
        if (args.Reconstruction is Reconstruction.Native)
        {
            return;
        }

        GpuImage output = resources.Image(args.Slot, ImageSlot.Reconstructed);
        context.Transition(output, ImageUse.Storage);
        context.NGX.Evaluate(args.Slot, context.Command, resources.Frames[args.Slot], args.Camera, args.Reset, args.DeltaSeconds);
        context.Transition(output, ImageUse.ShaderRead);
    }

    private void RecordTonemap(in FrameArgs args)
    {
        FrameConstants constants = args.Constants;
        constants.Parameters.Z = args.Reconstruction is not Reconstruction.Native ? 1 : 0;
        context.Transition(resources.Exposure, ImageUse.Storage);
        context.Dispatch(ComputePass.AutoExposure, 8, 8, constants);
        // Orders exposure writes before ToneMap's typed UAV load, on both APIs.
        context.Transition(resources.Exposure, ImageUse.Storage);
        GpuImage display = resources.Image(args.Slot, ImageSlot.DisplayInput);
        GpuImage hudless = resources.Image(args.Slot, ImageSlot.Hudless);
        context.Transition(display, ImageUse.Storage);
        context.Dispatch(ComputePass.ToneMap, display.Width, display.Height, constants);
        context.Transition(display, ImageUse.ShaderRead);
        context.Transition(hudless, ImageUse.Storage);

        context.Dispatch(ComputePass.CopyDisplay, resources.OutputWidth, resources.OutputHeight, constants);
        context.Transition(hudless, ImageUse.ShaderRead);
    }

    private void RecordUI(in FrameArgs args)
    {
        context.DrawUI(args.DrawData);
        context.Transition(resources.Image(args.Slot, ImageSlot.UI), ImageUse.ShaderRead);
        context.Transition(resources.Image(args.Slot, ImageSlot.Final), ImageUse.Storage);
        context.Dispatch(ComputePass.Composite, resources.OutputWidth, resources.OutputHeight, args.Constants);
    }

    private GpuImage? GenerateFrame(in FrameArgs args)
    {
        if (!args.FrameGeneration)
        {
            return null;
        }

        GpuImage output = resources.GeneratedFrames[args.Slot];
        context.Transition(resources.Image(args.Slot, ImageSlot.Final), ImageUse.ShaderRead);
        context.Transition(output, ImageUse.Storage);
        bool generated = context.NGX.Generate(args.Slot, context.Command, resources.Frames[args.Slot], output, args.Camera, args.Reset);
        context.Transition(output, ImageUse.CopySource);

        return generated ? output : null;
    }

    private void ResetHistory()
    {
        needsReset = true;
        filmSamples = 0;
        elapsedSeconds = 0.0f;
        statistics.Reset();
    }

    private void Configure()
    {
        bool outputChanged = resources.OutputWidth != width || resources.OutputHeight != height;
        bool reconstructionChanged = applied is null || outputChanged || applied.Value.Quality != Settings.Quality || applied.Value.IsRayReconstructionEnabled != Settings.IsRayReconstructionEnabled;
        bool generationChanged = applied is null || reconstructionChanged || applied.Value.IsFrameGenerationEnabled != Settings.IsFrameGenerationEnabled;
        presenter.Drain();
        context.WaitIdle();

        if (generationChanged)
        {
            context.NGX.ReleaseFrameGeneration();
        }

        if (reconstructionChanged)
        {
            context.NGX.ReleaseReconstruction();
        }

        if (needsSwapChainRecreation)
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

        if (needsSwapChainRecreation)
        {
            context.CreateSwapChain();
        }

        _ = presenter.ReadPresentedCount();
        applied = Settings;
        needsSwapChainRecreation = false;
        resources.Scene.CommitHistory();
        ResetHistory();
    }

    private bool ApplySettings()
    {
        needsSwapChainRecreation |= presenter.NeedsRecreation;

        if (!needsSwapChainRecreation && applied is RenderSettings previous && previous.Quality == Settings.Quality && previous.IsRayReconstructionEnabled == Settings.IsRayReconstructionEnabled && previous.IsFrameGenerationEnabled == Settings.IsFrameGenerationEnabled)
        {
            if (previous.DaylightProgress != Settings.DaylightProgress)
            {
                // Native film averages one fixed lighting state. RR/SR/FG keep useTemporalReconstruction history during a sun sweep, like other animation. NGX InReset is for complete scene changes, not each slider step: NVIDIA/DLSS include/nvsdk_ngx_helpers_dlssd_d3d.h.
                filmSamples = 0;
                applied = Settings;
            }

            return false;
        }

        Configure();

        return true;
    }

    private FrameArgs CreateArgs(int slot, CameraHandler camera, ImDrawDataPtr drawData)
    {
        Scene scene = resources.Scene;
        bool useTemporalReconstruction = Settings.Reconstruction is not Reconstruction.Native;
        Matrix4x4.Invert(camera.JitteredViewProjection, out Matrix4x4 inverse);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 atmosphereInverse);
        Vector3 sun = Settings.SunDirection;
        const float SolarAngularRadius = 0.2666f * MathF.PI / 180.0f;
        float projectedArea = MathF.PI * scene.Scale * scene.Scale * 0.25f;
        // glTF positions are meters; atmosphere integration is in kilometers.
        const float WorldToKilometers = 0.001f;
        // Earth-centered float32 math uses the same two-meter precision floor as the atmosphere shader; the local scene geometry is unaffected.
        float altitude = Math.Clamp((camera.Position.Y - scene.GroundHeight) * WorldToKilometers, 0.002f, 99.9f);
        float verticalExtent = MathF.Tan(CameraHandler.FieldOfView * 0.5f);
        float horizontalExtent = verticalExtent * width / height;
        // Camera far is a view-plane distance; corner rays travel farther.
        float aerialRange = camera.Far * MathF.Sqrt(1.0f + (verticalExtent * verticalExtent) + (horizontalExtent * horizontalExtent)) * WorldToKilometers;
        FrameConstants constants = new()
        {
            ViewProjection = camera.JitteredViewProjection,
            CurrentViewProjection = camera.ViewProjection,
            PreviousViewProjection = camera.PreviousViewProjection,
            InverseViewProjection = inverse,
            Camera = new(camera.Position, 0.0f),
            Size = new(resources.InputWidth, resources.InputHeight, width, height),
            Sun = new(sun, SolarAngularRadius),
            Scene = new(scene.AreaLights.Length, camera.Near, camera.Far, scene.Scale),
            Lighting = new(SunIrradiance, 0.0f, ExposureCompensationEv, useTemporalReconstruction ? 1.0f : 0.0f),
            // Sun/sky powers and selection probabilities are computed from the atmosphere on the GPU, in the same frame as the updated daylight.
            LightSelection = new(0.0f, 0.0f, scene.AreaLightPower / projectedArea, 0.0f),
            SamplesPerPixel = SamplesPerPixel,
            MaxDepth = MaxPathDepth,
            SampleOffset = useTemporalReconstruction ? frameIndex * SamplesPerPixel : filmSamples,
            FilmSamples = filmSamples,
            Exposure = new(elapsedSeconds, isExposureInitialized ? 0.0f : 1.0f, 0.0f, 0.0f),
            Atmosphere = new(scene.GroundHeight, WorldToKilometers, altitude, aerialRange),
            InverseAtmosphereViewProjection = atmosphereInverse
        };

        return new()
        {
            Slot = slot,
            Constants = constants,
            Camera = camera,
            Reconstruction = Settings.Reconstruction,
            FrameGeneration = Settings.IsFrameGenerationEnabled,
            Reset = needsReset,
            DeltaSeconds = elapsedSeconds,
            DrawData = drawData
        };
    }
}
