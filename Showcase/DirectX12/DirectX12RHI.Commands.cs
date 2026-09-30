using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    protected override void BeginCommands()
    {
        DxFrame frame = slots[Frame.Slot];
        WaitFence(frame.Fence);
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        recording = true;
        SetData<SceneObject>(frame.Objects, Resources.Scene.Objects);
        constantIndex = 0;
    }

    private void Bind(bool graphics, ComPtr<ID3D12PipelineState> pipeline, FrameConstants constants)
    {
        int offset = constantIndex++ * RenderLayout.UniformStride;

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        DxFrame frame = slots[Frame.Slot];
        SetData<FrameConstants>(frame.Constants, new ReadOnlySpan<FrameConstants>(in constants), offset);
        ID3D12DescriptorHeap* heap = descriptors.Handle;
        commandList.Handle->SetDescriptorHeaps(1, &heap);
        commandList.Handle->SetPipelineState(pipeline.Handle);

        if (graphics)
        {
            commandList.Handle->SetGraphicsRootSignature(root.Handle);
            commandList.Handle->SetGraphicsRootConstantBufferView(0, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetGraphicsRootDescriptorTable(1, Gpu(Frame.Slot, 0));
            commandList.Handle->SetGraphicsRootDescriptorTable(2, Gpu(Frame.Slot, RenderLayout.SrvCount));
            commandList.Handle->IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        }
        else
        {
            commandList.Handle->SetComputeRootSignature(root.Handle);
            commandList.Handle->SetComputeRootConstantBufferView(0, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetComputeRootDescriptorTable(1, Gpu(Frame.Slot, 0));
            commandList.Handle->SetComputeRootDescriptorTable(2, Gpu(Frame.Slot, RenderLayout.SrvCount));
        }
    }

    public override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[Frame.Slot]);

    public override void DrawShadow()
    {
        DxImage shadow = (DxImage)Resources.Image(Frame.Slot, ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        commandList.Handle->ClearDepthStencilView(shadow.Dsv, ClearFlags.Depth, 1, 0, 0, null);
        Bind(true, shadowPipeline, Frame.Constants);
        CpuDescriptorHandle dsv = shadow.Dsv;
        commandList.Handle->OMSetRenderTargets(0, null, false, &dsv);
        SetViewport(shadow.Width, shadow.Height);
        SetScissor(0, 0, shadow.Width, shadow.Height);
        commandList.Handle->DrawInstanced((uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        commandList.Handle->OMSetRenderTargets(0, null, false, null);
    }

    public override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        Span<CpuDescriptorHandle> targets = stackalloc CpuDescriptorHandle[colorTargets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            DxImage image = (DxImage)Resources.Image(Frame.Slot, colorTargets[i]);
            Transition(image, ImageUse.ColorAttachment);
            targets[i] = image.Rtv;
            ClearColor(image.Rtv);
        }

        DxImage depth = (DxImage)Resources.Image(Frame.Slot, ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        commandList.Handle->ClearDepthStencilView(depth.Dsv, ClearFlags.Depth, 0, 0, 0, null);
        Bind(true, depthPipeline, Frame.Constants);
        CpuDescriptorHandle dsv = depth.Dsv;
        commandList.Handle->OMSetRenderTargets(0, null, false, &dsv);
        SetViewport(Resources.InputWidth, Resources.InputHeight);
        SetScissor(0, 0, Resources.InputWidth, Resources.InputHeight);
        commandList.Handle->DrawInstanced((uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        FrameConstants colorConstants = Frame.Constants;
        colorConstants.Parameters.W = Resources.Scene.Vertices.Length / 3;
        Bind(true, scenePipeline, colorConstants);
        fixed (CpuDescriptorHandle* handles = targets)
        {
            commandList.Handle->OMSetRenderTargets((uint)targets.Length, handles, false, &dsv);
        }
        commandList.Handle->DrawInstanced((uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        commandList.Handle->OMSetRenderTargets(0, null, false, null);
    }

    public override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(false, pipelines[pass], constants);
        commandList.Handle->Dispatch((uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    public override void Transition(GpuImage image, ImageUse use)
    {
        DxImage texture = (DxImage)image;
        ResourceStates state = use switch
        {
            ImageUse.Storage => ResourceStates.UnorderedAccess,
            ImageUse.ColorAttachment => ResourceStates.RenderTarget,
            ImageUse.DepthAttachment => ResourceStates.DepthWrite,
            ImageUse.CopySource => ResourceStates.CopySource,
            ImageUse.CopyDestination => ResourceStates.CopyDest,
            _ => ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource
        };

        if (texture.State != state)
        {
            TransitionBarrier(commandList, texture.Texture, texture.State, state);
        }

        texture.State = state;
    }

    public override void SubmitFrame()
    {
        Check(commandList.Handle->Close());
        recording = false;
        Execute(queue, commandList);
        slots[Frame.Slot].Fence = ++fenceValue;
        Check(queue.Handle->Signal(fence.Handle, fenceValue));
    }
}
