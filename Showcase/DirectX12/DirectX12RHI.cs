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
    private const int MinUploadCapacityInBytes = 4096;
    // Silk does not emit the SDK macros D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING and DXGI_MWA_NO_ALT_ENTER.
    private const uint ShaderComponentMapping = 0x1688u;
    private const uint NoAltEnter = 0x2u;

    private static readonly uint[] SceneBufferStridesInBytes = [(uint)sizeof(SceneVertex), (uint)sizeof(SceneMaterial), sizeof(uint), (uint)sizeof(TextureDescription), (uint)sizeof(SceneObject), (uint)sizeof(SceneAreaLight)];

    private readonly AutoResetEvent presentEvent = new(false);
    private readonly Dictionary<ComputePass, ComPtr<ID3D12PipelineState>> pipelines = [];
    private readonly DxFrame[] slots = new DxFrame[RenderLayout.FramesInFlight];
    private readonly ComPtr<ID3D12Resource>[] sceneBuffers = new ComPtr<ID3D12Resource>[5];
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
    private ComPtr<ID3D12PipelineState> uiPipeline;
    private ComPtr<ID3D12DescriptorHeap> descriptors;
    private ComPtr<ID3D12DescriptorHeap> renderTargets;
    private DxImage? font;
    private uint descriptorIncrement;
    private uint rtvIncrement;
    private ulong fenceValue;
    private int constantIndex;
    private bool isRecording;
    private D3D12 d3d12 = null!;
    private DXGI dxgi = null!;
    private ComPtr<ID3D12Device5> rayDevice;
    private ComPtr<ID3D12GraphicsCommandList4> rayCommands;

    public override nint Command => (nint)commandList.Handle;

    public override void CreateSwapChain()
    {
        SwapChainDesc1 desc = new()
        {
            Width = (uint)Resources.OutputWidth,
            Height = (uint)Resources.OutputHeight,
            Format = Format.FormatR8G8B8A8Unorm,
            SampleDesc = new(1, 0),
            BufferUsage = DXGI.UsageRenderTargetOutput,
            BufferCount = RenderLayout.FramesInFlight,
            Scaling = Scaling.Stretch,
            SwapEffect = SwapEffect.FlipDiscard,
            AlphaMode = AlphaMode.Ignore,
            Flags = tearingSupported ? (uint)SwapChainFlag.AllowTearing : 0u
        };

        using ComPtr<IDXGISwapChain1> created = default;
        Check(factory.Handle->CreateSwapChainForHwnd((IUnknown*)presentQueue.Handle, Window.Native!.Win32!.Value.Hwnd, &desc, null, null, created.GetAddressOf()));

        fixed (IDXGISwapChain3** pSwapChain = &swapChain.Handle)
        {
            Check(created.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**)pSwapChain));
        }

        Check(factory.Handle->MakeWindowAssociation(Window.Native!.Win32!.Value.Hwnd, NoAltEnter));

        for (uint i = 0u; i < RenderLayout.FramesInFlight; i++)
        {
            ComPtr<ID3D12Resource> buffer = default;
            Check(swapChain.Handle->GetBuffer(i, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)buffer.GetAddressOf()));
            backBuffers.Add(buffer);
        }
    }

    public override void DestroySwapChain()
    {
        if (backBuffers is not null)
        {
            for (int i = backBuffers.Count - 1; i >= 0; i--)
            {
                backBuffers[i].Dispose();
            }
            backBuffers.Clear();
        }

        swapChain.Dispose();
        swapChain = default;
    }

    public override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ResourceFlags flags = ResourceFlags.AllowUnorderedAccess;
        if (format is ImageFormat.Rgba8)
        {
            flags |= ResourceFlags.AllowRenderTarget;
        }

        ResourceDesc desc = new()
        {
            Dimension = ResourceDimension.Texture2D,
            Width = (uint)width,
            Height = (uint)height,
            DepthOrArraySize = (ushort)layers,
            MipLevels = 1,
            Format = ToNativeFormat(format),
            SampleDesc = new(1, 0),
            Flags = flags
        };

        return new DxImage
        {
            Width = width,
            Height = height,
            Format = format,
            Layers = layers,
            Texture = CreateResource(HeapType.Default, desc, ResourceStates.Common),
            State = ResourceStates.Common
        };
    }

    public override void UpdateDescriptors()
    {
        ReadOnlySpan<uint> counts = [(uint)Resources.Scene.Vertices.Length, (uint)Resources.Scene.Materials.Length, (uint)Resources.Scene.Texels.Length, (uint)Resources.Scene.TextureInfo.Length, (uint)Resources.Scene.Objects.Length, (uint)Math.Max(1, Resources.Scene.AreaLights.Length)];
        for (int frame = 0; frame < Resources.Frames.Length; frame++)
        {
            for (int i = 0; i < counts.Length; i++)
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
                ComPtr<ID3D12Resource> buffer = i is 4 ? slots[frame].Objects : sceneBuffers[i is 5 ? 4 : i];
                device.Handle->CreateShaderResourceView(buffer.Handle, &bufferView, GetCpuDescriptor(frame, i));
            }

            ShaderResourceViewDesc rayView = new()
            {
                ViewDimension = SrvDimension.RaytracingAccelerationStructure,
                Shader4ComponentMapping = ShaderComponentMapping,
                RaytracingAccelerationStructure = new()
                {
                    Location = slots[frame].Tlas.Handle->GetGPUVirtualAddress()
                }
            };
            device.Handle->CreateShaderResourceView(null, &rayView, GetCpuDescriptor(frame, 6));

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                DxImage image = (DxImage)Resources.Frames[frame][(int)slot];
                CreateSrv(image, GetCpuDescriptor(frame, 7 + (int)slot));

                if (slot is ImageSlot.UI)
                {
                    image.Rtv = new()
                    {
                        Ptr = renderTargets.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)(frame * rtvIncrement)
                    };
                    device.Handle->CreateRenderTargetView(image.Texture.Handle, null, image.Rtv);
                }
            }

            CreateSrv(font!, GetCpuDescriptor(frame, RenderLayout.FontSrv));

            for (AtmosphereImage slot = 0; slot < AtmosphereImage.Count; slot++)
            {
                DxImage image = (DxImage)Resources.AtmosphereTexture(slot);
                CreateSrv(image, GetCpuDescriptor(frame, RenderLayout.AtmosphereSrv + (int)slot));
                UnorderedAccessViewDesc storageView = new()
                {
                    Format = ToNativeFormat(image.Format),
                    ViewDimension = UavDimension.Texture2D
                };
                device.Handle->CreateUnorderedAccessView(image.Texture.Handle, null, &storageView, GetCpuDescriptor(frame, RenderLayout.SrvCount + RenderLayout.AtmosphereUav + (int)slot));
            }

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                DxImage image = (DxImage)Resources.Frames[frame][(int)RenderLayout.StorageImages[i]];
                UnorderedAccessViewDesc storageView = new()
                {
                    Format = ToNativeFormat(image.Format),
                    ViewDimension = UavDimension.Texture2D
                };
                device.Handle->CreateUnorderedAccessView(image.Texture.Handle, null, &storageView, GetCpuDescriptor(frame, RenderLayout.SrvCount + i));
            }

            UnorderedAccessViewDesc filmView = new()
            {
                Format = ToNativeFormat(Resources.Film.Format),
                ViewDimension = UavDimension.Texture2D
            };
            device.Handle->CreateUnorderedAccessView(((DxImage)Resources.Film).Texture.Handle, null, &filmView, GetCpuDescriptor(frame, RenderLayout.SrvCount + RenderLayout.FilmUav));
            UnorderedAccessViewDesc exposureView = new()
            {
                Format = Format.FormatR32Float,
                ViewDimension = UavDimension.Texture2D
            };
            device.Handle->CreateUnorderedAccessView(((DxImage)Resources.Exposure).Texture.Handle, null, &exposureView, GetCpuDescriptor(frame, RenderLayout.SrvCount + RenderLayout.ExposureUav));
        }
    }

    public override void UpdateFontTexture()
    {
        DxFrame frame = slots[Frame.Slot];
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        isRecording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    public override void UpdateRayTracingScene()
    {
        UpdateAccelerationStructure(slots[Frame.Slot]);
    }

    public override void DrawUI(ImDrawDataPtr data)
    {
        DxFrame frame = slots[Frame.Slot];
        EnsureUpload(ref frame.Vertices, ref frame.VertexCapacityInBytes, data.TotalVtxCount * sizeof(ImDrawVert));
        EnsureUpload(ref frame.Indices, ref frame.IndexCapacityInBytes, data.TotalIdxCount * sizeof(ushort));
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
        Bind(useGraphics: true, uiPipeline, in uiConstants);
        CpuDescriptorHandle rtv = image.Rtv;
        commandList.Handle->OMSetRenderTargets(1u, &rtv, false, null);
        SetViewport(Resources.OutputWidth, Resources.OutputHeight);
        VertexBufferView vertices = new(frame.Vertices.Handle->GetGPUVirtualAddress(), (uint)(data.TotalVtxCount * sizeof(ImDrawVert)), (uint)sizeof(ImDrawVert));
        commandList.Handle->IASetVertexBuffers(0u, 1u, &vertices);
        IndexBufferView indices = new(frame.Indices.Handle->GetGPUVirtualAddress(), (uint)(data.TotalIdxCount * sizeof(ushort)), Format.FormatR16Uint);
        commandList.Handle->IASetIndexBuffer(&indices);
        vertexOffset = 0;
        indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            for (int j = 0; j < list.CmdBuffer.Size; j++)
            {
                ref readonly ImDrawCmd draw = ref list.CmdBuffer.Data[j];
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
                commandList.Handle->DrawIndexedInstanced(draw.ElemCount, 1u, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0u);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        commandList.Handle->OMSetRenderTargets(0u, null, false, null);
    }

    public override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(useGraphics: false, pipelines[pass], in constants);
        commandList.Handle->Dispatch((uint)(width + 7) / 8u, (uint)(height + 7) / 8u, (uint)groupsZ);
    }

    public override void Transition(GpuImage image, ImageUse use)
    {
        DxImage texture = (DxImage)image;
        ResourceStates state = use switch
        {
            ImageUse.Storage => ResourceStates.UnorderedAccess,
            ImageUse.ColorAttachment => ResourceStates.RenderTarget,
            ImageUse.CopySource => ResourceStates.CopySource,
            ImageUse.CopyDestination => ResourceStates.CopyDest,
            _ => ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource
        };
        if (texture.State != state)
        {
            TransitionBarrier(commandList, texture.Texture, texture.State, state);
        }
        else if (state is ResourceStates.UnorderedAccess)
        {
            // Shared film pixels are read and written on consecutive frames.
            UavBarrier(texture.Texture);
        }

        texture.State = state;
    }

    public override void SubmitFrame()
    {
        Check(commandList.Handle->Close());
        isRecording = false;
        Execute(queue, commandList);
        slots[Frame.Slot].FenceValue = ++fenceValue;
        Check(queue.Handle->Signal(fence.Handle, fenceValue));
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

        // Signal the copy fence before Present to avoid waiting for DXGI presentation work.
        Check(presentQueue.Handle->Signal(presentFence.Handle, ++presentFenceValue));
        Check(swapChain.Handle->Present(0u, tearingSupported ? DXGI.PresentAllowTearing : 0u));

        return true;
    }

    public override void WaitRenderedFrame(int slot)
    {
        ulong value = slots[slot].FenceValue;
        if (fence.Handle->GetCompletedValue() < value)
        {
            Check(fence.Handle->SetEventOnCompletion(value, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }

        Check(presentQueue.Handle->Wait(fence.Handle, value));
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

    protected override void InitializeDevice()
    {
        d3d12 = D3D12.GetApi();
        dxgi = DXGI.GetApi(null);

        fixed (IDXGIFactory4** pFactory = &factory.Handle)
        {
            Check(dxgi.CreateDXGIFactory2(0u, SilkMarshal.GuidPtrOf<IDXGIFactory4>(), (void**)pFactory));
        }

        using ComPtr<IDXGIFactory5> presentationFactory = default;
        int allowTearing = 0;
        tearingSupported = factory.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGIFactory5>(), (void**)presentationFactory.GetAddressOf()) >= 0 && presentationFactory.Handle->CheckFeatureSupport(Silk.NET.DXGI.Feature.PresentAllowTearing, &allowTearing, sizeof(int)) >= 0 && allowTearing is not 0;
        bool isNvidiaSelected = false;
        for (uint i = 0u; ; i++)
        {
            ComPtr<IDXGIAdapter1> candidate = default;
            if (factory.Handle->EnumAdapters1(i, candidate.GetAddressOf()) < 0)
            {
                break;
            }

            try
            {
                AdapterDesc1 desc = default;
                Check(candidate.Handle->GetDesc1(&desc));
                bool isNvidia = desc.VendorId is 0x10DEu;
                if ((desc.Flags & (uint)AdapterFlag.Software) is 0u && (adapter.Handle == null || (!isNvidiaSelected && isNvidia)))
                {
                    adapter.Dispose();
                    adapter = candidate;
                    candidate = default;
                    isNvidiaSelected = isNvidia;
                    AdapterName = Marshal.PtrToStringUni((nint)desc.Description)!;
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

        // Scene loading can move this managed owner while native creation is in progress.
        fixed (ID3D12Device** pDevice = &device.Handle)
        {
            Check(d3d12.CreateDevice((IUnknown*)adapter.Handle, D3DFeatureLevel.Level120, SilkMarshal.GuidPtrOf<ID3D12Device>(), (void**)pDevice));
        }

        FeatureDataD3D12Options5 options = default;
        Check(device.Handle->CheckFeatureSupport(Silk.NET.Direct3D12.Feature.D3D12Options5, &options, (uint)sizeof(FeatureDataD3D12Options5)));
        RayQuerySupported = options.RaytracingTier >= RaytracingTier.Tier11;
        RayQueryStatus = RayQuerySupported ? "DXR 1.1" : "Requires DXR tier 1.1";

        if (!RayQuerySupported)
        {
            throw new NotSupportedException("The path tracer requires DXR tier 1.1.");
        }

        ValidateImageFormats();
        NGX.Initialize((nint)device.Handle);

        fixed (ID3D12Device5** pRayDevice = &rayDevice.Handle)
        {
            Check(device.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Device5>(), (void**)pRayDevice));
        }

        CommandQueueDesc queueDesc = new()
        {
            Type = CommandListType.Direct
        };

        fixed (ID3D12CommandQueue** pQueue = &queue.Handle)
        {
            Check(device.Handle->CreateCommandQueue(&queueDesc, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)pQueue));
        }

        fixed (ID3D12CommandQueue** pPresentQueue = &presentQueue.Handle)
        {
            Check(device.Handle->CreateCommandQueue(&queueDesc, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)pPresentQueue));
        }

        fixed (ID3D12Fence** pFence = &fence.Handle)
        {
            Check(device.Handle->CreateFence(0UL, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)pFence));
        }

        presentAllocator = CreateAllocator();
        presentCommands = CreateCommands(presentAllocator);
        Check(presentCommands.Handle->Close());

        fixed (ID3D12Fence** pPresentFence = &presentFence.Handle)
        {
            Check(device.Handle->CreateFence(0UL, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)pPresentFence));
        }
    }

    protected override void InitializeRendererImpl()
    {
        descriptors = CreateDescriptorHeap(DescriptorHeapType.CbvSrvUav, RenderLayout.FramesInFlight * DescriptorsPerFrame, DescriptorHeapFlags.ShaderVisible);
        renderTargets = CreateDescriptorHeap(DescriptorHeapType.Rtv, RenderLayout.FramesInFlight);
        descriptorIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        rtvIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);

        for (int i = 0; i < slots.Length; i++)
        {
            DxFrame frame = slots[i] = new();
            frame.Allocator = CreateAllocator();
            frame.Constants = CreateUploadBuffer(RenderLayout.UniformStride * RenderLayout.UniformSlots);
            frame.Objects = CreateUploadBuffer(Resources.Scene.Objects.Length * sizeof(SceneObject));
        }

        commandList = CreateCommands(slots[0].Allocator);
        isRecording = true;

        fixed (ID3D12GraphicsCommandList4** pRayCommands = &rayCommands.Handle)
        {
            Check(commandList.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList4>(), (void**)pRayCommands));
        }

        sceneBuffers[0] = CreateStaticBuffer<SceneVertex>(Resources.Scene.Vertices);
        sceneBuffers[1] = CreateStaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = CreateStaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = CreateStaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);

        SceneAreaLight[] areaLights = Resources.Scene.AreaLights.Length is 0 ? [default] : Resources.Scene.AreaLights;
        sceneBuffers[4] = CreateStaticBuffer<SceneAreaLight>(areaLights);
        InitializeAccelerationStructures();

        UploadFont();
        ExecuteUploads();
        InitializePipelines();
    }

    protected override void BeginCommands()
    {
        DxFrame frame = slots[Frame.Slot];
        WaitFence(frame.FenceValue);
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        isRecording = true;
        SetData<SceneObject>(frame.Objects, Resources.Scene.Objects);
        constantIndex = 0;
    }

    protected override void DestroyDevice()
    {
        if (isRecording)
        {
            commandList.Handle->Close();
            isRecording = false;
        }

        DestroySwapChain();

        if (pipelines is not null)
        {
            for (int i = RenderLayout.ComputePasses.Length - 1; i >= 0; i--)
            {
                if (pipelines.Remove(RenderLayout.ComputePasses[i], out ComPtr<ID3D12PipelineState> pipeline))
                {
                    pipeline.Dispose();
                }
            }
        }

        uiPipeline.Dispose();
        root.Dispose();
        font?.Dispose();
        font = null;

        DestroyUploads();
        rayCommands.Dispose();
        commandList.Dispose();

        if (slots is not null)
        {
            for (int i = slots.Length - 1; i >= 0; i--)
            {
                slots[i]?.Dispose();
            }
        }

        if (bottomLevels is not null)
        {
            for (int i = bottomLevels.Count - 1; i >= 0; i--)
            {
                bottomLevels[i].Dispose();
            }
            bottomLevels.Clear();
        }

        if (sceneBuffers is not null)
        {
            for (int i = sceneBuffers.Length - 1; i >= 0; i--)
            {
                sceneBuffers[i].Dispose();
            }
        }

        renderTargets.Dispose();
        descriptors.Dispose();
        presentFence.Dispose();
        presentCommands.Dispose();
        presentAllocator.Dispose();
        fence.Dispose();
        presentQueue.Dispose();
        queue.Dispose();
        rayDevice.Dispose();
        device.Dispose();
        adapter.Dispose();
        factory.Dispose();
        dxgi?.Dispose();
        d3d12?.Dispose();
        fenceEvent?.Dispose();
        presentEvent?.Dispose();
    }

    private void ValidateImageFormats()
    {
        foreach (ImageFormat format in Enum.GetValues<ImageFormat>())
        {
            FeatureDataFormatSupport support = new()
            {
                Format = ToNativeFormat(format)
            };
            Check(device.Handle->CheckFeatureSupport(Silk.NET.Direct3D12.Feature.FormatSupport, &support, (uint)sizeof(FeatureDataFormatSupport)));
            FormatSupport1 required = FormatSupport1.Texture2D | FormatSupport1.ShaderLoad | FormatSupport1.TypedUnorderedAccessView;
            FormatSupport2 storage = FormatSupport2.UavTypedStore;
            if (format is ImageFormat.Rgba32 or ImageFormat.Float)
            {
                // Film and display exposure both read their previous values.
                storage |= FormatSupport2.UavTypedLoad;
            }

            if (format is ImageFormat.Rgba32)
            {
                required |= FormatSupport1.ShaderSample;
            }

            if (format is ImageFormat.Rgba8)
            {
                required |= FormatSupport1.ShaderSample | FormatSupport1.RenderTarget | FormatSupport1.Blendable;
            }

            if ((support.Support1 & required) != required || (support.Support2 & storage) != storage)
            {
                throw new NotSupportedException($"The path tracer requires texture, sampling and storage support for {support.Format}.");
            }
        }
    }

    private void Bind(bool useGraphics, ComPtr<ID3D12PipelineState> pipeline, in FrameConstants constants)
    {
        int offset = constantIndex++ * RenderLayout.UniformStride;

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        DxFrame frame = slots[Frame.Slot];
        SetData<FrameConstants>(frame.Constants, new ReadOnlySpan<FrameConstants>(in constants), offset);
        ID3D12DescriptorHeap* heap = descriptors.Handle;
        commandList.Handle->SetDescriptorHeaps(1u, &heap);
        commandList.Handle->SetPipelineState(pipeline.Handle);

        if (useGraphics)
        {
            commandList.Handle->SetGraphicsRootSignature(root.Handle);
            commandList.Handle->SetGraphicsRootConstantBufferView(0u, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetGraphicsRootDescriptorTable(1u, GetGpuDescriptor(Frame.Slot, 0));
            commandList.Handle->SetGraphicsRootDescriptorTable(2u, GetGpuDescriptor(Frame.Slot, RenderLayout.SrvCount));
            commandList.Handle->IASetPrimitiveTopology(D3DPrimitiveTopology.D3DPrimitiveTopologyTrianglelist);
        }
        else
        {
            commandList.Handle->SetComputeRootSignature(root.Handle);
            commandList.Handle->SetComputeRootConstantBufferView(0u, frame.Constants.Handle->GetGPUVirtualAddress() + (ulong)offset);
            commandList.Handle->SetComputeRootDescriptorTable(1u, GetGpuDescriptor(Frame.Slot, 0));
            commandList.Handle->SetComputeRootDescriptorTable(2u, GetGpuDescriptor(Frame.Slot, RenderLayout.SrvCount));
        }
    }

    private CpuDescriptorHandle GetCpuDescriptor(int frame, int index)
    {
        return new()
        {
            Ptr = descriptors.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)(((frame * DescriptorsPerFrame) + index) * descriptorIncrement)
        };
    }

    private GpuDescriptorHandle GetGpuDescriptor(int frame, int index)
    {
        return new()
        {
            Ptr = descriptors.Handle->GetGPUDescriptorHandleForHeapStart().Ptr + (ulong)(((frame * DescriptorsPerFrame) + index) * descriptorIncrement)
        };
    }

    private void CreateSrv(DxImage image, CpuDescriptorHandle descriptor)
    {
        ShaderResourceViewDesc desc = new()
        {
            Format = ToNativeFormat(image.Format),
            ViewDimension = image.Layers > 1 ? SrvDimension.Texture2Darray : SrvDimension.Texture2D,
            Shader4ComponentMapping = ShaderComponentMapping
        };
        if (image.Layers > 1)
        {
            desc.Texture2DArray = new()
            {
                MipLevels = 1u,
                ArraySize = (uint)image.Layers
            };
        }
        else
        {
            desc.Texture2D = new()
            {
                MipLevels = 1u
            };
        }

        device.Handle->CreateShaderResourceView(image.Texture.Handle, &desc, descriptor);
    }

    private void UploadFont()
    {
        DxImage replacement = (DxImage)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        const int RowAlignmentInBytes = (int)D3D12.TextureDataPitchAlignment;
        int rowPitch = ((UI.FontWidth * 4) + RowAlignmentInBytes - 1) & ~(RowAlignmentInBytes - 1);
        ComPtr<ID3D12Resource> fontUpload = CreateUploadBuffer(rowPitch * UI.FontHeight);
        uploads.Add(fontUpload);
        byte* mapped = Map<byte>(fontUpload);
        for (int row = 0; row < UI.FontHeight; row++)
        {
            UI.FontPixels.AsSpan(row * UI.FontWidth * 4, UI.FontWidth * 4).CopyTo(new Span<byte>(mapped + (row * rowPitch), UI.FontWidth * 4));
        }

        fontUpload.Handle->Unmap(0u, null);
        Transition(font, ImageUse.CopyDestination);
        PlacedSubresourceFootprint footprint = new()
        {
            Footprint = new()
            {
                Format = Format.FormatR8G8B8A8Unorm,
                Width = (uint)UI.FontWidth,
                Height = (uint)UI.FontHeight,
                Depth = 1u,
                RowPitch = (uint)rowPitch
            }
        };
        TextureCopyLocation src = new()
        {
            PResource = fontUpload.Handle,
            Type = TextureCopyType.PlacedFootprint,
            PlacedFootprint = footprint
        };
        TextureCopyLocation dst = new()
        {
            PResource = font.Texture.Handle,
            Type = TextureCopyType.SubresourceIndex
        };
        commandList.Handle->CopyTextureRegion(&dst, 0u, 0u, 0u, &src, null);
        Transition(font, ImageUse.ShaderRead);
    }

    private void ExecuteUploads()
    {
        Check(commandList.Handle->Close());
        isRecording = false;
        Execute(queue, commandList);
        WaitIdle();

        DestroyUploads();
    }

    private void DestroyUploads()
    {
        if (uploads is null)
        {
            return;
        }

        for (int i = uploads.Count - 1; i >= 0; i--)
        {
            uploads[i].Dispose();
        }
        uploads.Clear();
    }

    private ComPtr<ID3D12Resource> CreateResource(HeapType heap, ResourceDesc desc, ResourceStates state)
    {
        HeapProperties properties = new()
        {
            Type = heap,
            CreationNodeMask = 1u,
            VisibleNodeMask = 1u
        };

        ComPtr<ID3D12Resource> resource = default;
        Check(device.Handle->CreateCommittedResource(&properties, HeapFlags.None, &desc, state, null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)resource.GetAddressOf()));

        return resource;
    }

    private ComPtr<ID3D12Resource> CreateBuffer(ulong sizeInBytes, HeapType heap, ResourceStates state, ResourceFlags flags = ResourceFlags.None)
    {
        ResourceDesc desc = new()
        {
            Dimension = ResourceDimension.Buffer,
            Width = sizeInBytes,
            Height = 1u,
            DepthOrArraySize = 1,
            MipLevels = 1,
            SampleDesc = new(1, 0),
            Layout = TextureLayout.LayoutRowMajor,
            Flags = flags
        };

        return CreateResource(heap, desc, state);
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
        Check(device.Handle->CreateCommandList(0u, CommandListType.Direct, allocator.Handle, null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(), (void**)commands.GetAddressOf()));

        return commands;
    }

    private ComPtr<ID3D12DescriptorHeap> CreateDescriptorHeap(DescriptorHeapType type, int count, DescriptorHeapFlags flags = DescriptorHeapFlags.None)
    {
        DescriptorHeapDesc desc = new()
        {
            Type = type,
            NumDescriptors = (uint)count,
            Flags = flags
        };

        ComPtr<ID3D12DescriptorHeap> heap = default;
        Check(device.Handle->CreateDescriptorHeap(&desc, SilkMarshal.GuidPtrOf<ID3D12DescriptorHeap>(), (void**)heap.GetAddressOf()));

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

        commandList.Handle->ResourceBarrier(1u, &barrier);
    }

    private void SetViewport(int width, int height)
    {
        Viewport viewport = new(0, 0, width, height, 0, 1);
        commandList.Handle->RSSetViewports(1u, &viewport);
    }

    private void SetScissor(int left, int top, int right, int bottom)
    {
        Silk.NET.Maths.Box2D<int> rectangle = new(left, top, right, bottom);
        commandList.Handle->RSSetScissorRects(1u, &rectangle);
    }

    private void ClearColor(CpuDescriptorHandle handle)
    {
        float* color = stackalloc float[4];
        new Span<float>(color, 4).Clear();
        commandList.Handle->ClearRenderTargetView(handle, color, 0u, null);
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
                NumDescriptorRanges = 1u,
                PDescriptorRanges = &ranges[0]
            },
            ShaderVisibility = ShaderVisibility.All
        };
        parameters[2] = new()
        {
            ParameterType = RootParameterType.TypeDescriptorTable,
            DescriptorTable = new()
            {
                NumDescriptorRanges = 1u,
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
            MaxAnisotropy = 1u,
            ComparisonFunc = ComparisonFunc.Never,
            BorderColor = StaticBorderColor.TransparentBlack,
            MinLOD = float.MinValue,
            MaxLOD = float.MaxValue,
            ShaderVisibility = ShaderVisibility.All
        };

        VersionedRootSignatureDesc desc = new()
        {
            Version = D3DRootSignatureVersion.Version11,
            Desc11 = new()
            {
                NumParameters = 3u,
                PParameters = parameters,
                NumStaticSamplers = 1u,
                PStaticSamplers = &sampler,
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
            }
        };

        using ComPtr<ID3D10Blob> serialized = default;
        using ComPtr<ID3D10Blob> errors = default;

        Check(d3d12.SerializeVersionedRootSignature(&desc, serialized.GetAddressOf(), errors.GetAddressOf()));

        fixed (ID3D12RootSignature** pRoot = &root.Handle)
        {
            Check(device.Handle->CreateRootSignature(0u, serialized.Handle->GetBufferPointer(), serialized.Handle->GetBufferSize(), SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)pRoot));
        }

        uiPipeline = CreateUiPipeline();

        foreach (ComputePass pass in RenderLayout.ComputePasses)
        {
            byte[] shader = CompileShader(pass.ToString(), "compute");

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

    private ComPtr<ID3D12PipelineState> CreateUiPipeline()
    {
        byte[] vertexCode = CompileShader("UiVS", "vertex");
        byte[] fragmentCode = CompileShader("UiPS", "fragment");
        DepthStencilopDesc stencil = new()
        {
            StencilFailOp = StencilOp.Keep,
            StencilDepthFailOp = StencilOp.Keep,
            StencilPassOp = StencilOp.Keep,
            StencilFunc = ComparisonFunc.Always
        };

        GraphicsPipelineStateDesc desc = new()
        {
            PRootSignature = root.Handle,
            SampleMask = uint.MaxValue,
            RasterizerState = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontCounterClockwise = false,
                DepthClipEnable = true
            },
            DepthStencilState = new()
            {
                DepthEnable = false,
                DepthWriteMask = DepthWriteMask.Zero,
                DepthFunc = ComparisonFunc.Always,
                StencilReadMask = byte.MaxValue,
                StencilWriteMask = byte.MaxValue,
                FrontFace = stencil,
                BackFace = stencil
            },
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            NumRenderTargets = 1u,
            DSVFormat = Format.FormatUnknown,
            SampleDesc = new(1, 0)
        };

        desc.BlendState.RenderTarget[0] = new()
        {
            BlendEnable = true,
            SrcBlend = Blend.One,
            DestBlend = Blend.InvSrcAlpha,
            BlendOp = BlendOp.Add,
            SrcBlendAlpha = Blend.One,
            DestBlendAlpha = Blend.InvSrcAlpha,
            BlendOpAlpha = BlendOp.Add,
            LogicOp = LogicOp.Noop,
            RenderTargetWriteMask = (byte)ColorWriteEnable.All
        };

        desc.RTVFormats[0] = ToNativeFormat(RenderLayout.Format(ImageSlot.UI));

        using NGXStrings strings = new();

        fixed (byte* vs = vertexCode)
        fixed (byte* ps = fragmentCode)
        {
            InputElementDesc* elements = stackalloc InputElementDesc[3];
            elements[0] = new()
            {
                SemanticName = strings.AllocUtf8("POSITION"),
                Format = Format.FormatR32G32Float
            };
            elements[1] = new()
            {
                SemanticName = strings.AllocUtf8("TEXCOORD"),
                Format = Format.FormatR32G32Float,
                AlignedByteOffset = 8u
            };
            elements[2] = new()
            {
                SemanticName = strings.AllocUtf8("COLOR"),
                Format = Format.FormatR8G8B8A8Unorm,
                AlignedByteOffset = 16u
            };

            desc.VS = new(vs, (nuint)vertexCode.Length);
            desc.PS = new(ps, (nuint)fragmentCode.Length);
            desc.InputLayout = new()
            {
                PInputElementDescs = elements,
                NumElements = 3u
            };

            ComPtr<ID3D12PipelineState> pipeline = default;
            Check(device.Handle->CreateGraphicsPipelineState(&desc, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf()));

            return pipeline;
        }
    }

    private void WaitFence(ulong value)
    {
        if (value is 0UL || fence.Handle->GetCompletedValue() >= value)
        {
            return;
        }

        Check(fence.Handle->SetEventOnCompletion(value, (void*)fenceEvent.SafeWaitHandle.DangerousGetHandle()));
        fenceEvent.WaitOne();
    }

    private ComPtr<ID3D12Resource> CreateAccelerationBuffer(ulong sizeInBytes, ResourceStates state)
    {
        const ulong Alignment = D3D12.RaytracingAccelerationStructureByteAlignment;
        ulong alignedSizeInBytes = (sizeInBytes + Alignment - 1UL) / Alignment * Alignment;

        return CreateBuffer(alignedSizeInBytes, HeapType.Default, state, ResourceFlags.AllowUnorderedAccess);
    }

    private void InitializeAccelerationStructures()
    {
        foreach (SceneObject instance in Resources.Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            RaytracingGeometryDesc geometry = new()
            {
                Type = RaytracingGeometryType.Triangles,
                Flags = range.Opaque is not 0u ? RaytracingGeometryFlags.Opaque : RaytracingGeometryFlags.None,
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
                NumDescs = 1u,
                DescsLayout = ElementsLayout.Array,
                PGeometryDescs = &geometry
            };

            RaytracingAccelerationStructurePrebuildInfo sizes = default;
            rayDevice.Handle->GetRaytracingAccelerationStructurePrebuildInfo(&inputs, &sizes);
            ComPtr<ID3D12Resource> bottom = CreateAccelerationBuffer(sizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            bottomLevels.Add(bottom);
            ComPtr<ID3D12Resource> scratch = CreateAccelerationBuffer(sizes.ScratchDataSizeInBytes, ResourceStates.UnorderedAccess);

            // Startup submission owns this scratch storage until WaitIdle releases the upload batch.
            uploads.Add(scratch);
            BuildRaytracingAccelerationStructureDesc build = new()
            {
                DestAccelerationStructureData = bottom.Handle->GetGPUVirtualAddress(),
                Inputs = inputs,
                ScratchAccelerationStructureData = scratch.Handle->GetGPUVirtualAddress()
            };
            rayCommands.Handle->BuildRaytracingAccelerationStructure(&build, 0u, null);
            UavBarrier(bottom);
        }

        BuildRaytracingAccelerationStructureInputs topInputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            NumDescs = (uint)Resources.Scene.Objects.Length,
            DescsLayout = ElementsLayout.Array
        };

        RaytracingAccelerationStructurePrebuildInfo topSizes = default;
        rayDevice.Handle->GetRaytracingAccelerationStructurePrebuildInfo(&topInputs, &topSizes);

        foreach (DxFrame frame in slots)
        {
            frame.Tlas = CreateAccelerationBuffer(topSizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            frame.RayScratch = CreateAccelerationBuffer(Math.Max(topSizes.ScratchDataSizeInBytes, topSizes.UpdateScratchDataSizeInBytes), ResourceStates.UnorderedAccess);
            frame.RayInstances = CreateUploadBuffer(Resources.Scene.Objects.Length * sizeof(RaytracingInstanceDesc));
        }
    }

    private void UpdateAccelerationStructure(DxFrame frame)
    {
        ulong transformVersion = Resources.Scene.TransformVersion;
        if (frame.IsTlasBuilt && frame.TlasVersion == transformVersion)
        {
            return;
        }

        // BeginCommands waits for this slot while other slots retain their own ray tracing resources.
        Span<RaytracingInstanceDesc> instances = new(Map<RaytracingInstanceDesc>(frame.RayInstances), Resources.Scene.Objects.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Resources.Scene.Objects[i], (uint)i, bottomLevels[i].Handle->GetGPUVirtualAddress());
        }

        frame.RayInstances.Handle->Unmap(0u, null);

        if (frame.IsTlasBuilt)
        {
            UavBarrier(frame.Tlas);
            UavBarrier(frame.RayScratch);
        }

        BuildRaytracingAccelerationStructureInputs inputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            NumDescs = (uint)Resources.Scene.Objects.Length,
            DescsLayout = ElementsLayout.Array,
            InstanceDescs = frame.RayInstances.Handle->GetGPUVirtualAddress()
        };
        if (frame.IsTlasBuilt)
        {
            inputs.Flags |= RaytracingAccelerationStructureBuildFlags.PerformUpdate;
        }

        BuildRaytracingAccelerationStructureDesc build = new()
        {
            DestAccelerationStructureData = frame.Tlas.Handle->GetGPUVirtualAddress(),
            Inputs = inputs,
            SourceAccelerationStructureData = frame.IsTlasBuilt ? frame.Tlas.Handle->GetGPUVirtualAddress() : 0UL,
            ScratchAccelerationStructureData = frame.RayScratch.Handle->GetGPUVirtualAddress()
        };
        rayCommands.Handle->BuildRaytracingAccelerationStructure(&build, 0u, null);
        UavBarrier(frame.Tlas);
        frame.IsTlasBuilt = true;
        frame.TlasVersion = transformVersion;
    }

    private ComPtr<ID3D12Resource> CreateUploadBuffer(int sizeInBytes)
    {
        return CreateBuffer((ulong)Math.Max(sizeInBytes, sizeof(uint)), HeapType.Upload, ResourceStates.GenericRead);
    }

    private ComPtr<ID3D12Resource> CreateStaticBuffer<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        int sizeInBytes = data.Length * sizeof(T);
        ComPtr<ID3D12Resource> buffer = CreateBuffer((ulong)sizeInBytes, HeapType.Default, ResourceStates.CopyDest);
        bool isCompleted = false;
        try
        {
            ComPtr<ID3D12Resource> upload = CreateUploadBuffer(sizeInBytes);
            uploads.Add(upload);
            SetData(upload, data);
            commandList.Handle->CopyBufferRegion(buffer.Handle, 0UL, upload.Handle, 0UL, (ulong)sizeInBytes);
            TransitionBarrier(commandList, buffer, ResourceStates.CopyDest, ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource);

            isCompleted = true;

            return buffer;
        }
        finally
        {
            if (!isCompleted)
            {
                buffer.Dispose();
            }
        }
    }

    private void EnsureUpload(ref ComPtr<ID3D12Resource> buffer, ref int capacityInBytes, int requiredSizeInBytes)
    {
        if (buffer.Handle != null && capacityInBytes >= requiredSizeInBytes)
        {
            return;
        }

        int newCapacityInBytes = Math.Max(MinUploadCapacityInBytes, requiredSizeInBytes * 2);
        ComPtr<ID3D12Resource> replacement = CreateUploadBuffer(newCapacityInBytes);
        buffer.Dispose();
        buffer = replacement;
        capacityInBytes = newCapacityInBytes;
    }

    private static byte[] CompileShader(string entry, string stage)
    {
        return ShaderCompiler.Compile("Scene.slang", entry, stage, false);
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
        Check(resource.Handle->Map(0u, &readRange, &pointer));

        return (T*)pointer;
    }

    private static void SetData<T>(ComPtr<ID3D12Resource> resource, ReadOnlySpan<T> values, int offsetInBytes = 0)
        where T : unmanaged
    {
        T* pointer = (T*)((byte*)Map<byte>(resource) + offsetInBytes);
        values.CopyTo(new Span<T>(pointer, values.Length));
        resource.Handle->Unmap(0u, null);
    }

    private static void Execute(ComPtr<ID3D12CommandQueue> target, ComPtr<ID3D12GraphicsCommandList> commands)
    {
        ID3D12CommandList* list = (ID3D12CommandList*)commands.Handle;
        target.Handle->ExecuteCommandLists(1u, &list);
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

        commands.Handle->ResourceBarrier(1u, &barrier);
    }

    private static RaytracingInstanceDesc CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        Vector4 offset = instance.Offset;

        RaytracingInstanceDesc desc = new()
        {
            InstanceID = id,
            InstanceMask = byte.MaxValue,
            // Default ray facing matches dot(direction, cross(edge1, edge2)) < 0.
            Flags = instance.Geometry.DoubleSided is not 0u ? (uint)RaytracingInstanceFlags.TriangleCullDisable : 0u,
            AccelerationStructure = address
        };
        desc.Transform[0] = 1.0f;
        desc.Transform[3] = offset.X;
        desc.Transform[5] = 1.0f;
        desc.Transform[7] = offset.Y;
        desc.Transform[10] = 1.0f;
        desc.Transform[11] = offset.Z;

        return desc;
    }

    private static Format ToNativeFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba16 => Format.FormatR16G16B16A16Float,
            ImageFormat.Rgba32 => Format.FormatR32G32B32A32Float,
            ImageFormat.Rg16 => Format.FormatR16G16Float,
            ImageFormat.Float => Format.FormatR32Float,
            _ => Format.FormatR8G8B8A8Unorm
        };
    }

    private class DxFrame : DisposableObject
    {
        public ComPtr<ID3D12CommandAllocator> Allocator;

        public ComPtr<ID3D12Resource> Constants;

        public ComPtr<ID3D12Resource> Objects;

        public ComPtr<ID3D12Resource> Vertices;

        public ComPtr<ID3D12Resource> Indices;

        public int VertexCapacityInBytes;

        public int IndexCapacityInBytes;

        public ulong FenceValue;

        public ComPtr<ID3D12Resource> Tlas;

        public ComPtr<ID3D12Resource> RayScratch;

        public ComPtr<ID3D12Resource> RayInstances;

        public bool IsTlasBuilt;

        public ulong TlasVersion;

        protected override void Destroy()
        {
            Indices.Dispose();
            Vertices.Dispose();
            RayInstances.Dispose();
            RayScratch.Dispose();
            Tlas.Dispose();
            Objects.Dispose();
            Constants.Dispose();
            Allocator.Dispose();
        }
    }

    private class DxImage : GpuImage
    {
        public required ComPtr<ID3D12Resource> Texture;

        public ResourceStates State;

        public CpuDescriptorHandle Rtv;

        public override NativeImage Describe()
        {
            return new()
            {
                DirectX = (nint)Texture.Handle
            };
        }

        protected override void Destroy()
        {
            Texture.Dispose();
        }
    }
}
