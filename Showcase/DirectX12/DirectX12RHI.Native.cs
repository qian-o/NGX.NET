using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private static void Check(int result) => Marshal.ThrowExceptionForHR(result);

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

    private static T* Map<T>(ComPtr<ID3D12Resource> resource) where T : unmanaged
    {
        void* pointer = null;
        Silk.NET.Direct3D12.Range readRange = default;
        Check(resource.Handle->Map(0, &readRange, &pointer));

        return (T*)pointer;
    }

    private static void SetData<T>(ComPtr<ID3D12Resource> resource, ReadOnlySpan<T> values, int byteOffset = 0) where T : unmanaged
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
}
