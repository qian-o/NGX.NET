using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private sealed class DxFrame : IDisposable
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

    private sealed class DxImage : GpuImage
    {
        public required ComPtr<ID3D12Resource> Texture;

        public ResourceStates State;

        public CpuDescriptorHandle Rtv;

        public CpuDescriptorHandle Dsv;

        public override NativeImage Describe() => new()
        {
            DirectX = (nint)Texture.Handle
        };

        public override void Dispose() => Texture.Dispose();
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

    public override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
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
