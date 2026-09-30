using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI(Window window, UserInterface ui) : RHI(window, ui)
{
    protected override nint Command => (nint)commandList.Handle;

    private ComPtr<ID3D12CommandAllocator> presentAllocator;
    private ComPtr<ID3D12GraphicsCommandList> presentCommands;
    private ComPtr<ID3D12Fence> presentFence;
    private ulong presentFenceValue;
    private readonly AutoResetEvent presentEvent = new(false);

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
    private readonly Dictionary<ComputePass, ComPtr<ID3D12PipelineState>> pipelines = [];
    private ComPtr<ID3D12DescriptorHeap> descriptors;
    private ComPtr<ID3D12DescriptorHeap> renderTargets;
    private ComPtr<ID3D12DescriptorHeap> depthViews;
    private readonly DxFrame[] slots = new DxFrame[RenderLayout.FramesInFlight];
    private readonly ComPtr<ID3D12Resource>[] sceneBuffers = new ComPtr<ID3D12Resource>[4];
    private readonly List<ComPtr<ID3D12Resource>> uploads = [];
    private readonly List<ComPtr<ID3D12Resource>> backBuffers = [];
    private DxImage font = null!;
    private uint descriptorIncrement;
    private uint rtvIncrement;
    private uint dsvIncrement;
    private ulong fenceValue;
    private int constantIndex;
    private bool recording;
    private readonly AutoResetEvent fenceEvent = new(false);
    private const int DescriptorsPerFrame = RenderLayout.SrvCount + RenderLayout.UavCount;
    // SDK constants expressed by macros that Silk does not emit.
    private const uint ShaderComponentMapping = 0x1688; // D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING
    private const uint NoAltEnter = 0x2; // DXGI_MWA_NO_ALT_ENTER

    private D3D12 d3d12 = null!;
    private DXGI dxgi = null!;

    protected override void InitializeDevice()
    {
        d3d12 = D3D12.GetApi();
        dxgi = DXGI.GetApi(null);

        Check(dxgi.CreateDXGIFactory2(0, SilkMarshal.GuidPtrOf<IDXGIFactory4>(), (void**)factory.GetAddressOf()));
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
                bool nvidia = description.VendorId == 0x10DE;

                if (((AdapterFlag)description.Flags & AdapterFlag.Software) == 0 &&
                    (adapter.Handle == null || !nvidiaSelected && nvidia))
                {
                    adapter.Dispose();
                    adapter = candidate;
                    candidate = default;
                    nvidiaSelected = nvidia;
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

        AdapterDesc1 selected = default;
        Check(adapter.Handle->GetDesc1(&selected));
        AdapterName = NGXMarshal.PtrToString(selected.Description, NGXEncoding.NativeWide)!;
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

        CommandQueueDesc queueDescription = new() { Type = CommandListType.Direct };
        Check(device.Handle->CreateCommandQueue(&queueDescription, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)queue.GetAddressOf()));
        Check(device.Handle->CreateCommandQueue(&queueDescription, SilkMarshal.GuidPtrOf<ID3D12CommandQueue>(), (void**)presentQueue.GetAddressOf()));
        Check(device.Handle->CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)fence.GetAddressOf()));
        presentAllocator = CreateAllocator();
        presentCommands = CreateCommands(presentAllocator);
        Check(presentCommands.Handle->Close());
        Check(device.Handle->CreateFence(0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(), (void**)presentFence.GetAddressOf()));
    }

    private ComPtr<ID3D12Resource> UploadBuffer(int bytes) => CreateBuffer((ulong)Math.Max(bytes, 4), HeapType.Upload, ResourceStates.GenericRead);

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

    protected override void CreateSwapChain()
    {
        SwapChainDesc1 description = new()
        {
            Width = (uint)Window.Width,
            Height = (uint)Window.Height,
            Format = Format.FormatR8G8B8A8Unorm,
            BufferCount = RenderLayout.FramesInFlight,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SampleDesc = new(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Scaling = Scaling.Stretch,
            AlphaMode = AlphaMode.Ignore
        };

        using ComPtr<IDXGISwapChain1> created = default;
        Check(factory.Handle->CreateSwapChainForHwnd((IUnknown*)presentQueue.Handle, Window.Handle, &description, null, null, created.GetAddressOf()));
        Check(created.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**)swapChain.GetAddressOf()));
        Check(factory.Handle->MakeWindowAssociation(Window.Handle, NoAltEnter));

        for (uint i = 0; i < RenderLayout.FramesInFlight; i++)
        {
            ComPtr<ID3D12Resource> buffer = default;
            Check(swapChain.Handle->GetBuffer(i, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)buffer.GetAddressOf()));
            backBuffers.Add(buffer);
        }
    }

    protected override void DestroySwapChain()
    {
        foreach (ComPtr<ID3D12Resource> buffer in backBuffers)
        {
            buffer.Dispose();
        }

        backBuffers.Clear();
        swapChain.Dispose();
    }

    protected override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ResourceFlags flags = format == ImageFormat.Depth ? ResourceFlags.AllowDepthStencil : ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess;
        Format resourceFormat = format == ImageFormat.Depth ? Format.FormatR32Typeless : NativeFormat(format);
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

    private static Format NativeFormat(ImageFormat format) => format switch
    {
        ImageFormat.Rgba16 => Format.FormatR16G16B16A16Float,
        ImageFormat.Rgba32 => Format.FormatR32G32B32A32Float,
        ImageFormat.Rg16 => Format.FormatR16G16Float,
        ImageFormat.Float => Format.FormatR32Float,
        ImageFormat.Depth => Format.FormatD32Float,
        _ => Format.FormatR8G8B8A8Unorm
    };
}
