using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private SwapchainKHR swapChain;
    private Image[] backBuffers = [];
    private ImageLayout[] backLayouts = [];
    private Semaphore[] presentSemaphores = [];
    private Extent2D swapChainExtent;

    public override void CreateSwapChain()
    {
        Check(surfaceApi.GetPhysicalDeviceSurfaceCapabilities(physical, surface, out SurfaceCapabilitiesKHR capabilities), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        Extent2D extent = capabilities.CurrentExtent;

        if (extent.Width == uint.MaxValue)
        {
            Vector2D<int> framebufferSize = Window.FramebufferSize;
            extent = new(Math.Clamp((uint)Math.Max(0, framebufferSize.X), capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width), Math.Clamp((uint)Math.Max(0, framebufferSize.Y), capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height));
        }

        // A surface can become minimized between the renderer's size check and
        // this query. Presentation requests recreation when no chain is ready.
        if (extent.Width == 0 || extent.Height == 0)
        {
            return;
        }

        uint count = 0;
        Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, null), "vkGetPhysicalDeviceSurfaceFormatsKHR(count)");
        SurfaceFormatKHR[] formats = new SurfaceFormatKHR[count];

        fixed (SurfaceFormatKHR* pointer = formats)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfaceFormatsKHR");
        }
        SurfaceFormatKHR selected = formats.FirstOrDefault(x => x.Format == Format.R8G8B8A8Unorm && x.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);

        if (formats.Length == 1 && formats[0].Format == Format.Undefined && formats[0].ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr)
        {
            selected = new(Format.R8G8B8A8Unorm, formats[0].ColorSpace);
        }

        if (selected.Format == Format.Undefined)
        {
            selected = formats.FirstOrDefault(x => x.Format == Format.B8G8R8A8Unorm && x.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);
        }

        if (selected.Format == Format.Undefined)
        {
            throw new NotSupportedException("The surface must support an SDR UNORM format.");
        }

        if ((capabilities.SupportedUsageFlags & ImageUsageFlags.TransferDstBit) == 0)
        {
            throw new NotSupportedException("The swap chain does not support transfer destinations.");
        }

        Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, null), "vkGetPhysicalDeviceSurfacePresentModesKHR(count)");
        PresentModeKHR[] modes = new PresentModeKHR[count];

        fixed (PresentModeKHR* pointer = modes)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfacePresentModesKHR");
        }
        uint imageCount = Math.Max(capabilities.MinImageCount, RenderLayout.FramesInFlight);

        if (capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
        }

        SwapchainCreateInfoKHR create = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = imageCount,
            ImageFormat = selected.Format,
            ImageColorSpace = selected.ColorSpace,
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.TransferDstBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = SelectCompositeAlpha(capabilities.SupportedCompositeAlpha),
            PresentMode = modes.Contains(PresentModeKHR.ImmediateKhr) ? PresentModeKHR.ImmediateKhr : modes.Contains(PresentModeKHR.MailboxKhr) ? PresentModeKHR.MailboxKhr : PresentModeKHR.FifoKhr,
            Clipped = true
        };

        Check(swapChainApi.CreateSwapchain(device, &create, null, out SwapchainKHR createdSwapChain), "vkCreateSwapchainKHR");
        swapChain = createdSwapChain;
        swapChainExtent = extent;
        Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, null), "vkGetSwapchainImagesKHR(count)");
        backBuffers = new Image[count];
        backLayouts = new ImageLayout[count];
        presentSemaphores = new Semaphore[count];
        fixed (Image* pointer = backBuffers)
        {
            Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, pointer), "vkGetSwapchainImagesKHR");
        }
        SemaphoreCreateInfo semaphore = new() { SType = StructureType.SemaphoreCreateInfo };

        for (int i = 0; i < presentSemaphores.Length; i++)
        {
            Check(api.CreateSemaphore(device, &semaphore, null, out Semaphore createdSemaphore), "vkCreateSemaphore(present)");
            presentSemaphores[i] = createdSemaphore;
        }
    }

    public override void DestroySwapChain()
    {
        if (device.Handle == 0)
        {
            return;
        }

        foreach (Semaphore semaphore in presentSemaphores)
        {
            api.DestroySemaphore(device, semaphore, null);
        }

        presentSemaphores = [];
        backBuffers = [];
        backLayouts = [];
        swapChainExtent = default;

        if (swapChain.Handle != 0)
        {
            swapChainApi.DestroySwapchain(device, swapChain, null);
            swapChain = default;
        }
    }

    private static CompositeAlphaFlagsKHR SelectCompositeAlpha(CompositeAlphaFlagsKHR supported)
    {
        ReadOnlySpan<CompositeAlphaFlagsKHR> modes =
        [
            CompositeAlphaFlagsKHR.OpaqueBitKhr,
            CompositeAlphaFlagsKHR.PreMultipliedBitKhr,
            CompositeAlphaFlagsKHR.PostMultipliedBitKhr,
            CompositeAlphaFlagsKHR.InheritBitKhr
        ];

        foreach (CompositeAlphaFlagsKHR mode in modes)
        {
            if ((supported & mode) != 0)
            {
                return mode;
            }
        }

        throw new NotSupportedException("The surface does not expose a supported composite alpha mode.");
    }
}
