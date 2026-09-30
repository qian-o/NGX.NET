using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    public override void DrawShadow()
    {
        VkTexture shadow = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depth = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = shadow.View,
            ImageLayout = shadow.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new() { DepthStencil = new(1, 0) }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)shadow.Width, (uint)shadow.Height)),
            LayerCount = 1,
            PDepthAttachment = &depth
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, shadowPipeline, Frame.Constants);
        Viewport(shadow.Width, shadow.Height);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }

    public override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        RenderingAttachmentInfo* colors = stackalloc RenderingAttachmentInfo[colorTargets.Length];

        for (int i = 0; i < colorTargets.Length; i++)
        {
            VkTexture image = (VkTexture)Resources.Image(Frame.Slot, colorTargets[i]);
            Transition(image, ImageUse.ColorAttachment);
            colors[i] = new()
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageView = image.View,
                ImageLayout = image.Layout,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store
            };
        }

        VkTexture depth = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depthAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = depth.View,
            ImageLayout = depth.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new() { DepthStencil = new(0, 0) }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)Resources.InputWidth, (uint)Resources.InputHeight)),
            LayerCount = 1,
            PDepthAttachment = &depthAttachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, depthPipeline, Frame.Constants);
        Viewport(Resources.InputWidth, Resources.InputHeight);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);

        // Make prepass depth writes visible to the next rendering scope's tests.
        Barrier(depth.Texture, depth.Layout, depth.Layout, Range(depth.Format));
        depthAttachment.LoadOp = AttachmentLoadOp.Load;
        rendering.ColorAttachmentCount = (uint)colorTargets.Length;
        rendering.PColorAttachments = colors;
        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants colorConstants = Frame.Constants;
        colorConstants.Parameters.W = Resources.Scene.Vertices.Length / 3;
        Bind(PipelineBindPoint.Graphics, scenePipeline, colorConstants);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }
}
