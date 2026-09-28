using Showcase.Models;
using Streamline.NET;
using Vortice.Direct3D12;
using Resource = Streamline.NET.Resource;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private sealed class DxFrame : IDisposable
    {
        public required ID3D12CommandAllocator Allocator;
        public required ID3D12Resource Constants;
        public required ID3D12Resource Objects;
        public ID3D12Resource? Vertices;
        public ID3D12Resource? Indices;
        public int VertexCapacity;
        public int IndexCapacity;
        public ulong Fence;
        public ID3D12Resource? Tlas;
        public ID3D12Resource? RayScratch;
        public ID3D12Resource? RayInstances;
        public bool TlasBuilt;

        public void Dispose()
        {
            Tlas?.Dispose();
            RayScratch?.Dispose();
            RayInstances?.Dispose();
            Vertices?.Dispose();
            Indices?.Dispose();
            Constants.Dispose();
            Objects.Dispose();
            Allocator.Dispose();
        }
    }

    private sealed class DxImage : GpuImage
    {
        public required ID3D12Resource Texture;
        public ResourceStates State;
        public CpuDescriptorHandle Rtv;
        public CpuDescriptorHandle Dsv;

        public override Resource Describe() => new()
        {
            Type = ResourceType.Tex2d,
            Native = (void*)Texture.NativePointer,
            State = (uint)State,
            Width = (uint)Width,
            Height = (uint)Height,
            NativeFormat = (uint)NativeFormat(Format),
            MipLevels = 1,
            ArrayLayers = (uint)Layers
        };

        public override void Dispose() => Texture.Dispose();
    }
}
