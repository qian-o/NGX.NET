using Showcase.Models;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private readonly object queueSync = new();
    private CommandPool presentPool;
    private CommandBuffer presentCommand;
    private Fence presentFence;
    private Semaphore presentAcquire;
    private bool presentationPending;

    private void InitializePresentation()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamily
        };

        Check(api.CreateCommandPool(device, &poolInfo, null, out presentPool), "vkCreateCommandPool(present)");
        CommandBufferAllocateInfo commandInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = presentPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        CommandBuffer command = default;
        Check(api.AllocateCommandBuffers(device, &commandInfo, &command), "vkAllocateCommandBuffers(present)");
        presentCommand = command;
        FenceCreateInfo fenceInfo = new() { SType = StructureType.FenceCreateInfo };
        Check(api.CreateFence(device, &fenceInfo, null, out presentFence), "vkCreateFence(present)");
        SemaphoreCreateInfo semaphoreInfo = new() { SType = StructureType.SemaphoreCreateInfo };
        Check(api.CreateSemaphore(device, &semaphoreInfo, null, out presentAcquire), "vkCreateSemaphore(acquire)");
    }

    protected override void WaitRenderedFrame(int slot)
    {
        Fence fence = slots[slot].Fence;
        Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(render)");
        Semaphore complete = slots[slot].RenderComplete;
        PipelineStageFlags stage = PipelineStageFlags.AllCommandsBit;
        SubmitInfo wait = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &complete,
            PWaitDstStageMask = &stage
        };

        lock (queueSync)
        {
            Check(api.QueueSubmit(presentQueue, 1, &wait, default), "vkQueueSubmit(render dependency)");
        }
    }

    protected override bool PresentImage(GpuImage image)
    {
        WaitPresentation();
        uint index = 0;
        Result acquire = swapChainApi.AcquireNextImage(device, swapChain, ulong.MaxValue, presentAcquire, default, &index);

        if (acquire == Result.ErrorOutOfDateKhr)
        {
            return false;
        }

        if (acquire != Result.SuboptimalKhr)
        {
            Check(acquire, "vkAcquireNextImageKHR");
        }

        Check(api.ResetCommandPool(device, presentPool, 0), "vkResetCommandPool(present)");
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(presentCommand, &begin), "vkBeginCommandBuffer(present)");
        Barrier(backBuffers[index], backLayouts[index], ImageLayout.TransferDstOptimal, Range(ImageFormat.Rgba8), presentCommand);
        ImageBlit blit = new()
        {
            SrcSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            DstSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1)
        };

        blit.SrcOffsets[1] = new(image.Width, image.Height, 1);
        blit.DstOffsets[1] = new(image.Width, image.Height, 1);
        api.CmdBlitImage(
            presentCommand,
            ((VkTexture)image).Texture,
            ImageLayout.TransferSrcOptimal,
            backBuffers[index],
            ImageLayout.TransferDstOptimal,
            1,
            &blit,
            Filter.Nearest);
        Barrier(backBuffers[index], ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr, Range(ImageFormat.Rgba8), presentCommand);
        backLayouts[index] = ImageLayout.PresentSrcKhr;
        Check(api.EndCommandBuffer(presentCommand), "vkEndCommandBuffer(present)");
        CommandBuffer command = presentCommand;
        Semaphore acquireSemaphore = presentAcquire, complete = presentSemaphores[index];
        Fence fence = presentFence;
        Check(api.ResetFences(device, 1, &fence), "vkResetFences(present)");
        PipelineStageFlags stage = PipelineStageFlags.TransferBit;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &acquireSemaphore,
            PWaitDstStageMask = &stage,
            CommandBufferCount = 1,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &complete
        };

        SwapchainKHR swap = swapChain;
        PresentInfoKHR present = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &complete,
            SwapchainCount = 1,
            PSwapchains = &swap,
            PImageIndices = &index
        };

        Result result;
        lock (queueSync)
        {
            Check(api.QueueSubmit(presentQueue, 1, &submit, fence), "vkQueueSubmit(present)");
            presentationPending = true;
            result = swapChainApi.QueuePresent(presentQueue, &present);
        }

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
        {
            return false;
        }

        Check(result, "vkQueuePresentKHR");

        return true;
    }

    protected override void WaitPresentation()
    {
        if (presentationPending)
        {
            Fence fence = presentFence;
            Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(present copy)");
            presentationPending = false;
        }
    }

    private void DisposePresentation()
    {
        if (presentAcquire.Handle != 0)
        {
            api.DestroySemaphore(device, presentAcquire, null);
        }

        if (presentFence.Handle != 0)
        {
            api.DestroyFence(device, presentFence, null);
        }

        if (presentPool.Handle != 0)
        {
            api.DestroyCommandPool(device, presentPool, null);
        }
    }
}
