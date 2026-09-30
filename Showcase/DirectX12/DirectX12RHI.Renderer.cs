using System.Numerics;
using ImGuiNET;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    protected override void InitializeRenderer()
    {
        descriptors = CreateDescriptorHeap(
            DescriptorHeapType.CbvSrvUav,
            RenderLayout.FramesInFlight * DescriptorsPerFrame,
            DescriptorHeapFlags.ShaderVisible);
        renderTargets = CreateDescriptorHeap(DescriptorHeapType.Rtv, RenderLayout.FramesInFlight * (int)ImageSlot.Count);
        depthViews = CreateDescriptorHeap(DescriptorHeapType.Dsv, RenderLayout.FramesInFlight * 2);
        descriptorIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        rtvIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        dsvIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.Dsv);

        for (int i = 0; i < slots.Length; i++)
        {
            DxFrame frame = slots[i] = new();
            frame.Allocator = CreateAllocator();
            frame.Constants = UploadBuffer(RenderLayout.UniformStride * RenderLayout.UniformSlots);
            frame.Objects = UploadBuffer(Scene.Objects.Length * sizeof(SceneObject));
        }

        commandList = CreateCommands(slots[0].Allocator);
        recording = true;

        if (RayQuerySupported)
        {
            Check(commandList.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList4>(), (void**)rayCommands.GetAddressOf()));
        }

        sceneBuffers[0] = StaticBuffer<SceneVertex>(Scene.Vertices);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Scene.TextureInfo);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        UploadFont();
        ExecuteUploads();
        InitializePipelines();
    }

    protected override void UpdateFontTexture()
    {
        DxFrame frame = slots[FrameSlot];
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        recording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    private void UploadFont()
    {
        DxImage replacement = (DxImage)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        int rowPitch = (UI.FontWidth * 4 + 255) & ~255;
        ComPtr<ID3D12Resource> fontUpload = UploadBuffer(rowPitch * UI.FontHeight);
        uploads.Add(fontUpload);
        byte* mapped = Map<byte>(fontUpload);

        for (int row = 0; row < UI.FontHeight; row++)
        {
            UI.FontPixels.AsSpan(row * UI.FontWidth * 4, UI.FontWidth * 4).CopyTo(new Span<byte>(mapped + row * rowPitch, UI.FontWidth * 4));
        }

        fontUpload.Handle->Unmap(0, null);
        Transition(font, ImageUse.CopyDestination);
        PlacedSubresourceFootprint footprint = new()
        {
            Footprint = new()
            {
                Format = Format.FormatR8G8B8A8Unorm,
                Width = (uint)UI.FontWidth,
                Height = (uint)UI.FontHeight,
                Depth = 1,
                RowPitch = (uint)rowPitch
            }
        };

        TextureCopyLocation source = new() { PResource = fontUpload.Handle, Type = TextureCopyType.PlacedFootprint, PlacedFootprint = footprint };
        TextureCopyLocation destination = new() { PResource = font.Texture.Handle, Type = TextureCopyType.SubresourceIndex };
        commandList.Handle->CopyTextureRegion(&destination, 0, 0, 0, &source, null);
        Transition(font, ImageUse.ShaderRead);
    }

    private void ExecuteUploads()
    {
        Check(commandList.Handle->Close());
        recording = false;
        Execute(queue, commandList);
        WaitIdle();

        foreach (ComPtr<ID3D12Resource> upload in uploads)
        {
            upload.Dispose();
        }

        uploads.Clear();
    }

    private CpuDescriptorHandle Cpu(int frame, int index) =>
        new() { Ptr = descriptors.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * DescriptorsPerFrame + index) * descriptorIncrement) };

    private GpuDescriptorHandle Gpu(int frame, int index) =>
        new() { Ptr = descriptors.Handle->GetGPUDescriptorHandleForHeapStart().Ptr + (ulong)((frame * DescriptorsPerFrame + index) * descriptorIncrement) };

    protected override void UpdateDescriptors()
    {
        uint[] strides =
        [
            (uint)sizeof(SceneVertex),
            (uint)sizeof(SceneMaterial),
            sizeof(uint),
            (uint)sizeof(TextureDescription),
            (uint)sizeof(SceneObject)
        ];

        uint[] counts =
        [
            (uint)Scene.Vertices.Length,
            (uint)Scene.Materials.Length,
            (uint)Scene.Texels.Length,
            (uint)Scene.TextureInfo.Length,
            (uint)Scene.Objects.Length
        ];

        for (int frame = 0; frame < Frames.Length; frame++)
        {
            for (int i = 0; i < 5; i++)
            {
                ShaderResourceViewDesc bufferView = new()
                {
                    ViewDimension = SrvDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    Buffer = new()
                    {
                        NumElements = counts[i],
                        StructureByteStride = strides[i]
                    }
                };
                device.Handle->CreateShaderResourceView((i == 4 ? slots[frame].Objects : sceneBuffers[i]).Handle, &bufferView, Cpu(frame, i));
            }

            if (RayQuerySupported)
            {
                ShaderResourceViewDesc rayView = new()
                {
                    ViewDimension = SrvDimension.RaytracingAccelerationStructure,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    RaytracingAccelerationStructure = new()
                    {
                        Location = slots[frame].Tlas.Handle->GetGPUVirtualAddress()
                    }
                };
                device.Handle->CreateShaderResourceView(null, &rayView, Cpu(frame, 5));
            }
            else
            {
                // The raster shader variant has no acceleration-structure binding.
                ShaderResourceViewDesc rayView = new()
                {
                    ViewDimension = SrvDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    Buffer = new()
                    {
                        NumElements = 1,
                        StructureByteStride = sizeof(uint)
                    }
                };
                device.Handle->CreateShaderResourceView(null, &rayView, Cpu(frame, 5));
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                DxImage image = (DxImage)Frames[frame][(int)slot];
                CreateSrv(image, Cpu(frame, 6 + (int)slot));

                if (image.Format == ImageFormat.Depth)
                {
                    image.Dsv = new() { Ptr = depthViews.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * 2 + (slot == ImageSlot.Depth ? 0 : 1)) * dsvIncrement) };
                    DepthStencilViewDesc depthView = new()
                    {
                        Format = Format.FormatD32Float,
                        ViewDimension = DsvDimension.Texture2D
                    };
                    device.Handle->CreateDepthStencilView(image.Texture.Handle, &depthView, image.Dsv);
                }
                else
                {
                    image.Rtv = new() { Ptr = renderTargets.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * (int)ImageSlot.Count + (int)slot) * rtvIncrement) };
                    device.Handle->CreateRenderTargetView(image.Texture.Handle, null, image.Rtv);
                }
            }

            int previousFrame = (frame + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            CreateSrv((DxImage)Frames[previousFrame][(int)ImageSlot.Exposure], Cpu(frame, RenderLayout.PreviousExposureSrv));
            CreateSrv(font, Cpu(frame, RenderLayout.FontSrv));
            CreateSrv((DxImage)LightingSamples, Cpu(frame, RenderLayout.LightingSamplesSrv));

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                DxImage image = (DxImage)Frames[frame][(int)RenderLayout.StorageImages[i]];
                UnorderedAccessViewDesc storageView = new()
                {
                    Format = NativeFormat(image.Format),
                    ViewDimension = UavDimension.Texture2D
                };
                device.Handle->CreateUnorderedAccessView(image.Texture.Handle, null, &storageView, Cpu(frame, RenderLayout.SrvCount + i));
            }

            UnorderedAccessViewDesc lightingView = new()
            {
                Format = NativeFormat(LightingSamples.Format),
                ViewDimension = UavDimension.Texture2Darray,
                Texture2DArray = new()
                {
                    ArraySize = (uint)LightingSamples.Layers
                }
            };
            device.Handle->CreateUnorderedAccessView(((DxImage)LightingSamples).Texture.Handle, null, &lightingView, Cpu(frame, RenderLayout.SrvCount + RenderLayout.LightingSamplesUav));
        }
    }

    private void CreateSrv(DxImage image, CpuDescriptorHandle descriptor)
    {
        ShaderResourceViewDesc description = new()
        {
            Format = image.Format == ImageFormat.Depth ? Format.FormatR32Float : NativeFormat(image.Format),
            ViewDimension = image.Layers > 1 ? SrvDimension.Texture2Darray : SrvDimension.Texture2D,
            Shader4ComponentMapping = ShaderComponentMapping
        };

        if (image.Layers > 1)
        {
            description.Texture2DArray = new()
            {
                MipLevels = 1,
                ArraySize = (uint)image.Layers
            };
        }
        else
        {
            description.Texture2D = new()
            {
                MipLevels = 1
            };
        }

        device.Handle->CreateShaderResourceView(image.Texture.Handle, &description, descriptor);
    }

    protected override void BeginCommands()
    {
        DxFrame frame = slots[FrameSlot];
        WaitFence(frame.Fence);
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        recording = true;
        SetData<SceneObject>(frame.Objects, Scene.Objects);
        constantIndex = 0;
    }

    private void Bind(bool graphics, ComPtr<ID3D12PipelineState> pipeline, FrameConstants constants)
    {
        int offset = constantIndex++ * RenderLayout.UniformStride;

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        DxFrame frame = slots[FrameSlot];
        SetData<FrameConstants>(frame.Constants, new ReadOnlySpan<FrameConstants>(in constants), offset);
        ID3D12DescriptorHeap* heap = descriptors.Handle;
        commandList.Handle->SetDescriptorHeaps(1, &heap);
        commandList.Handle->SetPipelineState(pipeline.Handle);

        if (graphics)
        {
            commandList.Handle->SetGraphicsRootSignature(root.Handle);
            commandList.Handle->SetGraphicsRootConstantBufferView(0, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetGraphicsRootDescriptorTable(1, Gpu(FrameSlot, 0));
            commandList.Handle->SetGraphicsRootDescriptorTable(2, Gpu(FrameSlot, RenderLayout.SrvCount));
            commandList.Handle->IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        }
        else
        {
            commandList.Handle->SetComputeRootSignature(root.Handle);
            commandList.Handle->SetComputeRootConstantBufferView(0, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetComputeRootDescriptorTable(1, Gpu(FrameSlot, 0));
            commandList.Handle->SetComputeRootDescriptorTable(2, Gpu(FrameSlot, RenderLayout.SrvCount));
        }
    }

    protected override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[FrameSlot]);

    protected override void DrawShadow()
    {
        DxImage shadow = (DxImage)Image(ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        commandList.Handle->ClearDepthStencilView(shadow.Dsv, ClearFlags.Depth, 1, 0, 0, null);
        Bind(true, shadowPipeline, Constants);
        CpuDescriptorHandle dsv = shadow.Dsv;
        commandList.Handle->OMSetRenderTargets(0, null, false, &dsv);
        SetViewport(shadow.Width, shadow.Height);
        SetScissor(0, 0, shadow.Width, shadow.Height);
        commandList.Handle->DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        commandList.Handle->OMSetRenderTargets(0, null, false, null);
    }

    protected override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        Span<CpuDescriptorHandle> targets = stackalloc CpuDescriptorHandle[colorTargets.Length];

        for (int i = 0; i < targets.Length; i++)
        {
            DxImage image = (DxImage)Image(colorTargets[i]);
            Transition(image, ImageUse.ColorAttachment);
            targets[i] = image.Rtv;
            ClearColor(image.Rtv);
        }

        DxImage depth = (DxImage)Image(ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        commandList.Handle->ClearDepthStencilView(depth.Dsv, ClearFlags.Depth, 0, 0, 0, null);
        Bind(true, depthPipeline, Constants);
        CpuDescriptorHandle dsv = depth.Dsv;
        commandList.Handle->OMSetRenderTargets(0, null, false, &dsv);
        SetViewport(InputWidth, InputHeight);
        SetScissor(0, 0, InputWidth, InputHeight);
        commandList.Handle->DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        FrameConstants colorConstants = Constants;
        colorConstants.Parameters.W = Scene.Vertices.Length / 3;
        Bind(true, scenePipeline, colorConstants);
        fixed (CpuDescriptorHandle* handles = targets)
        {
            commandList.Handle->OMSetRenderTargets((uint)targets.Length, handles, false, &dsv);
        }
        commandList.Handle->DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        commandList.Handle->OMSetRenderTargets(0, null, false, null);
    }

    protected override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(false, pipelines[pass], constants);
        commandList.Handle->Dispatch((uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    protected override void DrawUI(ImDrawDataPtr data)
    {
        DxFrame frame = slots[FrameSlot];
        EnsureUpload(ref frame.Vertices, ref frame.VertexCapacity, data.TotalVtxCount * sizeof(ImDrawVert));
        EnsureUpload(ref frame.Indices, ref frame.IndexCapacity, data.TotalIdxCount * sizeof(ushort));
        int vertexOffset = 0, indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            SetData(frame.Vertices, new ReadOnlySpan<ImDrawVert>((void*)list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            SetData(frame.Indices, new ReadOnlySpan<ushort>((void*)list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        DxImage image = (DxImage)Image(ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        ClearColor(image.Rtv);
        FrameConstants uiConstants = Constants;
        uiConstants.Size.Z = data.DisplaySize.X;
        uiConstants.Size.W = data.DisplaySize.Y;
        Bind(true, uiPipeline, uiConstants);
        CpuDescriptorHandle rtv = image.Rtv;
        commandList.Handle->OMSetRenderTargets(1, &rtv, false, null);
        SetViewport(Window.Width, Window.Height);
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
                ImDrawCmdPtr draw = list.CmdBuffer[c];

                if (draw.UserCallback != 0)
                {
                    throw new NotSupportedException("Unexpected UI draw callback.");
                }

                Vector4 clip = draw.ClipRect * new Vector4(data.FramebufferScale, data.FramebufferScale.X, data.FramebufferScale.Y);
                int left = Math.Max(0, (int)clip.X),
                    top = Math.Max(0, (int)clip.Y),
                    right = Math.Min(Window.Width, (int)clip.Z),
                    bottom = Math.Min(Window.Height, (int)clip.W);

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

    protected override void Transition(GpuImage image, ImageUse use)
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

    protected override void SubmitFrame()
    {
        Check(commandList.Handle->Close());
        recording = false;
        Execute(queue, commandList);
    }

    protected override void WaitRenderedFrame(int slot)
    {
        ulong value = slots[slot].Fence;

        if (fence.Handle->GetCompletedValue() < value)
        {
            Check(fence.Handle->SetEventOnCompletion(value, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }

        Check(presentQueue.Handle->Wait(fence.Handle, value));
    }

    protected override bool PresentImage(GpuImage image)
    {
        WaitPresentation();
        Check(presentAllocator.Handle->Reset());
        Check(presentCommands.Handle->Reset(presentAllocator.Handle, null));
        ComPtr<ID3D12Resource> back = backBuffers[(int)swapChain.Handle->GetCurrentBackBufferIndex()];
        TransitionBarrier(presentCommands, back, ResourceStates.Present, ResourceStates.CopyDest);
        presentCommands.Handle->CopyResource(back.Handle, ((DxImage)image).Texture.Handle);
        TransitionBarrier(presentCommands, back, ResourceStates.CopyDest, ResourceStates.Present);
        Check(presentCommands.Handle->Close());
        Execute(presentQueue, presentCommands);
        // The fence protects the copy's command allocator and source texture.
        // Signal before Present so it does not wait for DXGI presentation work.
        Check(presentQueue.Handle->Signal(presentFence.Handle, ++presentFenceValue));
        Check(swapChain.Handle->Present(0, 0));

        return true;
    }

    protected override void WaitPresentation()
    {
        if (presentFence.Handle->GetCompletedValue() < presentFenceValue)
        {
            Check(presentFence.Handle->SetEventOnCompletion(presentFenceValue, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }
    }

    protected override void FinishFrame()
    {
        slots[FrameSlot].Fence = ++fenceValue;
        Check(queue.Handle->Signal(fence.Handle, fenceValue));
    }

    private void WaitFence(ulong value)
    {
        if (value == 0 || fence.Handle->GetCompletedValue() >= value)
        {
            return;
        }

        Check(fence.Handle->SetEventOnCompletion(value, (void*)fenceEvent.SafeWaitHandle.DangerousGetHandle()));
        fenceEvent.WaitOne();
    }

    protected override void WaitIdle()
    {
        if (queue.Handle == null || fence.Handle == null)
        {
            return;
        }

        Check(queue.Handle->Signal(fence.Handle, ++fenceValue));
        WaitFence(fenceValue);
    }

    protected override void DisposeDevice()
    {
        if (recording)
        {
            commandList.Handle->Close();
        }

        DestroySwapChain();

        foreach (ComPtr<ID3D12PipelineState> pipeline in pipelines.Values)
        {
            pipeline.Dispose();
        }

        shadowPipeline.Dispose();
        scenePipeline.Dispose();
        depthPipeline.Dispose();
        uiPipeline.Dispose();
        root.Dispose();

        foreach (DxFrame? slot in slots)
        {
            slot?.Dispose();
        }

        foreach (ComPtr<ID3D12Resource> blas in bottomLevels)
        {
            blas.Dispose();
        }

        rayCommands.Dispose();
        rayDevice.Dispose();

        foreach (ComPtr<ID3D12Resource> buffer in sceneBuffers)
        {
            buffer.Dispose();
        }

        foreach (ComPtr<ID3D12Resource> upload in uploads)
        {
            upload.Dispose();
        }

        font?.Dispose();
        descriptors.Dispose();
        renderTargets.Dispose();
        depthViews.Dispose();
        presentCommands.Dispose();
        presentAllocator.Dispose();
        presentFence.Dispose();
        presentEvent.Dispose();
        commandList.Dispose();
        fence.Dispose();
        presentQueue.Dispose();
        queue.Dispose();
        device.Dispose();
        adapter.Dispose();
        factory.Dispose();
        fenceEvent.Dispose();
        d3d12?.Dispose();
        dxgi?.Dispose();
    }
}
