using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;
using Silk.NET.Windowing;

namespace Showcase;

internal abstract class RHI(IWindow window, ImGuiHandler ui) : DisposableObject
{
    private bool hasInitializationStarted;

    public NGXSession NGX { get; } = new();

    public abstract nint Command { get; }

    public string AdapterName { get; protected set; } = "Unknown";

    public bool RayQuerySupported { get; protected set; }

    public string RayQueryStatus { get; protected set; } = "Unavailable";

    public RenderCapabilities Capabilities => new(NGX.IsAvailable(NGXFeature.SuperSampling), RayQuerySupported && NGX.IsAvailable(NGXFeature.RayReconstruction), NGX.IsAvailable(NGXFeature.FrameGeneration));

    protected IWindow Window { get; } = window;

    protected ImGuiHandler UI { get; } = ui;

    protected RenderResources Resources { get; private set; } = null!;

    protected FrameArgs Frame { get; private set; }

    public void Initialize()
    {
        hasInitializationStarted = true;
        InitializeDevice();
        Console.WriteLine($"Hardware Ray Query: {RayQueryStatus}");

        if (!RayQuerySupported)
        {
            throw new NotSupportedException($"The path tracer requires hardware ray queries: {RayQueryStatus}.");
        }
    }

    public void InitializeRenderer(RenderResources resources)
    {
        Resources = resources;
        InitializeRendererImpl();
    }

    public void BeginFrame(in FrameArgs args)
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

    public abstract void DrawUI(ImDrawDataPtr data);

    public abstract void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1);

    public abstract void Transition(GpuImage image, ImageUse use);

    public abstract void SubmitFrame();

    public abstract bool PresentImage(GpuImage image);

    public abstract void WaitRenderedFrame(int slot);

    public abstract void WaitPresentation();

    public abstract void WaitIdle();

    protected override void Destroy()
    {
        try
        {
            if (hasInitializationStarted)
            {
                WaitIdle();
            }
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine($"GPU shutdown wait failed: {exception.Message}");
        }
        catch (COMException exception)
        {
            Console.Error.WriteLine($"GPU shutdown wait failed: {exception.Message}");
        }
        finally
        {
            try
            {
                NGX?.Dispose();
            }
            finally
            {
                DestroyDevice();
            }
        }
    }

    protected abstract void InitializeDevice();

    protected abstract void InitializeRendererImpl();

    protected abstract void BeginCommands();

    protected abstract void DestroyDevice();
}
