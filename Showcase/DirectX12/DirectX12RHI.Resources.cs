using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

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
}
