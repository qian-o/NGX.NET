using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Showcase;

internal sealed unsafe partial class DirectX12RHI
{
    private ID3D12Device5? rayDevice;
    private ID3D12GraphicsCommandList4? rayCommands;
    private readonly List<ID3D12Resource> bottomLevels = [];

    private ID3D12Resource AccelerationBuffer(ulong size, ResourceStates state)
    {
        if (size == 0)
        {
            throw new InvalidOperationException("DXR returned an empty acceleration-structure allocation size.");
        }
        ulong alignment = D3D12.RaytracingAccelerationStructureByteAlignment;
        ulong alignedSize = (size + alignment - 1) / alignment * alignment;
        return device.CreateCommittedResource(HeapType.Default,
            ResourceDescription.Buffer(alignedSize, ResourceFlags.AllowUnorderedAccess), state);
    }

    private void InitializeAccelerationStructures()
    {
        foreach (SceneObject instance in Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            RaytracingGeometryDescription geometry = new(new RaytracingGeometryTrianglesDescription
            {
                VertexFormat = Format.R32G32B32_Float,
                VertexCount = range.VertexCount,
                VertexBuffer = new()
                {
                    StartAddress = sceneBuffers[0].GPUVirtualAddress + (ulong)range.FirstVertex * (uint)sizeof(SceneVertex),
                    StrideInBytes = (uint)sizeof(SceneVertex)
                }
            }, RaytracingGeometryFlags.None);
            // Non-opaque geometry exposes candidates for per-material alpha and sidedness checks.
            BuildRaytracingAccelerationStructureInputs inputs = new()
            {
                Type = RaytracingAccelerationStructureType.BottomLevel,
                Flags = RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
                Layout = ElementsLayout.Array,
                DescriptorsCount = 1,
                GeometryDescriptions = [geometry]
            };
            RaytracingAccelerationStructurePrebuildInfo sizes = rayDevice!.GetRaytracingAccelerationStructurePrebuildInfo(inputs);
            ID3D12Resource bottom = AccelerationBuffer(sizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            bottomLevels.Add(bottom);
            ID3D12Resource scratch = AccelerationBuffer(sizes.ScratchDataSizeInBytes, ResourceStates.UnorderedAccess);
            // Startup submission owns this scratch storage until WaitIdle releases the upload batch.
            uploads.Add(scratch);
            rayCommands!.BuildRaytracingAccelerationStructure(new()
            {
                Inputs = inputs,
                DestinationAccelerationStructureData = bottom.GPUVirtualAddress,
                ScratchAccelerationStructureData = scratch.GPUVirtualAddress
            });
            commandList.ResourceBarrierUnorderedAccessView(bottom);
        }

        BuildRaytracingAccelerationStructureInputs topInputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            Layout = ElementsLayout.Array,
            DescriptorsCount = (uint)Scene.Objects.Length
        };
        RaytracingAccelerationStructurePrebuildInfo topSizes = rayDevice!.GetRaytracingAccelerationStructurePrebuildInfo(topInputs);
        foreach (DxFrame frame in slots)
        {
            frame.Tlas = AccelerationBuffer(topSizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            frame.RayScratch = AccelerationBuffer(Math.Max(topSizes.ScratchDataSizeInBytes, topSizes.UpdateScratchDataSizeInBytes), ResourceStates.UnorderedAccess);
            frame.RayInstances = UploadBuffer(Scene.Objects.Length * sizeof(RaytracingInstanceDescription));
        }
    }

    private static RaytracingInstanceDescription CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        System.Numerics.Vector4 offset = instance.Offset;
        return new()
        {
            Transform = new Matrix3x4(1, 0, 0, offset.X, 0, 1, 0, offset.Y, 0, 0, 1, offset.Z),
            InstanceID = (Vortice.UInt24)id,
            InstanceMask = byte.MaxValue,
            AccelerationStructure = address
        };
    }

    private void UpdateAccelerationStructure(DxFrame frame)
    {
        // BeginCommands has already waited for this frame slot's fence. Other slots
        // retain their own TLAS, scratch and instance data until their GPU work ends.
        Span<RaytracingInstanceDescription> instances = frame.RayInstances!.Map<RaytracingInstanceDescription>(0, Scene.Objects.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Scene.Objects[i], (uint)i, bottomLevels[i].GPUVirtualAddress);
        }
        frame.RayInstances.Unmap(0);
        if (frame.TlasBuilt)
        {
            commandList.ResourceBarrierUnorderedAccessView(frame.Tlas!);
            commandList.ResourceBarrierUnorderedAccessView(frame.RayScratch!);
        }
        BuildRaytracingAccelerationStructureInputs inputs = new()
        {
            Type = RaytracingAccelerationStructureType.TopLevel,
            Flags = RaytracingAccelerationStructureBuildFlags.AllowUpdate | RaytracingAccelerationStructureBuildFlags.PreferFastTrace,
            Layout = ElementsLayout.Array,
            DescriptorsCount = (uint)Scene.Objects.Length,
            InstanceDescriptions = frame.RayInstances.GPUVirtualAddress
        };
        if (frame.TlasBuilt)
        {
            inputs.Flags |= RaytracingAccelerationStructureBuildFlags.PerformUpdate;
        }

        rayCommands!.BuildRaytracingAccelerationStructure(new()
        {
            Inputs = inputs,
            DestinationAccelerationStructureData = frame.Tlas!.GPUVirtualAddress,
            SourceAccelerationStructureData = frame.TlasBuilt ? frame.Tlas.GPUVirtualAddress : 0,
            ScratchAccelerationStructureData = frame.RayScratch!.GPUVirtualAddress
        });
        commandList.ResourceBarrierUnorderedAccessView(frame.Tlas);
        frame.TlasBuilt = true;
    }
}
