using System.Numerics;
using Hexa.NET.ImGui;
using Showcase.DirectX12;
using Showcase.Handlers;
using Showcase.Vulkan;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace Showcase;

internal static class App
{
    public static void Run()
    {
        Console.WriteLine("NGX.NET Showcase\n1. DirectX 12\n2. Vulkan");
        string? choice;

        do
        {
            Console.Write("Select backend (1/2): ");
            choice = Console.ReadLine();

            if (choice is null)
            {
                return;
            }
        }
        while (choice is not ("1" or "2"));

        bool vulkan = choice == "2";
        using IWindow window = Window.Create(WindowOptions.Default with
        {
            Size = new(1280, 720),
            API = vulkan ? GraphicsAPI.DefaultVulkan : GraphicsAPI.None,
            Title = $"NGX.NET Showcase - {(vulkan ? "Vulkan" : "DirectX 12")}",
            ShouldSwapAutomatically = false,
            VSync = false,
            UpdatesPerSecond = 0,
            FramesPerSecond = 0
        });
        window.Initialize();
        window.Center();

        using ImGuiHandler imGui = new();
        Vector2 initialSize = (Vector2)window.Size;
        Vector2 initialFramebuffer = (Vector2)window.FramebufferSize;

        if (initialSize.X > 0 && initialSize.Y > 0 && initialFramebuffer.X > 0 && initialFramebuffer.Y > 0)
        {
            imGui.UpdateFont(initialFramebuffer / initialSize);
        }

        using IInputContext input = window.CreateInput();
        using InputHandler inputHandler = new(window, input);
        using RHI context = vulkan ? new VulkanRHI(window, imGui) : new DirectX12RHI(window, imGui);
        context.Initialize();
        CameraHandler camera = new();
        using Renderer renderer = new(context, camera);
        renderer.Resize(window.FramebufferSize.X, window.FramebufferSize.Y);
        bool active = false;
        bool frameReady = false;

        window.Update += delta =>
        {
            inputHandler.Update();
            Vector2 size = (Vector2)window.FramebufferSize;
            Vector2 logicalSize = (Vector2)window.Size;

            if (window.WindowState == WindowState.Minimized || size.X <= 0 || size.Y <= 0 || logicalSize.X <= 0 || logicalSize.Y <= 0)
            {
                active = false;
                frameReady = false;
                window.IsEventDriven = true;

                return;
            }

            float elapsed = active ? (float)delta : 0;
            window.IsEventDriven = false;

            if (!active)
            {
                renderer.Suspend();
            }

            active = true;
            Vector2 dpiScale = size / logicalSize;

            if (imGui.UpdateFont(dpiScale))
            {
                renderer.UpdateFont();
            }

            imGui.Update((float)delta, size, dpiScale, renderer);
            elapsed = renderer.Update(elapsed);
            camera.Move(inputHandler, elapsed);
            frameReady = true;
        };

        window.Render += _ =>
        {
            if (frameReady)
            {
                renderer.Render(camera, ImGui.GetDrawData());
                frameReady = false;
            }
        };

        window.FramebufferResize += size => renderer.Resize(size.X, size.Y);

        // The parameterless Run extension resets the native window on return.
        // Keep it alive until the presenter, GPU resources and input are disposed.
        window.Run(() =>
        {
            window.DoEvents();

            if (!window.IsClosing)
            {
                window.DoUpdate();
            }

            if (!window.IsClosing)
            {
                window.DoRender();
            }
        });
    }
}
