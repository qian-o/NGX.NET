using System.Numerics;
using Hexa.NET.ImGui;
using Showcase.Models;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private VkTexture font = null!;

    public override void UpdateFontTexture()
    {
        VkFrame frame = slots[Frame.Slot];
        Check(api.ResetCommandPool(device, frame.Pool, 0), "vkResetCommandPool(font upload)");
        commandBuffer = frame.Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(font upload)");
        recording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    private void UploadFont()
    {
        VkTexture replacement = (VkTexture)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        VkBufferResource fontUpload = CreateBuffer((ulong)UI.FontPixels.Length, BufferUsageFlags.TransferSrcBit, true);
        uploads.Add(fontUpload);
        fontUpload.Write<byte>(UI.FontPixels);
        Transition(font, ImageUse.CopyDestination);
        BufferImageCopy copy = new()
        {
            ImageSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageExtent = new((uint)UI.FontWidth, (uint)UI.FontHeight, 1)
        };

        api.CmdCopyBufferToImage(commandBuffer, fontUpload.Buffer, font.Texture, ImageLayout.TransferDstOptimal, 1, &copy);
        Transition(font, ImageUse.ShaderRead);
    }

    public override void DrawUI(ImDrawDataPtr data)
    {
        VkFrame frame = slots[Frame.Slot];

        void Ensure(ref VkBufferResource? buffer, ulong size, BufferUsageFlags usage)
        {
            if (buffer is not null && buffer.Size >= size)
            {
                return;
            }

            VkBufferResource replacement = CreateBuffer(Math.Max(4096, size * 2), usage, true);
            buffer?.Dispose();
            buffer = replacement;
        }

        Ensure(ref frame.Vertices, (ulong)(data.TotalVtxCount * sizeof(ImDrawVert)), BufferUsageFlags.VertexBufferBit);
        Ensure(ref frame.Indices, (ulong)(data.TotalIdxCount * sizeof(ushort)), BufferUsageFlags.IndexBufferBit);
        int vertexOffset = 0, indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            frame.Vertices!.Write(new ReadOnlySpan<ImDrawVert>(list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            frame.Indices!.Write(new ReadOnlySpan<ushort>(list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        VkTexture image = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        RenderingAttachmentInfo attachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = image.View,
            ImageLayout = image.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)Resources.OutputWidth, (uint)Resources.OutputHeight)),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &attachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants uiConstants = Frame.Constants;
        uiConstants.Size.Z = data.DisplaySize.X;
        uiConstants.Size.W = data.DisplaySize.Y;
        Bind(PipelineBindPoint.Graphics, uiPipeline, uiConstants);
        Viewport(Resources.OutputWidth, Resources.OutputHeight);
        Buffer vertexBuffer = frame.Vertices!.Buffer;
        ulong offset = 0;
        api.CmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
        api.CmdBindIndexBuffer(commandBuffer, frame.Indices!.Buffer, 0, IndexType.Uint16);
        vertexOffset = 0;
        indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];

            for (int c = 0; c < list.CmdBuffer.Size; c++)
            {
                ref readonly ImDrawCmd draw = ref list.CmdBuffer.Data[c];

                if (draw.UserCallback != null)
                {
                    throw new NotSupportedException("Unexpected UI draw callback.");
                }

                Vector4 clip = draw.ClipRect * new Vector4(data.FramebufferScale, data.FramebufferScale.X, data.FramebufferScale.Y);
                int left = Math.Max(0, (int)clip.X),
                    top = Math.Max(0, (int)clip.Y),
                    right = Math.Min(Resources.OutputWidth, (int)clip.Z),
                    bottom = Math.Min(Resources.OutputHeight, (int)clip.W);

                if (right <= left || bottom <= top)
                {
                    continue;
                }

                Rect2D scissor = new(new(left, top), new((uint)(right - left), (uint)(bottom - top)));
                api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
                api.CmdDrawIndexed(commandBuffer, draw.ElemCount, 1, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        api.CmdEndRendering(commandBuffer);
    }
}
