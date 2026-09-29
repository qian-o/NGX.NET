using Showcase.Models;
using Vortice.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private readonly object queueSync = new();
    private readonly object latencySync = new();
    private nint vulkanModule;
    private bool lowLatency;
    private VkSemaphore sleepSemaphore;
    private VkCommandPool presentPool;
    private VkCommandBuffer presentCommand;
    private VkFence presentFence;
    private VkSemaphore presentAcquire;

    protected override bool LowLatencyAvailable => lowLatency;

    private void InitializePresentation()
    {
        VkCommandPoolCreateInfo poolInfo = new() { queueFamilyIndex = queueFamily };
        Check(api.vkCreateCommandPool(&poolInfo, null, out presentPool), "vkCreateCommandPool(present)");
        VkCommandBufferAllocateInfo commandInfo = new() { commandPool = presentPool, level = VkCommandBufferLevel.Primary, commandBufferCount = 1 };
        VkCommandBuffer command = default;
        Check(api.vkAllocateCommandBuffers(&commandInfo, &command), "vkAllocateCommandBuffers(present)");
        presentCommand = command;
        VkFenceCreateInfo fenceInfo = new();
        Check(api.vkCreateFence(&fenceInfo, null, out presentFence), "vkCreateFence(present)");
        VkSemaphoreCreateInfo semaphoreInfo = new();
        Check(api.vkCreateSemaphore(&semaphoreInfo, null, out presentAcquire), "vkCreateSemaphore(acquire)");

        if (lowLatency)
        {
            VkOutOfBandQueueTypeInfoNV outOfBand = new() { queueType = VkOutOfBandQueueTypeNV.Present };
            api.vkQueueNotifyOutOfBandNV(presentQueue, &outOfBand);
            VkSemaphoreTypeCreateInfo timeline = new() { semaphoreType = VkSemaphoreType.Timeline };
            semaphoreInfo.pNext = &timeline;
            Check(api.vkCreateSemaphore(&semaphoreInfo, null, out sleepSemaphore), "vkCreateSemaphore(Reflex)");
        }

        Console.WriteLine($"Reflex (VK_NV_low_latency2): {(lowLatency ? "Available" : "Unavailable")}");
    }

    protected override void BeginLatency(ulong frame)
    {
        if (lowLatency && !swapChain.IsNull)
        {
            VkSemaphore semaphore = sleepSemaphore;
            VkLatencySleepInfoNV sleep = new() { signalSemaphore = semaphore, value = frame };
            lock (latencySync)
            {
                Check(api.vkLatencySleepNV(swapChain, &sleep), "vkLatencySleepNV");
            }

            VkSemaphoreWaitInfo wait = new() { semaphoreCount = 1, pSemaphores = &semaphore, pValues = &frame };
            Check(api.vkWaitSemaphores(&wait, ulong.MaxValue), "vkWaitSemaphores(Reflex)");
        }
    }

    protected override void Marker(LatencyMarker marker, ulong frame)
    {
        if (lowLatency && !swapChain.IsNull)
        {
            VkSetLatencyMarkerInfoNV info = new() { presentID = frame, marker = (VkLatencyMarkerNV)marker };
            lock (latencySync)
            {
                api.vkSetLatencyMarkerNV(swapChain, &info);
            }
        }
    }

    protected override void WaitRenderedFrame(int slot)
    {
        VkFence fence = slots[slot].Fence;
        Check(api.vkWaitForFences(1, &fence, true, ulong.MaxValue), "vkWaitForFences(render)");
        VkSemaphore complete = slots[slot].RenderComplete;
        VkPipelineStageFlags stage = VkPipelineStageFlags.AllCommands;
        VkSubmitInfo wait = new() { waitSemaphoreCount = 1, pWaitSemaphores = &complete, pWaitDstStageMask = &stage };

        lock (queueSync)
        {
            Check(api.vkQueueSubmit(presentQueue, 1, &wait, default), "vkQueueSubmit(render dependency)");
        }
    }

    protected override bool PresentImage(GpuImage image, ulong frame, bool generated)
    {
        VkResult acquire = api.vkAcquireNextImageKHR(swapChain, ulong.MaxValue, presentAcquire, default, out uint index);

        if (acquire == VkResult.ErrorOutOfDateKHR)
        {
            return false;
        }

        if (acquire != VkResult.SuboptimalKHR)
        {
            Check(acquire, "vkAcquireNextImageKHR");
        }

        Check(api.vkResetCommandPool(presentPool, 0), "vkResetCommandPool(present)");
        VkCommandBufferBeginInfo begin = new() { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
        Check(api.vkBeginCommandBuffer(presentCommand, &begin), "vkBeginCommandBuffer(present)");
        Barrier(backBuffers[index], backLayouts[index], VkImageLayout.TransferDstOptimal, Range(ImageFormat.Rgba8), presentCommand);
        VkImageBlit blit = new()
        {
            srcSubresource = new(VkImageAspectFlags.Color, 0, 0, 1),
            dstSubresource = new(VkImageAspectFlags.Color, 0, 0, 1)
        };
        blit.srcOffsets[1] = new(image.Width, image.Height, 1);
        blit.dstOffsets[1] = new(image.Width, image.Height, 1);
        api.vkCmdBlitImage(presentCommand, ((VkTexture)image).Texture, VkImageLayout.TransferSrcOptimal, backBuffers[index], VkImageLayout.TransferDstOptimal, 1, &blit, VkFilter.Nearest);
        Barrier(backBuffers[index], VkImageLayout.TransferDstOptimal, VkImageLayout.PresentSrcKHR, Range(ImageFormat.Rgba8), presentCommand);
        backLayouts[index] = VkImageLayout.PresentSrcKHR;
        Check(api.vkEndCommandBuffer(presentCommand), "vkEndCommandBuffer(present)");
        VkCommandBuffer command = presentCommand;
        VkSemaphore acquireSemaphore = presentAcquire, complete = presentSemaphores[index];
        VkFence fence = presentFence;
        Check(api.vkResetFences(1, &fence), "vkResetFences(present)");
        VkPipelineStageFlags stage = VkPipelineStageFlags.Transfer;
        VkSubmitInfo submit = new()
        {
            waitSemaphoreCount = 1,
            pWaitSemaphores = &acquireSemaphore,
            pWaitDstStageMask = &stage,
            commandBufferCount = 1,
            pCommandBuffers = &command,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &complete
        };
        VkSwapchainKHR swap = swapChain;
        ulong presentId = generated ? frame - 1 : frame;
        VkPresentIdKHR id = new() { swapchainCount = 1, pPresentIds = &presentId };
        VkPresentInfoKHR present = new()
        {
            pNext = lowLatency ? &id : null,
            waitSemaphoreCount = 1,
            pWaitSemaphores = &complete,
            swapchainCount = 1,
            pSwapchains = &swap,
            pImageIndices = &index
        };
        VkResult result;
        Marker(LatencyMarker.OutOfBandPresentStart, frame);
        lock (queueSync)
        {
            Check(api.vkQueueSubmit(presentQueue, 1, &submit, fence), "vkQueueSubmit(present)");
            result = api.vkQueuePresentKHR(presentQueue, &present);
        }

        Marker(LatencyMarker.OutOfBandPresentEnd, frame);
        Check(api.vkWaitForFences(1, &fence, true, ulong.MaxValue), "vkWaitForFences(present copy)");

        if (result is VkResult.ErrorOutOfDateKHR or VkResult.SuboptimalKHR)
        {
            return false;
        }

        Check(result, "vkQueuePresentKHR");

        return true;
    }

    private void DisposePresentation()
    {
        if (!sleepSemaphore.IsNull)
        {
            api.vkDestroySemaphore(sleepSemaphore);
        }

        if (!presentAcquire.IsNull)
        {
            api.vkDestroySemaphore(presentAcquire);
        }

        if (!presentFence.IsNull)
        {
            api.vkDestroyFence(presentFence);
        }

        if (!presentPool.IsNull)
        {
            api.vkDestroyCommandPool(presentPool);
        }
    }
}
