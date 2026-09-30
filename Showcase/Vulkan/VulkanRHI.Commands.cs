using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private CommandBuffer commandBuffer;
    private int uniformStride = RenderLayout.UniformStride;
    private int constantIndex;
    private bool recording;

    protected override void BeginCommands()
    {
        VkFrame frame = slots[Frame.Slot];
        Fence fence = frame.Fence;
        Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(frame)");
        Check(api.ResetFences(device, 1, &fence), "vkResetFences");
        Check(api.ResetCommandPool(device, frame.Pool, 0), "vkResetCommandPool");
        commandBuffer = frame.Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer");
        recording = true;
        frame.Objects.Write<SceneObject>(Resources.Scene.Objects);
        constantIndex = 0;
    }

    private void Bind(PipelineBindPoint point, Pipeline pipeline, FrameConstants constants)
    {
        VkFrame frame = slots[Frame.Slot];
        uint offset = (uint)(constantIndex++ * uniformStride);

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        *(FrameConstants*)((byte*)frame.Constants.Mapped + offset) = constants;
        api.CmdBindPipeline(commandBuffer, point, pipeline);
        DescriptorSet descriptors = frame.Descriptors;
        api.CmdBindDescriptorSets(commandBuffer, point, pipelineLayout, 0, 1, &descriptors, 1, &offset);
    }

    private void Viewport(int width, int height)
    {
        Viewport viewport = new(0, 0, width, height, 0, 1);
        Rect2D scissor = new(new(0, 0), new((uint)width, (uint)height));
        api.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
    }

    public override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[Frame.Slot]);

    public override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(PipelineBindPoint.Compute, pipelines[pass], constants);
        api.CmdDispatch(commandBuffer, (uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    public override void Transition(GpuImage image, ImageUse use)
    {
        VkTexture texture = (VkTexture)image;
        ImageLayout layout = use switch
        {
            ImageUse.Storage => ImageLayout.General,
            ImageUse.ColorAttachment => ImageLayout.ColorAttachmentOptimal,
            ImageUse.DepthAttachment => ImageLayout.DepthStencilAttachmentOptimal,
            ImageUse.CopySource => ImageLayout.TransferSrcOptimal,
            ImageUse.CopyDestination => ImageLayout.TransferDstOptimal,
            _ => ImageLayout.ShaderReadOnlyOptimal
        };

        if (texture.Layout != layout)
        {
            Barrier(texture.Texture, texture.Layout, layout, Range(texture.Format, texture.Layers));
        }

        texture.Layout = layout;
    }

    private void Barrier(Image image, ImageLayout oldLayout, ImageLayout newLayout, ImageSubresourceRange range, CommandBuffer target = default)
    {
        ImageMemoryBarrier2 barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = oldLayout == ImageLayout.Undefined ? PipelineStageFlags2.None : PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = oldLayout == ImageLayout.Undefined ? AccessFlags2.None : AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = range
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier
        };

        api.CmdPipelineBarrier2(target.Handle == 0 ? commandBuffer : target, &dependency);
    }

    public override void SubmitFrame()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer");
        recording = false;
        CommandBuffer command = commandBuffer;
        Semaphore complete = slots[Frame.Slot].RenderComplete;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &complete
        };

        lock (queueSync)
        {
            Check(api.QueueSubmit(queue, 1, &submit, slots[Frame.Slot].Fence), "vkQueueSubmit(frame)");
        }
    }

    public override void WaitIdle()
    {
        if (device.Handle != 0)
        {
            Check(api.DeviceWaitIdle(device), "vkDeviceWaitIdle");
        }
    }
}
