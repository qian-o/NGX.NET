using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Windowing;

namespace Showcase.DirectX12;

internal unsafe class DirectX12RHI(IWindow window, ImGuiHandler ui) : RHI(window, ui)
{
    private const int DescriptorsPerFrame = RenderLayout.SrvCount + RenderLayout.UavCount;
    // SDK constants expressed by macros that Silk does not emit.
    private const uint ShaderComponentMapping = 0x1688; // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
    private const uint NoAltEnter = 0x2; // DXGI_MWA_NO_ALT_ENTER

    private static readonly uint[] SceneBufferStridesInBytes = [(uint)sizeof(SceneVertex), (uint)sizeof(SceneMaterial), sizeof(uint), (uint)sizeof(TextureDescription), (uint)sizeof(SceneObject)];

    private readonly AutoResetEvent presentEvent = new(false);
    private readonly Dictionary<ComputePass, ComPtr<ID3D12PipelineState>> pipelines = [];
    private readonly DxFrame[] slots = new DxFrame[RenderLayout.FramesInFlight];
    private readonly ComPtr<ID3D12Resource>[] sceneBuffers = new ComPtr<ID3D12Resource>[4];
    private readonly List<ComPtr<ID3D12Resource>> uploads = [];
    private readonly List<ComPtr<ID3D12Resource>> backBuffers = [];
    private readonly AutoResetEvent fenceEvent = new(false);
    private readonly List<ComPtr<ID3D12Resource>> bottomLevels = [];

    private ComPtr<ID3D12CommandAllocator> presentAllocator;
    private ComPtr<ID3D12GraphicsCommandList> presentCommands;
    private ComPtr<ID3D12Fence> presentFence;
    private ulong presentFenceValue;
    private bool tearingSupported;
    private ComPtr<ID3D12Device> device;
    private ComPtr<IDXGIFactory4> factory;
    private ComPtr<IDXGIAdapter1> adapter;
    private ComPtr<ID3D12CommandQueue> queue;
    private ComPtr<ID3D12CommandQueue> presentQueue;
    private ComPtr<ID3D12GraphicsCommandList> commandList;
    private ComPtr<ID3D12Fence> fence;
    private ComPtr<IDXGISwapChain3> swapChain;
    private ComPtr<ID3D12RootSignature> root;
    private ComPtr<ID3D12PipelineState> scenePipeline;
    private ComPtr<ID3D12PipelineState> depthPipeline;
    private ComPtr<ID3D12PipelineState> uiPipeline;
    private ComPtr<ID3D12PipelineState> shadowPipeline;
    private ComPtr<ID3D12DescriptorHeap> descriptors;
    private ComPtr<ID3D12DescriptorHeap> renderTargets;
    private ComPtr<ID3D12DescriptorHeap> depthViews;
    private DxImage font = null!;
    private uint descriptorIncrement;
    private uint rtvIncrement;
    private uint dsvIncrement;
    private ulong fenceValue;
    private int constantIndex;
    private bool recording;
    private D3D12 d3d12 = null!;
    private DXGI dxgi = null!;
    private ComPtr<ID3D12Device5> rayDevice;
    private ComPtr<ID3D12GraphicsCommandList4> rayCommands;

    public override nint Command => (nint)commandList.Handle;

    public override void UpdateRayTracingScene()
    {
        UpdateAccelerationStructure(slots[Frame.Slot]);
    }

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

    public override void UpdateDescriptors()
    {
        uint[] counts = [(uint)Resources.Scene.Vertices.Length, (uint)Resources.Scene.Materials.Length, (uint)Resources.Scene.Texels.Length, (uint)Resources.Scene.TextureInfo.Length, (uint)Resources.Scene.Objects.Length];
        for (int frame = 0; frame < Resources.Frames.Length; frame++)
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
                        StructureByteStride = SceneBufferStridesInBytes[i]
                    }
                };
                device.Handle->CreateShaderResourceView((i is 4 ? slots[frame].Objects : sceneBuffers[i]).Handle, &bufferView, Cpu(frame, i));
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
                DxImage image = (DxImage)Resources.Frames[frame][(int)slot];
                CreateSrv(image, Cpu(frame, 6 + (int)slot));

                if (image.Format is ImageFormat.Depth)
                {
                    image.Dsv = new()
                    {
                        Ptr = depthViews.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)(((frame * 2) + (slot is ImageSlot.Depth ? 0 : 1)) * dsvIncrement)
                    };
                    DepthStencilViewDesc depthView = new()
                    {
                        Format = Format.FormatD32Float,
                        ViewDimension = DsvDimension.Texture2D
                    };
                    device.Handle->CreateDepthStencilView(image.Texture.Handle, &depthView, image.Dsv);
                }
                else
                {
                    image.Rtv = new()
                    {
                        Ptr = renderTargets.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)(((frame * (int)ImageSlot.Count) + (int)slot) * rtvIncrement)
                    };
                    device.Handle->CreateRenderTargetView(image.Texture.Handle, null, image.Rtv);
                }
            }

            int previousFrame = (frame + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            CreateSrv((DxImage)Resources.Frames[previousFrame][(int)ImageSlot.Exposure], Cpu(frame, RenderLayout.PreviousExposureSrv));
            CreateSrv(font, Cpu(frame, RenderLayout.FontSrv));
            CreateSrv((DxImage)Resources.LightingSamples, Cpu(frame, RenderLayout.LightingSamplesSrv));

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                DxImage image = (DxImage)Resources.Frames[frame][(int)RenderLayout.StorageImages[i]];
                UnorderedAccessViewDesc storageView = new()
                {
                    Format = NativeFormat(image.Format),
                    ViewDimension = UavDimension.Texture2D
                };
                device.Handle->CreateUnorderedAccessView(image.Texture.Handle, null, &storageView, Cpu(frame, RenderLayout.SrvCount + i));
            }

            UnorderedAccessViewDesc lightingView = new()
            {
                Format = NativeFormat(Resources.LightingSamples.Format),
                ViewDimension = UavDimension.Texture2Darray,
                Texture2DArray = new()
                {
                    ArraySize = (uint)Resources.LightingSamples.Layers
                }
            };
            device.Handle->CreateUnorderedAccessView(((DxImage)Resources.LightingSamples).Texture.Handle, null, &lightingView, Cpu(frame, RenderLayout.SrvCount + RenderLayout.LightingSamplesUav));
        }
    }

    public override void UpdateFontTexture()
    {
        DxFrame frame = slots[Frame.Slot];
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        recording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    public override void CreateSwapChain()
    {
        SwapChainDesc1 description = new()
        {
            Width = (uint)Resources.OutputWidth,
            Height = (uint)Resources.OutputHeight,
            Format = Format.FormatR8G8B8A8Unorm,
            BufferCount = RenderLayout.FramesInFlight,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SampleDesc = new(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Scaling = Scaling.Stretch,
            AlphaMode = AlphaMode.Ignore,
            Flags = tearingSupported ? (uint)SwapChainFlag.AllowTearing : 0
        };

        using ComPtr<IDXGISwapChain1> created = default;
        Check(factory.Handle->CreateSwapChainForHwnd((IUnknown*)presentQueue.Handle, Window.Native!.Win32!.Value.Hwnd, &description, null, null, created.GetAddressOf()));
        Check(created.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**)swapChain.GetAddressOf()));
        Check(factory.Handle->MakeWindowAssociation(Window.Native!.Win32!.Value.Hwnd, NoAltEnter));

        for (uint i = 0; i < RenderLayout.FramesInFlight; i++)
        {
            ComPtr<ID3D12Resource> buffer = default;
            Check(swapChain.Handle->GetBuffer(i, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)buffer.GetAddressOf()));
            backBuffers.Add(buffer);
        }
    }

    public override void DestroySwapChain()
    {
        foreach (ComPtr<ID3D12Resource> buffer in backBuffers)
        {
            buffer.Dispose();
        }

        backBuffers.Clear();
        swapChain.Dispose();
    }

    public override void WaitRenderedFrame(int slot)
    {
        ulong value = slots[slot].Fence;
        if (fence.Handle->GetCompletedValue() < value)
        {
            Check(fence.Handle->SetEventOnCompletion(value, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }

        Check(presentQueue.Handle->Wait(fence.Handle, value));
    }

    public override bool PresentImage(GpuImage image)
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
        Check(swapChain.Handle->Present(0, tearingSupported ? DXGI.PresentAllowTearing : 0));

        return true;
    }

    public override void WaitPresentation()
    {
        if (presentFence.Handle != null && presentFence.Handle->GetCompletedValue() < presentFenceValue)
        {
            Check(presentFence.Handle->SetEventOnCompletion(presentFenceValue, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }
    }

    public override void WaitIdle()
    {
        if (queue.Handle != null && fence.Handle != null)
        {
            Check(queue.Handle->Signal(fence.Handle, ++fenceValue));
            WaitFence(fenceValue);
        }

        if (presentQueue.Handle != null && presentFence.Handle != null)
        {
            Check(presentQueue.Handle->Signal(presentFence.Handle, ++presentFenceValue));
            WaitPresentation();
        }
    }

    public override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ResourceFlags flags = format is ImageFormat.Depth ? ResourceFlags.AllowDepthStencil : ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess;
        Format resourceFormat = format is ImageFormat.Depth ? Format.FormatR32Typeless : NativeFormat(format);
        ResourceDesc description = new()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (uint)width,
            Height = (uint)height,
            DepthOrArraySize = (ushort)layers,
            MipLevels = 1,
            Format = resourceFormat,
            SampleDesc = new(1, 0),
            Flags = flags
        };

        return new DxImage
        {
            Width = width,
            Height = height,
            Layers = layers,
            Format = format,
            Texture = CreateResource(HeapType.Default, description, ResourceStates.Common),
            State = ResourceStates.Common
        };
    }

    public override void DrawUI(ImDrawDataPtr data)
    {
        DxFrame frame = slots[Frame.Slot];
        EnsureUpload(ref frame.Vertices, ref frame.VertexCapacity, data.TotalVtxCount * sizeof(ImDrawVert));
        EnsureUpload(ref frame.Indices, ref frame.IndexCapacity, data.TotalIdxCount * sizeof(ushort));
        int vertexOffset = 0;
        int indexOffset = 0;
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
                int left = Math.Max(0, (int)clip.X);
                int top = Math.Max(0, (int)clip.Y);
                int right = Math.Min(Resources.OutputWidth, (int)clip.Z);
                int bottom = Math.Min(Resources.OutputHeight, (int)clip.W);
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

    protected override void InitializeDevice()
    {
        d3d12 = D3D12.GetApi();
        dxgi = DXGI.GetApi(null);

        Check(dxgi.CreateDXGIFactory2(0, SilkMarshal.GuidPtrOf<IDXGIFactory4>(), (void**)factory.GetAddressOf()));
        using ComPtr<IDXGIFactory5> presentationFactory = default;
        int allowTearing = 0;
        tearingSupported = factory.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIFactory5>(), (void**)presentationFactory.GetAddressOf()) >= 0 && presentationFactory.Handle->CheckFeatureSupport(Silk.NET.DXGI.Feature.PresentAllowTearing, &allowTearing, sizeof(int)) >= 0 && allowTearing is not 0;
        bool nvidiaSelected = false;
        for (uint i = 0; ; i++)
        {
            ComPtr<IDXGIAdapter1> candidate = default;
            if (factory.Handle->EnumAdapters1(i, candidate.GetAddressOf()) < 0)
            {
                break;
            }

            try
            {
                AdapterDesc1 description = default;
                Check(candidate.Handle->GetDesc1(&description));
                bool nvidia = description.VendorId is 0x10DE;
                if ((description.Flags & (uint)AdapterFlag.Software) is 0 && (adapter.Handle == null || (!nvidiaSelected && nvidia)))
                {
                    adapter.Dispose();
                    adapter = candidate;
                    candidate = default;
                    nvidiaSelected = nvidia;
                    AdapterName = NGXMarshal.PtrToString(description.Description, NGXEncoding.NativeWide)!;
                }
            }
            finally
            {
                candidate.Dispose();
            }
        }

        if (adapter.Handle == null)
        {
            throw new InvalidOperationException("No hardware graphics adapter found.");
        }

        Check(d3d12.CreateDevice((IUnknown*)adapter.Handle, D3DFeatureLevel.Level120, SilkMarshal.GuidPtrOf<ID3D12Device>(), (void**)device.GetAddressOf()));
        NGX.Initialize((nint)device.Handle);
        FeatureDataD3D12Options5 options = default;
        Check(device.Handle->CheckFeatureSupport(Silk.NET.Direct3D12.Feature.D3D12Options5, &options, (uint)sizeof(FeatureDataD3D12Options5)));
        RayQuerySupported = options.RaytracingTier >= RaytracingTier.Tier11;
        RayQueryStatus = RayQuerySupported ? "DXR 1.1" : "Requires DXR tier 1.1";

        if (RayQuerySupported)
        {
            Check(device.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Device5>(), (void**)rayDevice.GetAddressOf()));
        }

        CommandQueueDesc queueDescription = new()
        {
            Type = CommandListType.Direct
        };
        Check(device.Handle->CreateCommandQueue(&queueDescription, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)queue.GetAddressOf()));
        Check(device.Handle->CreateCommandQueue(&queueDescription, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)presentQueue.GetAddressOf()));
        Check(device.Handle->CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)fence.GetAddressOf()));
        presentAllocator = CreateAllocator();
        presentCommands = CreateCommands(presentAllocator);
        Check(presentCommands.Handle->Close());
        Check(device.Handle->CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)presentFence.GetAddressOf()));
    }

    protected override void DisposeDevice()
    {
        if (recording)
        {
            commandList.Handle->Close();
            recording = false;
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

    protected override void InitializeRendererCore()
    {
        descriptors = CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, RenderLayout.FramesInFlight * DescriptorsPerFrame, DescriptorHeapFlags.ShaderVisible);
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
            frame.Objects = UploadBuffer(Resources.Scene.Objects.Length * sizeof(SceneObject));
        }

        commandList = CreateCommands(slots[0].Allocator);
        recording = true;

        if (RayQuerySupported)
        {
            Check(commandList.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList4>(), (void**)rayCommands.GetAddressOf()));
        }

        sceneBuffers[0] = StaticBuffer<SceneVertex>(Resources.Scene.Vertices);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        UploadFont();
        ExecuteUploads();
        InitializePipelines();
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

    private CpuDescriptorHandle Cpu(int frame, int index)
    {
        return new()
        {
            Ptr = descriptors.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)(((frame * DescriptorsPerFrame) + index) * descriptorIncrement)
        };
    }

    private GpuDescriptorHandle Gpu(int frame, int index)
    {
        return new()
        {
            Ptr = descriptors.Handle->GetGPUDescriptorHandleForHeapStart().Ptr + (ulong)(((frame * DescriptorsPerFrame) + index) * descriptorIncrement)
        };
    }

    private void CreateSrv(DxImage image, CpuDescriptorHandle descriptor)
    {
        ShaderResourceViewDesc description = new()
        {
            Format = image.Format is ImageFormat.Depth ? Format.FormatR32Float : NativeFormat(image.Format),
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

    private void UploadFont()
    {
        DxImage replacement = (DxImage)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        int rowPitch = ((UI.FontWidth * 4) + 255) & ~255;
        ComPtr<ID3D12Resource> fontUpload = UploadBuffer(rowPitch * UI.FontHeight);
        uploads.Add(fontUpload);
        byte* mapped = Map<byte>(fontUpload);
        for (int row = 0; row < UI.FontHeight; row++)
        {
            UI.FontPixels.AsSpan(row * UI.FontWidth * 4, UI.FontWidth * 4).CopyTo(new Span<byte>(mapped + (row * rowPitch), UI.FontWidth * 4));
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
        TextureCopyLocation source = new()
        {
            PResource = fontUpload.Handle,
            Type = TextureCopyType.PlacedFootprint,
            PlacedFootprint = footprint
        };
        TextureCopyLocation destination = new()
        {
            PResource = font.Texture.Handle,
            Type = TextureCopyType.SubresourceIndex
        };
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

    private ComPtr<ID3D12Resource> CreateResource(HeapType heap, ResourceDesc description, ResourceStates state)
    {
        HeapProperties properties = new()
        {
            Type = heap,
            CreationNodeMask = 1,
            VisibleNodeMask = 1
        };

        ComPtr<ID3D12Resource> resource = default;
        Check(device.Handle->CreateCommittedResource(&properties, HeapFlags.None, &description, state, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)resource.GetAddressOf()));

        return resource;
    }

    private ComPtr<ID3D12Resource> CreateBuffer(ulong size, HeapType heap, ResourceStates state, ResourceFlags flags = ResourceFlags.None)
    {
        ResourceDesc description = new()
        {
            Dimension = ResourceDimension.Buffer,
            Width = size,
            Height = 1,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new(1, 0),
            Layout = TextureLayout.LayoutRowMajor,
            Flags = flags
        };

        return CreateResource(heap, description, state);
    }

    private ComPtr<ID3D12CommandAllocator> CreateAllocator()
    {
        ComPtr<ID3D12CommandAllocator> allocator = default;
        Check(device.Handle->CreateCommandAllocator(CommandListType.Direct, SilkMarshal.GuidPtrOf<ID3D12CommandAllocator>(), (void**)allocator.GetAddressOf()));

        return allocator;
    }

    private ComPtr<ID3D12GraphicsCommandList> CreateCommands(ComPtr<ID3D12CommandAllocator> allocator)
    {
        ComPtr<ID3D12GraphicsCommandList> commands = default;
        Check(device.Handle->CreateCommandList(0, CommandListType.Direct, allocator.Handle, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)commands.GetAddressOf()));

        return commands;
    }

    private ComPtr<ID3D12DescriptorHeap> CreateDescriptorHeap(DescriptorHeapType type, int count, DescriptorHeapFlags flags = DescriptorHeapFlags.None)
    {
        DescriptorHeapDesc description = new()
        {
            Type = type,
            NumDescriptors = (uint)count,
            Flags = flags
        };

        ComPtr<ID3D12DescriptorHeap> heap = default;
        Check(device.Handle->CreateDescriptorHeap(&description, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)heap.GetAddressOf()));

        return heap;
    }

    private void UavBarrier(ComPtr<ID3D12Resource> resource)
    {
        ResourceBarrier barrier = new()
        {
            Type = ResourceBarrierType.Uav,
            UAV = new()
            {
                PResource = resource.Handle
            }
        };

        commandList.Handle->ResourceBarrier(1, &barrier);
    }

    private void SetViewport(int width, int height)
    {
        Viewport viewport = new(0, 0, width, height, 0, 1);
        commandList.Handle->RSSetViewports(1, &viewport);
    }

    private void SetScissor(int left, int top, int right, int bottom)
    {
        Silk.NET.Maths.Box2D<int> rectangle = new(left, top, right, bottom);
        commandList.Handle->RSSetScissorRects(1, &rectangle);
    }

    private void ClearColor(CpuDescriptorHandle handle)
    {
        float* color = stackalloc float[4];
        new Span<float>(color, 4).Clear();
        commandList.Handle->ClearRenderTargetView(handle, color, 0, null);
    }

    private void InitializePipelines()
    {
        DescriptorRange1* ranges = stackalloc DescriptorRange1[2];
        ranges[0] = new()
        {
            RangeType = DescriptorRangeType.Srv,
            NumDescriptors = RenderLayout.SrvCount,
            Flags = DescriptorRangeFlags.DataVolatile,
            OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend
        };
        ranges[1] = new()
        {
            RangeType = DescriptorRangeType.Uav,
            NumDescriptors = RenderLayout.UavCount,
            Flags = DescriptorRangeFlags.DataVolatile,
            OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend
        };

        RootParameter1* parameters = stackalloc RootParameter1[3];
        parameters[0] = new()
        {
            ParameterType = RootParameterType.TypeCbv,
            Descriptor = new()
            {
                Flags = RootDescriptorFlags.DataVolatile
            },
            ShaderVisibility = ShaderVisibility.All
        };
        parameters[1] = new()
        {
            ParameterType = RootParameterType.TypeDescriptorTable,
            DescriptorTable = new()
            {
                NumDescriptorRanges = 1,
                PDescriptorRanges = &ranges[0]
            },
            ShaderVisibility = ShaderVisibility.All
        };
        parameters[2] = new()
        {
            ParameterType = RootParameterType.TypeDescriptorTable,
            DescriptorTable = new()
            {
                NumDescriptorRanges = 1,
                PDescriptorRanges = &ranges[1]
            },
            ShaderVisibility = ShaderVisibility.All
        };

        StaticSamplerDesc sampler = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MaxAnisotropy = 1,
            ComparisonFunc = ComparisonFunc.Never,
            BorderColor = StaticBorderColor.TransparentBlack,
            MinLOD = float.MinValue,
            MaxLOD = float.MaxValue,
            ShaderVisibility = ShaderVisibility.All
        };

        VersionedRootSignatureDesc description = new()
        {
            Version = D3DRootSignatureVersion.Version11,
            Desc11 = new()
            {
                NumParameters = 3,
                PParameters = parameters,
                NumStaticSamplers = 1,
                PStaticSamplers = &sampler,
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
            }
        };

        using ComPtr<ID3D10Blob> serialized = default;
        using ComPtr<ID3D10Blob> errors = default;

        Check(d3d12.SerializeVersionedRootSignature(&description, serialized.GetAddressOf(), errors.GetAddressOf()));
        Check(device.Handle->CreateRootSignature(0, serialized.Handle->GetBufferPointer(), serialized.Handle->GetBufferSize(), SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)root.GetAddressOf()));

        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in RenderLayout.ComputePasses)
        {
            byte[] shader = Compile(pass.ToString(), "compute");

            fixed (byte* code = shader)
            {
                ComputePipelineStateDesc compute = new()
                {
                    PRootSignature = root.Handle,
                    CS = new(code, (nuint)shader.Length)
                };

                ComPtr<ID3D12PipelineState> pipeline = default;
                Check(device.Handle->CreateComputePipelineState(&compute, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf()));
                pipelines[pass] = pipeline;
            }
        }
    }

    private ComPtr<ID3D12PipelineState> GraphicsPipeline(GraphicsPass pass)
    {
        bool ui = pass is GraphicsPass.UI;
        (string vertex, string fragment) = RenderLayout.Shaders(pass);
        byte[] vertexCode = Compile(vertex, "vertex");
        byte[] fragmentCode = Compile(fragment, "fragment");
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        DepthStencilopDesc stencil = new()
        {
            StencilFailOp = StencilOp.Keep,
            StencilDepthFailOp = StencilOp.Keep,
            StencilPassOp = StencilOp.Keep,
            StencilFunc = ComparisonFunc.Always
        };

        GraphicsPipelineStateDesc description = new()
        {
            PRootSignature = root.Handle,
            SampleMask = uint.MaxValue,
            SampleDesc = new(1, 0),
            RasterizerState = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontCounterClockwise = !ui,
                DepthClipEnable = true
            },
            DepthStencilState = new()
            {
                DepthEnable = !ui,
                DepthWriteMask = pass is GraphicsPass.Depth or GraphicsPass.Shadow ? DepthWriteMask.All : DepthWriteMask.Zero,
                DepthFunc = pass switch
                {
                    GraphicsPass.Scene => ComparisonFunc.Equal,
                    GraphicsPass.Depth => ComparisonFunc.Greater,
                    _ => ComparisonFunc.LessEqual
                },
                StencilReadMask = byte.MaxValue,
                StencilWriteMask = byte.MaxValue,
                FrontFace = stencil,
                BackFace = stencil
            },
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            NumRenderTargets = (uint)targets.Length,
            DSVFormat = ui ? Format.FormatUnknown : Format.FormatD32Float
        };

        description.BlendState.RenderTarget[0] = new()
        {
            BlendEnable = ui,
            SrcBlend = Blend.One,
            DestBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DestBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            LogicOp = LogicOp.Noop,
            RenderTargetWriteMask = (byte)ColorWriteEnable.All
        };

        for (int i = 0; i < targets.Length; i++)
        {
            description.RTVFormats[i] = NativeFormat(RenderLayout.Format(targets[i]));
        }

        using NativeNames semantics = new(["POSITION", "TEXCOORD", "COLOR"]);

        fixed (byte* vs = vertexCode, ps = fragmentCode)
        {
            InputElementDesc* elements = stackalloc InputElementDesc[3];
            elements[0] = new()
            {
                SemanticName = semantics.Pointer[0],
                Format = Format.FormatR32G32Float
            };
            elements[1] = new()
            {
                SemanticName = semantics.Pointer[1],
                Format = Format.FormatR32G32Float,
                AlignedByteOffset = 8
            };
            elements[2] = new()
            {
                SemanticName = semantics.Pointer[2],
                Format = Format.FormatR8G8B8A8Unorm,
                AlignedByteOffset = 16
            };

            description.VS = new(vs, (nuint)vertexCode.Length);
            description.PS = new(ps, (nuint)fragmentCode.Length);
            description.InputLayout = new()
            {
                PInputElementDescs = ui ? elements : null,
                NumElements = ui ? 3u : 0u
            };

            ComPtr<ID3D12PipelineState> pipeline = default;
            Check(device.Handle->CreateGraphicsPipelineState(&description, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf()));

            return pipeline;
        }
    }

    private byte[] Compile(string entry, string stage)
    {
        return ShaderCompiler.Compile("Scene.slang", entry, stage, false, RayQuerySupported);
    }

    private void WaitFence(ulong value)
    {
        if (value is 0 || fence.Handle->GetCompletedValue() >= value)
        {
            return;
        }

        Check(fence.Handle->SetEventOnCompletion(value, (void*)fenceEvent.SafeWaitHandle.DangerousGetHandle()));
        fenceEvent.WaitOne();
    }

    private ComPtr<ID3D12Resource> AccelerationBuffer(ulong size, ResourceStates state)
    {
        const ulong Alignment = D3D12.RaytracingAccelerationStructureByteAlignment;
        ulong alignedSize = (size + Alignment - 1) / Alignment * Alignment;

        return CreateBuffer(alignedSize, HeapType.Default, state, ResourceFlags.AllowUnorderedAccess);
    }

    private void InitializeAccelerationStructures()
    {
        foreach (SceneObject instance in Resources.Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            RaytracingGeometryDesc geometry = new()
            {
                Type = RaytracingGeometryType.Triangles,
                Flags = range.Opaque is not 0 ? RaytracingGeometryFlags.Opaque : RaytracingGeometryFlags.None,
                Triangles = new()
                {
                    VertexFormat = Format.FormatR32G32B32Float,
                    VertexCount = range.VertexCount,
                    VertexBuffer = new()
                    {
                        StartAddress = sceneBuffers[0].Handle->GetGPUVirtualAddress() + ((ulong)range.FirstVertex * (uint)sizeof(SceneVertex)),
                        StrideInBytes = (uint)sizeof(SceneVertex)
                    }
                }
            };

            BuildRaytracingAccelerationStructureInputs inputs = new()
            {
                Type = RaytracingAccelerationStructureType.BottomLevel,
                Flags = RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
                DescsLayout = ElementsLayout.Array,
                NumDescs = 1,
                PGeometryDescs = &geometry
            };

            RaytracingAccelerationStructurePrebuildInfo sizes = default;
            rayDevice.Handle->GetRaytracingAccelerationStructurePrebuildInfo(&inputs, &sizes);
            ComPtr<ID3D12Resource> bottom = AccelerationBuffer(sizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            bottomLevels.Add(bottom);
            ComPtr<ID3D12Resource> scratch = AccelerationBuffer(sizes.ScratchDataSizeInBytes, ResourceStates.UnorderedAccess);

            // Startup submission owns this scratch storage until WaitIdle releases the upload batch.
            uploads.Add(scratch);
            BuildRaytracingAccelerationStructureDesc build = new()
            {
                Inputs = inputs,
                DestAccelerationStructureData = bottom.Handle->GetGPUVirtualAddress(),
                ScratchAccelerationStructureData = scratch.Handle->GetGPUVirtualAddress()
            };
            rayCommands.Handle->BuildRaytracingAccelerationStructure(&build, 0, null);
            UavBarrier(bottom);
        }

        BuildRaytracingAccelerationStructureInputs topInputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            DescsLayout = ElementsLayout.Array,
            NumDescs = (uint)Resources.Scene.Objects.Length
        };

        RaytracingAccelerationStructurePrebuildInfo topSizes = default;
        rayDevice.Handle->GetRaytracingAccelerationStructurePrebuildInfo(&topInputs, &topSizes);

        foreach (DxFrame frame in slots)
        {
            frame.Tlas = AccelerationBuffer(topSizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            frame.RayScratch = AccelerationBuffer(Math.Max(topSizes.ScratchDataSizeInBytes, topSizes.UpdateScratchDataSizeInBytes), ResourceStates.UnorderedAccess);
            frame.RayInstances = UploadBuffer(Resources.Scene.Objects.Length * sizeof(RaytracingInstanceDesc));
        }
    }

    private void UpdateAccelerationStructure(DxFrame frame)
    {
        // BeginCommands has already waited for this frame slot's fence. Other slots
        // retain their own TLAS, scratch and instance data until their GPU work ends.
        Span<RaytracingInstanceDesc> instances = new(Map<RaytracingInstanceDesc>(frame.RayInstances), Resources.Scene.Objects.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Resources.Scene.Objects[i], (uint)i, bottomLevels[i].Handle->GetGPUVirtualAddress());
        }

        frame.RayInstances.Handle->Unmap(0, null);

        if (frame.TlasBuilt)
        {
            UavBarrier(frame.Tlas);
            UavBarrier(frame.RayScratch);
        }

        BuildRaytracingAccelerationStructureInputs inputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            DescsLayout = ElementsLayout.Array,
            NumDescs = (uint)Resources.Scene.Objects.Length,
            InstanceDescs = frame.RayInstances.Handle->GetGPUVirtualAddress()
        };
        if (frame.TlasBuilt)
        {
            inputs.Flags |= RaytracingAccelerationStructureBuildFlags.PerformUpdate;
        }

        BuildRaytracingAccelerationStructureDesc build = new()
        {
            Inputs = inputs,
            DestAccelerationStructureData = frame.Tlas.Handle->GetGPUVirtualAddress(),
            SourceAccelerationStructureData = frame.TlasBuilt ? frame.Tlas.Handle->GetGPUVirtualAddress() : 0,
            ScratchAccelerationStructureData = frame.RayScratch.Handle->GetGPUVirtualAddress()
        };
        rayCommands.Handle->BuildRaytracingAccelerationStructure(&build, 0, null);
        UavBarrier(frame.Tlas);
        frame.TlasBuilt = true;
    }

    private ComPtr<ID3D12Resource> UploadBuffer(int bytes)
    {
        return CreateBuffer((ulong)Math.Max(bytes, 4), HeapType.Upload, ResourceStates.GenericRead);
    }

    private ComPtr<ID3D12Resource> StaticBuffer<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        int size = data.Length * sizeof(T);
        ComPtr<ID3D12Resource> buffer = CreateBuffer((ulong)size, HeapType.Default, ResourceStates.CopyDest);
        try
        {
            ComPtr<ID3D12Resource> upload = UploadBuffer(size);
            uploads.Add(upload);
            SetData(upload, data);
            commandList.Handle->CopyBufferRegion(buffer.Handle, 0, upload.Handle, 0, (ulong)size);
            TransitionBarrier(commandList, buffer, ResourceStates.CopyDest, ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource);

            return buffer;
        }
        catch
        {
            buffer.Dispose();

            throw;
        }
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

    private static void Check(int result)
    {
        Marshal.ThrowExceptionForHR(result);
    }

    private static T* Map<T>(ComPtr<ID3D12Resource> resource)
        where T : unmanaged
    {
        void* pointer = null;
        Silk.NET.Direct3D12.Range readRange = default;
        Check(resource.Handle->Map(0, &readRange, &pointer));

        return (T*)pointer;
    }

    private static void SetData<T>(ComPtr<ID3D12Resource> resource, ReadOnlySpan<T> values, int byteOffset = 0)
        where T : unmanaged
    {
        T* pointer = (T*)((byte*)Map<byte>(resource) + byteOffset);
        values.CopyTo(new Span<T>(pointer, values.Length));
        resource.Handle->Unmap(0, null);
    }

    private static void Execute(ComPtr<ID3D12CommandQueue> target, ComPtr<ID3D12GraphicsCommandList> commands)
    {
        ID3D12CommandList* list = (ID3D12CommandList*)commands.Handle;
        target.Handle->ExecuteCommandLists(1, &list);
    }

    private static void TransitionBarrier(ComPtr<ID3D12GraphicsCommandList> commands, ComPtr<ID3D12Resource> resource, ResourceStates before, ResourceStates after)
    {
        ResourceBarrier barrier = new()
        {
            Type = ResourceBarrierType.Transition,
            Transition = new()
            {
                PResource = resource.Handle,
                Subresource = D3D12.ResourceBarrierAllSubresources,
                StateBefore = before,
                StateAfter = after
            }
        };

        commands.Handle->ResourceBarrier(1, &barrier);
    }

    private static RaytracingInstanceDesc CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        System.Numerics.Vector4 offset = instance.Offset;

        RaytracingInstanceDesc description = new()
        {
            InstanceID = id,
            InstanceMask = byte.MaxValue,
            // Default ray facing matches dot(direction, cross(edge1, edge2)) < 0.
            Flags = instance.Geometry.DoubleSided is not 0 ? (uint)RaytracingInstanceFlags.TriangleCullDisable : 0,
            AccelerationStructure = address
        };
        description.Transform[0] = 1;
        description.Transform[3] = offset.X;
        description.Transform[5] = 1;
        description.Transform[7] = offset.Y;
        description.Transform[10] = 1;
        description.Transform[11] = offset.Z;

        return description;
    }

    private static Format NativeFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba16 => Format.FormatR16G16B16A16Float,
            ImageFormat.Rgba32 => Format.FormatR32G32B32A32Float,
            ImageFormat.Rg16 => Format.FormatR16G16Float,
            ImageFormat.Float => Format.FormatR32Float,
            ImageFormat.Depth => Format.FormatD32Float,
            _ => Format.FormatR8G8B8A8Unorm
        };
    }

    private class DxFrame : IDisposable
    {
        public ComPtr<ID3D12CommandAllocator> Allocator;

        public ComPtr<ID3D12Resource> Constants;

        public ComPtr<ID3D12Resource> Objects;

        public ComPtr<ID3D12Resource> Vertices;

        public ComPtr<ID3D12Resource> Indices;

        public int VertexCapacity;

        public int IndexCapacity;

        public ulong Fence;

        public ComPtr<ID3D12Resource> Tlas;

        public ComPtr<ID3D12Resource> RayScratch;

        public ComPtr<ID3D12Resource> RayInstances;

        public bool TlasBuilt;

        public void Dispose()
        {
            Tlas.Dispose();
            RayScratch.Dispose();
            RayInstances.Dispose();
            Vertices.Dispose();
            Indices.Dispose();
            Constants.Dispose();
            Objects.Dispose();
            Allocator.Dispose();
        }
    }

    private class DxImage : GpuImage
    {
        public required ComPtr<ID3D12Resource> Texture;

        public ResourceStates State;

        public CpuDescriptorHandle Rtv;

        public CpuDescriptorHandle Dsv;

        public override NativeImage Describe()
        {
            return new()
            {
                DirectX = (nint)Texture.Handle
            };
        }

        public override void Dispose()
        {
            Texture.Dispose();
        }
    }
}
