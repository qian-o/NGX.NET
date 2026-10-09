using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;
using Silk.NET.Windowing;

namespace Showcase;

internal abstract class RHI(IWindow window, ImGuiHandler ui) : IDisposable
{
    private bool disposed;

    protected IWindow Window { get; } = window;

    protected ImGuiHandler UI { get; } = ui;

    protected RenderResources Resources { get; private set; } = null!;

    protected PassArgs Frame { get; private set; }

    public NGXSession NGX { get; } = new();

    public abstract nint Command { get; }

    public string AdapterName { get; protected set; } = "Unknown";

    public bool RayQuerySupported { get; protected set; }

    public string RayQueryStatus { get; protected set; } = "Unavailable";

    public RenderCapabilities Capabilities => new(NGX.Available(NGXFeature.SuperSampling), RayQuerySupported && NGX.Available(NGXFeature.RayReconstruction), NGX.Available(NGXFeature.FrameGeneration));

    public void Initialize()
    {
        InitializeDevice();
        Console.WriteLine($"Hardware Ray Query: {RayQueryStatus}");

        if (!RayQuerySupported)
        {
            NGX.Unavailable[NGXFeature.RayReconstruction] = RayQueryStatus;
        }
    }

    public void InitializeRenderer(RenderResources resources)
    {
        Resources = resources;
        InitializeRendererCore();
    }

    public void BeginFrame(in PassArgs args)
    {
        Frame = args;
        BeginCommands();
    }

    public abstract void CreateSwapChain();

    public abstract void DestroySwapChain();

    public abstract GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1);

    public abstract void UpdateDescriptors();

    public abstract void UpdateFontTexture();

    public abstract void UpdateRayTracingScene();

    public abstract void DrawShadow();

    public abstract void DrawScene();

    public abstract void DrawUI(ImDrawDataPtr data);

    public abstract void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1);

    public abstract void Transition(GpuImage image, ImageUse use);

    public abstract void SubmitFrame();

    public abstract bool PresentImage(GpuImage image);

    public abstract void WaitRenderedFrame(int slot);

    public abstract void WaitPresentation();

    public abstract void WaitIdle();

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;

        WaitIdle();
        NGX.Dispose();
        DisposeDevice();
    }

    protected abstract void InitializeDevice();

    protected abstract void InitializeRendererCore();

    protected abstract void BeginCommands();

    protected abstract void DisposeDevice();
}
