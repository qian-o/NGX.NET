using System.Numerics;
using ImGuiNET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Mathematics;
using Format = Vortice.DXGI.Format;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    protected override void InitializeRenderer()
    {
        descriptors = device.CreateDescriptorHeap(new(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView, RenderLayout.FramesInFlight * DescriptorsPerFrame, DescriptorHeapFlags.ShaderVisible));
        renderTargets = device.CreateDescriptorHeap(new(DescriptorHeapType.RenderTargetView, RenderLayout.FramesInFlight * (int)ImageSlot.Count));
        depthViews = device.CreateDescriptorHeap(new(DescriptorHeapType.DepthStencilView, RenderLayout.FramesInFlight * 2));
        descriptorIncrement = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        rtvIncrement = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        dsvIncrement = device.GetDescriptorHandleIncrementSize(DescriptorHeapType.DepthStencilView);

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new()
            {
                Allocator = device.CreateCommandAllocator(CommandListType.Direct),
                Constants = UploadBuffer(RenderLayout.UniformStride * RenderLayout.UniformSlots),
                Objects = UploadBuffer(Scene.Objects.Length * sizeof(SceneObject))
            };
        }

        commandList = device.CreateCommandList<ID3D12GraphicsCommandList>(CommandListType.Direct, slots[0].Allocator);
        recording = true;

        if (RayQuerySupported)
        {
            rayCommands = commandList.QueryInterface<ID3D12GraphicsCommandList4>();
        }

        sceneBuffers[0] = StaticBuffer<SceneVertex>(Scene.Vertices);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Scene.TextureInfo);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        font = (DxImage)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        int rowPitch = (UI.FontWidth * 4 + 255) & ~255;
        ID3D12Resource fontUpload = UploadBuffer(rowPitch * UI.FontHeight);
        byte* mapped = fontUpload.Map<byte>(0);

        for (int row = 0; row < UI.FontHeight; row++)
        {
            UI.FontPixels.AsSpan(row * UI.FontWidth * 4, UI.FontWidth * 4).CopyTo(new Span<byte>(mapped + row * rowPitch, UI.FontWidth * 4));
        }

        fontUpload.Unmap(0);
        uploads.Add(fontUpload);
        Transition(font, ImageUse.CopyDestination);
        PlacedSubresourceFootPrint footprint = new()
        {
            Footprint = new()
            {
                Format = Format.R8G8B8A8_UNorm,
                Width = (uint)UI.FontWidth,
                Height = (uint)UI.FontHeight,
                Depth = 1,
                RowPitch = (uint)rowPitch
            }
        };
        commandList.CopyTextureRegion(new(font.Texture, 0), 0, 0, 0, new(fontUpload, footprint));
        Transition(font, ImageUse.ShaderRead);
        commandList.Close();
        recording = false;
        queue.ExecuteCommandList(commandList);
        WaitIdle();

        foreach (ID3D12Resource upload in uploads)
        {
            upload.Dispose();
        }

        uploads.Clear();
        RootParameter1[] parameters =
        [
            new(RootParameterType.ConstantBufferView, new RootDescriptor1(0, 0, RootDescriptorFlags.DataVolatile), ShaderVisibility.All),
            new(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, RenderLayout.SrvCount, 0, 0, flags: DescriptorRangeFlags.DataVolatile)), ShaderVisibility.All),
            new(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, RenderLayout.UavCount, 0, 0, flags: DescriptorRangeFlags.DataVolatile)), ShaderVisibility.All)
        ];
        StaticSamplerDescription sampler = new(ShaderVisibility.All, 0, 0)
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MaxLOD = float.MaxValue
        };
        root = device.CreateRootSignature(new RootSignatureDescription1(RootSignatureFlags.AllowInputAssemblerInputLayout, parameters, [sampler]));
        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
        {
            pipelines[pass] = device.CreateComputePipelineState(new()
            {
                RootSignature = root,
                ComputeShader = Compile(pass.ToString(), "compute")
            });
        }
    }

    private ID3D12PipelineState GraphicsPipeline(GraphicsPass pass)
    {
        bool ui = pass == GraphicsPass.UI;
        (string vertex, string fragment) = RenderLayout.Shaders(pass);
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        Format[] formats = new Format[targets.Length];

        for (int i = 0; i < formats.Length; i++)
        {
            formats[i] = NativeFormat(RenderLayout.Format(targets[i]));
        }

        return device.CreateGraphicsPipelineState(new()
        {
            RootSignature = root,
            VertexShader = Compile(vertex, "vertex"),
            PixelShader = Compile(fragment, "fragment"),
            BlendState = ui ? new(Blend.One, Blend.InverseSourceAlpha, Blend.One, Blend.InverseSourceAlpha) : BlendDescription.Opaque,
            RasterizerState = ui ? RasterizerDescription.CullNone : new RasterizerDescription(CullMode.None, FillMode.Solid)
            {
                FrontCounterClockwise = true
            },
            DepthStencilState = pass switch
            {
                GraphicsPass.Scene => new(true, DepthWriteMask.Zero, ComparisonFunction.Equal),
                GraphicsPass.Depth => new(true, DepthWriteMask.All, ComparisonFunction.Greater),
                GraphicsPass.Shadow => DepthStencilDescription.Default,
                _ => DepthStencilDescription.None
            },
            InputLayout = ui ? new InputLayoutDescription(
                new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
                new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0)) : default,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RenderTargetFormats = formats,
            DepthStencilFormat = ui ? Format.Unknown : Format.D32_Float
        });
    }

    private byte[] Compile(string entry, string stage) => ShaderCompiler.Compile("Scene.slang", entry, stage, false, RayQuerySupported);

    private CpuDescriptorHandle Cpu(int frame, int index) => descriptors.GetCPUDescriptorHandleForHeapStart() + (int)((frame * DescriptorsPerFrame + index) * descriptorIncrement);

    private GpuDescriptorHandle Gpu(int frame, int index) => descriptors.GetGPUDescriptorHandleForHeapStart() + (int)((frame * DescriptorsPerFrame + index) * descriptorIncrement);

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
                device.CreateShaderResourceView(i == 4 ? slots[frame].Objects : sceneBuffers[i], new()
                {
                    ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping.Default,
                    Buffer = new()
                    {
                        NumElements = counts[i],
                        StructureByteStride = strides[i]
                    }
                }, Cpu(frame, i));
            }

            if (RayQuerySupported)
            {
                device.CreateShaderResourceView(null, new()
                {
                    ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.RaytracingAccelerationStructure,
                    Shader4ComponentMapping = ShaderComponentMapping.Default,
                    RaytracingAccelerationStructure = new()
                    {
                        Location = slots[frame].Tlas!.GPUVirtualAddress
                    }
                }, Cpu(frame, 5));
            }
            else
            {
                // The raster shader variant has no acceleration-structure binding.
                device.CreateShaderResourceView(null, new()
                {
                    ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping.Default,
                    Buffer = new()
                    {
                        NumElements = 1,
                        StructureByteStride = sizeof(uint)
                    }
                }, Cpu(frame, 5));
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                DxImage image = (DxImage)Frames[frame][(int)slot];
                CreateSrv(image, Cpu(frame, 6 + (int)slot));

                if (image.Format == ImageFormat.Depth)
                {
                    image.Dsv = depthViews.GetCPUDescriptorHandleForHeapStart() + (int)((frame * 2 + (slot == ImageSlot.Depth ? 0 : 1)) * dsvIncrement);
                    device.CreateDepthStencilView(image.Texture, new()
                    {
                        Format = Format.D32_Float,
                        ViewDimension = DepthStencilViewDimension.Texture2D
                    }, image.Dsv);
                }
                else
                {
                    image.Rtv = renderTargets.GetCPUDescriptorHandleForHeapStart() + (int)((frame * (int)ImageSlot.Count + (int)slot) * rtvIncrement);
                    device.CreateRenderTargetView(image.Texture, null, image.Rtv);
                }
            }

            int previousFrame = (frame + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            CreateSrv((DxImage)Frames[previousFrame][(int)ImageSlot.Exposure], Cpu(frame, RenderLayout.PreviousExposureSrv));
            CreateSrv(font, Cpu(frame, RenderLayout.FontSrv));
            CreateSrv((DxImage)LightingSamples, Cpu(frame, RenderLayout.LightingSamplesSrv));

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                DxImage image = (DxImage)Frames[frame][(int)RenderLayout.StorageImages[i]];
                device.CreateUnorderedAccessView(image.Texture, null, new()
                {
                    Format = NativeFormat(image.Format),
                    ViewDimension = UnorderedAccessViewDimension.Texture2D
                }, Cpu(frame, RenderLayout.SrvCount + i));
            }

            device.CreateUnorderedAccessView(((DxImage)LightingSamples).Texture, null, new()
            {
                Format = NativeFormat(LightingSamples.Format),
                ViewDimension = UnorderedAccessViewDimension.Texture2DArray,
                Texture2DArray = new()
                {
                    ArraySize = (uint)LightingSamples.Layers
                }
            }, Cpu(frame, RenderLayout.SrvCount + RenderLayout.LightingSamplesUav));
        }
    }

    private void CreateSrv(DxImage image, CpuDescriptorHandle descriptor)
    {
        ShaderResourceViewDescription description = new()
        {
            Format = image.Format == ImageFormat.Depth ? Format.R32_Float : NativeFormat(image.Format),
            ViewDimension = image.Layers > 1 ? Vortice.Direct3D12.ShaderResourceViewDimension.Texture2DArray : Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
            Shader4ComponentMapping = ShaderComponentMapping.Default
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

        device.CreateShaderResourceView(image.Texture, description, descriptor);
    }

    protected override bool BeginCommands()
    {
        DxFrame frame = slots[FrameSlot];
        WaitFence(frame.Fence);
        frame.Allocator.Reset();
        commandList.Reset(frame.Allocator, null);
        recording = true;
        frame.Objects.SetData<SceneObject>(Scene.Objects);
        constantIndex = 0;

        return true;
    }

    private void Bind(bool graphics, ID3D12PipelineState pipeline, FrameConstants constants)
    {
        int offset = constantIndex++ * RenderLayout.UniformStride;

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        DxFrame frame = slots[FrameSlot];
        frame.Constants.SetData(in constants, offset);
        commandList.SetDescriptorHeaps(descriptors);
        commandList.SetPipelineState(pipeline);

        if (graphics)
        {
            commandList.SetGraphicsRootSignature(root);
            commandList.SetGraphicsRootConstantBufferView(0, frame.Constants.GPUVirtualAddress + (ulong)offset);
            commandList.SetGraphicsRootDescriptorTable(1, Gpu(FrameSlot, 0));
            commandList.SetGraphicsRootDescriptorTable(2, Gpu(FrameSlot, RenderLayout.SrvCount));
            commandList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        }
        else
        {
            commandList.SetComputeRootSignature(root);
            commandList.SetComputeRootConstantBufferView(0, frame.Constants.GPUVirtualAddress + (ulong)offset);
            commandList.SetComputeRootDescriptorTable(1, Gpu(FrameSlot, 0));
            commandList.SetComputeRootDescriptorTable(2, Gpu(FrameSlot, RenderLayout.SrvCount));
        }
    }

    protected override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[FrameSlot]);

    protected override void DrawShadow()
    {
        DxImage shadow = (DxImage)Image(ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        commandList.ClearDepthStencilView(shadow.Dsv, ClearFlags.Depth, 1, 0);
        Bind(true, shadowPipeline, Constants);
        commandList.OMSetRenderTargets(Array.Empty<CpuDescriptorHandle>(), shadow.Dsv);
        commandList.RSSetViewport(0, 0, shadow.Width, shadow.Height);
        commandList.RSSetScissorRect(shadow.Width, shadow.Height);
        commandList.DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        commandList.UnsetRenderTargets();
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
            commandList.ClearRenderTargetView(image.Rtv, new Color4(0, 0, 0, 0));
        }

        DxImage depth = (DxImage)Image(ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        commandList.ClearDepthStencilView(depth.Dsv, ClearFlags.Depth, 0, 0);
        Bind(true, depthPipeline, Constants);
        commandList.OMSetRenderTargets(Array.Empty<CpuDescriptorHandle>(), depth.Dsv);
        commandList.RSSetViewport(0, 0, InputWidth, InputHeight);
        commandList.RSSetScissorRect(InputWidth, InputHeight);
        commandList.DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        FrameConstants colorConstants = Constants;
        colorConstants.Parameters.W = Scene.Vertices.Length / 3;
        Bind(true, scenePipeline, colorConstants);
        commandList.OMSetRenderTargets(targets, depth.Dsv);
        commandList.DrawInstanced((uint)Scene.Vertices.Length, 1, 0, 0);
        commandList.UnsetRenderTargets();
    }

    protected override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(false, pipelines[pass], constants);
        commandList.Dispatch((uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
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
            frame.Vertices!.SetData(new ReadOnlySpan<ImDrawVert>((void*)list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            frame.Indices!.SetData(new ReadOnlySpan<ushort>((void*)list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        DxImage image = (DxImage)Image(ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        commandList.ClearRenderTargetView(image.Rtv, new Color4(0, 0, 0, 0));
        Bind(true, uiPipeline, Constants);
        commandList.OMSetRenderTargets(image.Rtv);
        commandList.RSSetViewport(0, 0, Window.Width, Window.Height);
        commandList.IASetVertexBuffers(0, new VertexBufferView(frame.Vertices!.GPUVirtualAddress, (uint)(data.TotalVtxCount * sizeof(ImDrawVert)), (uint)sizeof(ImDrawVert)));
        commandList.IASetIndexBuffer(frame.Indices!.GPUVirtualAddress, (uint)(data.TotalIdxCount * sizeof(ushort)), Format.R16_UInt);
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

                Vector4 clip = draw.ClipRect;
                int left = Math.Max(0, (int)clip.X), top = Math.Max(0, (int)clip.Y), right = Math.Min(Window.Width, (int)clip.Z), bottom = Math.Min(Window.Height, (int)clip.W);

                if (right <= left || bottom <= top)
                {
                    continue;
                }

                commandList.RSSetScissorRect(new Vortice.RawRect(left, top, right, bottom));
                commandList.DrawIndexedInstanced(draw.ElemCount, 1, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        commandList.UnsetRenderTargets();
    }

    private void EnsureUpload(ref ID3D12Resource? buffer, ref int capacity, int required)
    {
        if (buffer is not null && capacity >= required)
        {
            return;
        }

        buffer?.Dispose();
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
            commandList.ResourceBarrierTransition(texture.Texture, texture.State, state);
        }

        texture.State = state;
    }

    protected override void SubmitFrame()
    {
        commandList.Close();
        recording = false;
        queue.ExecuteCommandList(commandList);
    }

    protected override void WaitRenderedFrame(int slot)
    {
        ulong value = slots[slot].Fence;

        if (fence.CompletedValue < value)
        {
            fence.SetEventOnCompletion(value, presentEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
            presentEvent.WaitOne();
        }
    }

    protected override bool PresentImage(GpuImage image, ulong frame, bool generated)
    {
        presentAllocator.Reset();
        presentCommands.Reset(presentAllocator);
        ID3D12Resource back = backBuffers[(int)swapChain!.CurrentBackBufferIndex];
        presentCommands.ResourceBarrierTransition(back, ResourceStates.Present, ResourceStates.CopyDest);
        presentCommands.CopyResource(back, ((DxImage)image).Texture);
        presentCommands.ResourceBarrierTransition(back, ResourceStates.CopyDest, ResourceStates.Present);
        presentCommands.Close();
        if (generated)
        {
            Marker(LatencyMarker.OutOfBandRenderSubmitStart, frame);
        }

        queue.ExecuteCommandList(presentCommands);

        if (generated)
        {
            Marker(LatencyMarker.OutOfBandRenderSubmitEnd, frame);
        }
        Marker(generated ? LatencyMarker.OutOfBandPresentStart : LatencyMarker.PresentStart, frame);
        swapChain.Present(0, PresentFlags.None).CheckError();
        Marker(generated ? LatencyMarker.OutOfBandPresentEnd : LatencyMarker.PresentEnd, frame);
        queue.Signal(presentFence, ++presentFenceValue).CheckError();
        presentFence.SetEventOnCompletion(presentFenceValue, presentEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        presentEvent.WaitOne();

        return true;
    }

    protected override void FinishFrame()
    {
        slots[FrameSlot].Fence = ++fenceValue;
        queue.Signal(fence, fenceValue).CheckError();
    }

    private void WaitFence(ulong value)
    {
        if (value == 0 || fence.CompletedValue >= value)
        {
            return;
        }

        fence.SetEventOnCompletion(value, fenceEvent.SafeWaitHandle.DangerousGetHandle()).CheckError();
        fenceEvent.WaitOne();
    }

    protected override void WaitIdle()
    {
        if (queue is null || fence is null)
        {
            return;
        }

        queue.Signal(fence, ++fenceValue).CheckError();
        WaitFence(fenceValue);
    }

    protected override void DisposeDevice()
    {
        if (recording)
        {
            commandList?.Close();
        }

        DestroySwapChain();

        foreach (ID3D12PipelineState pipeline in pipelines.Values)
        {
            pipeline.Dispose();
        }

        shadowPipeline?.Dispose();
        scenePipeline?.Dispose();
        depthPipeline?.Dispose();
        uiPipeline?.Dispose();
        root?.Dispose();

        foreach (DxFrame? slot in slots)
        {
            slot?.Dispose();
        }

        foreach (ID3D12Resource blas in bottomLevels)
        {
            blas.Dispose();
        }

        rayCommands?.Dispose();
        rayDevice?.Dispose();

        foreach (ID3D12Resource? buffer in sceneBuffers)
        {
            buffer?.Dispose();
        }

        foreach (ID3D12Resource upload in uploads)
        {
            upload.Dispose();
        }

        font?.Dispose();
        descriptors?.Dispose();
        renderTargets?.Dispose();
        depthViews?.Dispose();
        latency?.Dispose();
        presentCommands?.Dispose();
        presentAllocator?.Dispose();
        presentFence?.Dispose();
        presentEvent.Dispose();
        commandList?.Dispose();
        fence?.Dispose();
        queue?.Dispose();
        device?.Dispose();
        adapter?.Dispose();
        factory?.Dispose();
        fenceEvent.Dispose();
    }
}
