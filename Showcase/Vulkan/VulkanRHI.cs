using Showcase.Handlers;
using Silk.NET.Vulkan;
using Silk.NET.Windowing;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI(IWindow window, ImGuiHandler ui) : RHI(window, ui)
{
    public override nint Command => commandBuffer.Handle;

    private static void Check(Result result, string operation)
    {
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"{operation}: {result}");
        }
    }
}
