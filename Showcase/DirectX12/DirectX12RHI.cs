using System.Runtime.InteropServices;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Streamline.NET;
using Vortice.DXGI;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Format = Vortice.DXGI.Format;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI(Window window, UserInterface ui) : RHI(window, ui)
{
    protected override RenderAPI API => RenderAPI.D3D12;

    protected override nint Command => commandList.NativePointer;

    private ID3D12Device device = null!;
    private IDXGIFactory4 factory = null!;
    private IDXGIAdapter1 adapter = null!;
    private ID3D12CommandQueue queue = null!;
    private ID3D12GraphicsCommandList commandList = null!;
    private ID3D12Fence fence = null!;
    private IDXGISwapChain3? swapChain;
    private ID3D12RootSignature root = null!;
    private ID3D12PipelineState scenePipeline = null!;
    private ID3D12PipelineState depthPipeline = null!;
    private ID3D12PipelineState uiPipeline = null!;
    private ID3D12PipelineState shadowPipeline = null!;
    private readonly Dictionary<ComputePass, ID3D12PipelineState> pipelines = [];
    private ID3D12DescriptorHeap descriptors = null!;
    private ID3D12DescriptorHeap renderTargets = null!;
    private ID3D12DescriptorHeap depthViews = null!;
    private readonly DxFrame[] slots = new DxFrame[RenderLayout.FramesInFlight];
    private readonly ID3D12Resource[] sceneBuffers = new ID3D12Resource[4];
    private readonly List<ID3D12Resource> uploads = [];
    private readonly List<ID3D12Resource> backBuffers = [];
    private DxImage font = null!;
    private uint descriptorIncrement;
    private uint rtvIncrement;
    private uint dsvIncrement;
    private ulong fenceValue;
    private int constantIndex;
    private bool recording;
    private readonly AutoResetEvent fenceEvent = new(false);
    private const int DescriptorsPerFrame = RenderLayout.SrvCount + RenderLayout.UavCount;

    protected override void InitializeDevice()
    {
        Guid iid = typeof(IDXGIFactory4).GUID;
        nint pointer = 0;
        delegate* unmanaged[Stdcall]<uint, Guid*, nint*, int> createFactory = (delegate* unmanaged[Stdcall]<uint, Guid*, nint*, int>)NativeLibrary.GetExport(Streamline.Module, "CreateDXGIFactory2");
        Marshal.ThrowExceptionForHR(createFactory(0, &iid, &pointer));
        factory = new(pointer);
        List<IDXGIAdapter1> candidates = [];

        for (uint i = 0; factory.EnumAdapters1(i, out IDXGIAdapter1 candidate).Success; i++)
        {
            if ((candidate.Description1.Flags & AdapterFlags.Software) == 0)
            {
                candidates.Add(candidate);
            }
            else
            {
                candidate.Dispose();
            }
        }

        adapter = candidates.OrderByDescending(x => x.Description1.VendorId == 0x10DE).FirstOrDefault() ?? throw new InvalidOperationException("No hardware graphics adapter found.");

        foreach (IDXGIAdapter1 candidate in candidates)
        {
            if (candidate != adapter)
            {
                candidate.Dispose();
            }
        }

        AdapterName = adapter.Description1.Description;
        delegate* unmanaged[Stdcall]<nint, FeatureLevel, Guid*, nint*, int> createDevice = (delegate* unmanaged[Stdcall]<nint, FeatureLevel, Guid*, nint*, int>)NativeLibrary.GetExport(Streamline.Module, "D3D12CreateDevice");
        iid = typeof(ID3D12Device).GUID;
        Marshal.ThrowExceptionForHR(createDevice(adapter.NativePointer, FeatureLevel.Level_12_0, &iid, &pointer));
        device = new(pointer);
        StreamlineSession.Check(SL.SetD3DDevice((void*)device.NativePointer), "slSetD3DDevice");
        long luid = device.AdapterLuid;
        AdapterInfo info = new()
        {
            DeviceLUID = (byte*)&luid,
            DeviceLUIDSizeInBytes = sizeof(long)
        };
        Streamline.QueryFeatures(info);
        RayQuerySupported = device.Options5.RaytracingTier >= RaytracingTier.Tier1_1;
        RayQueryStatus = RayQuerySupported ? "DXR 1.1" : "Requires DXR tier 1.1";

        if (RayQuerySupported)
        {
            rayDevice = device.QueryInterface<ID3D12Device5>();
        }

        queue = device.CreateCommandQueue(CommandListType.Direct);
        fence = device.CreateFence();
    }

    private ID3D12Resource UploadBuffer(int bytes) => device.CreateCommittedResource(HeapType.Upload, ResourceDescription.Buffer((ulong)Math.Max(bytes, 4)), ResourceStates.GenericRead);

    private ID3D12Resource StaticBuffer<T>(ReadOnlySpan<T> data)
        where T : unmanaged
    {
        int size = data.Length * sizeof(T);
        ID3D12Resource buffer = device.CreateCommittedResource(HeapType.Default, ResourceDescription.Buffer((ulong)size), ResourceStates.CopyDest);
        ID3D12Resource upload = UploadBuffer(size);
        upload.SetData(data);
        uploads.Add(upload);
        commandList.CopyBufferRegion(buffer, 0, upload, 0, (ulong)size);
        commandList.ResourceBarrierTransition(buffer, ResourceStates.CopyDest, ResourceStates.NonPixelShaderResource | ResourceStates.PixelShaderResource);

        return buffer;
    }

    protected override void CreateSwapChain()
    {
        SwapChainDescription1 description = new()
        {
            Width = (uint)Window.Width,
            Height = (uint)Window.Height,
            Format = Format.R8G8B8A8_UNorm,
            BufferCount = RenderLayout.FramesInFlight,
            BufferUsage = Usage.RenderTargetOutput,
            SampleDescription = new(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Scaling = Scaling.Stretch,
            AlphaMode = Vortice.DXGI.AlphaMode.Ignore
        };
        using IDXGISwapChain1 created = factory.CreateSwapChainForHwnd(queue, Window.Handle, description);
        swapChain = created.QueryInterface<IDXGISwapChain3>();
        factory.MakeWindowAssociation(Window.Handle, WindowAssociationFlags.IgnoreAltEnter).CheckError();

        for (uint i = 0; i < RenderLayout.FramesInFlight; i++)
        {
            backBuffers.Add(swapChain.GetBuffer<ID3D12Resource>(i));
        }
    }

    protected override void DestroySwapChain()
    {
        foreach (ID3D12Resource buffer in backBuffers)
        {
            buffer.Dispose();
        }

        backBuffers.Clear();
        swapChain?.Dispose();
        swapChain = null;
    }

    protected override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ResourceFlags flags = format == ImageFormat.Depth ? ResourceFlags.AllowDepthStencil : ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess;
        Format resourceFormat = format == ImageFormat.Depth ? Format.R32_Typeless : NativeFormat(format);
        ResourceDescription description = ResourceDescription.Texture2D(resourceFormat, (uint)width, (uint)height, (ushort)layers, 1, flags: flags);

        return new DxImage
        {
            Width = width,
            Height = height,
            Layers = layers,
            Format = format,
            Texture = device.CreateCommittedResource(HeapType.Default, description, ResourceStates.Common),
            State = ResourceStates.Common
        };
    }

    private static Format NativeFormat(ImageFormat format) => format switch
    {
        ImageFormat.Rgba16 => Format.R16G16B16A16_Float,
        ImageFormat.Rgba32 => Format.R32G32B32A32_Float,
        ImageFormat.Rg16 => Format.R16G16_Float,
        ImageFormat.Float => Format.R32_Float,
        ImageFormat.Depth => Format.D32_Float,
        _ => Format.R8G8B8A8_UNorm
    };
}
