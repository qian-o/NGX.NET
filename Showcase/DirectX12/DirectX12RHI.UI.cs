using System.Numerics;
using Hexa.NET.ImGui;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    public override void DrawUI(ImDrawDataPtr data)
    {
        DxFrame frame = slots[Frame.Slot];
        EnsureUpload(ref frame.Vertices, ref frame.VertexCapacity, data.TotalVtxCount * sizeof(ImDrawVert));
        EnsureUpload(ref frame.Indices, ref frame.IndexCapacity, data.TotalIdxCount * sizeof(ushort));
        int vertexOffset = 0, indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            SetData(frame.Vertices, new ReadOnlySpan<ImDrawVert>(list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            SetData(frame.Indices, new ReadOnlySpan<ushort>(list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        DxImage image = (DxImage)Resources.Image(Frame.Slot, ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        ClearColor(image.Rtv);
        FrameConstants uiConstants = Frame.Constants;
        uiConstants.Size.Z = data.DisplaySize.X;
        uiConstants.Size.W = data.DisplaySize.Y;
        Bind(true, uiPipeline, uiConstants);
        CpuDescriptorHandle rtv = image.Rtv;
        commandList.Handle->OMSetRenderTargets(1, &rtv, false, null);
        SetViewport(Resources.OutputWidth, Resources.OutputHeight);
        VertexBufferView vertices = new(frame.Vertices.Handle->GetGPUVirtualAddress(), (uint)(data.TotalVtxCount * sizeof(ImDrawVert)), (uint)sizeof(ImDrawVert));
        commandList.Handle->IASetVertexBuffers(0, 1, &vertices);
        IndexBufferView indices = new(frame.Indices.Handle->GetGPUVirtualAddress(), (uint)(data.TotalIdxCount * sizeof(ushort)), Format.FormatR16Uint);
        commandList.Handle->IASetIndexBuffer(&indices);
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

                SetScissor(left, top, right, bottom);
                commandList.Handle->DrawIndexedInstanced(draw.ElemCount, 1, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        commandList.Handle->OMSetRenderTargets(0, null, false, null);
    }

    private void EnsureUpload(ref ComPtr<ID3D12Resource> buffer, ref int capacity, int required)
    {
        if (buffer.Handle != null && capacity >= required)
        {
            return;
        }

        buffer.Dispose();
        capacity = Math.Max(4096, required * 2);
        buffer = UploadBuffer(capacity);
    }
}
