using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;
using Silk.NET.Windowing;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI(IWindow window, ImGuiHandler ui) : RHI(window, ui)
{
    public override nint Command => (nint)commandList.Handle;

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

                if ((description.Flags & (uint)AdapterFlag.Software) == 0 && (adapter.Handle == null || (!nvidiaSelected && nvidia)))
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

        CommandQueueDesc queueDescription = new() { Type = CommandListType.Direct };
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
}
