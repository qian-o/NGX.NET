using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private ComPtr<ID3D12Device5> rayDevice;
    private ComPtr<ID3D12GraphicsCommandList4> rayCommands;
    private readonly List<ComPtr<ID3D12Resource>> bottomLevels = [];

    private ComPtr<ID3D12Resource> AccelerationBuffer(ulong size, ResourceStates state)
    {
        if (size == 0)
        {
            throw new InvalidOperationException("DXR returned an empty acceleration-structure allocation size.");
        }

        ulong alignment = D3D12.RaytracingAccelerationStructureByteAlignment;
        ulong alignedSize = (size + alignment - 1) / alignment * alignment;

        return CreateBuffer(alignedSize, HeapType.Default, state, ResourceFlags.AllowUnorderedAccess);
    }

    private void InitializeAccelerationStructures()
    {
        foreach (SceneObject instance in Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            RaytracingGeometryDesc geometry = new()
            {
                Type = RaytracingGeometryType.Triangles,
                Flags = range.Opaque != 0 ? RaytracingGeometryFlags.Opaque : RaytracingGeometryFlags.None,
                Triangles = new()
                {
                    VertexFormat = Format.FormatR32G32B32Float,
                    VertexCount = range.VertexCount,
                    VertexBuffer = new()
                    {
                        StartAddress = sceneBuffers[0].Handle->GetGPUVirtualAddress() + (ulong)range.FirstVertex * (uint)sizeof(SceneVertex),
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
            NumDescs = (uint)Scene.Objects.Length
        };

        RaytracingAccelerationStructurePrebuildInfo topSizes = default;
        rayDevice.Handle->GetRaytracingAccelerationStructurePrebuildInfo(&topInputs, &topSizes);

        foreach (DxFrame frame in slots)
        {
            frame.Tlas = AccelerationBuffer(topSizes.ResultDataMaxSizeInBytes, ResourceStates.RaytracingAccelerationStructure);
            frame.RayScratch = AccelerationBuffer(Math.Max(topSizes.ScratchDataSizeInBytes, topSizes.UpdateScratchDataSizeInBytes), ResourceStates.UnorderedAccess);
            frame.RayInstances = UploadBuffer(Scene.Objects.Length * sizeof(RaytracingInstanceDesc));
        }
    }

    private static RaytracingInstanceDesc CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        System.Numerics.Vector4 offset = instance.Offset;

        RaytracingInstanceDesc description = new()
        {
            InstanceID = id,
            InstanceMask = byte.MaxValue,
            // Default ray facing matches dot(direction, cross(edge1, edge2)) < 0.
            Flags = instance.Geometry.DoubleSided != 0 ? (uint)RaytracingInstanceFlags.TriangleCullDisable : 0,
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

    private void UpdateAccelerationStructure(DxFrame frame)
    {
        // BeginCommands has already waited for this frame slot's fence. Other slots
        // retain their own TLAS, scratch and instance data until their GPU work ends.
        Span<RaytracingInstanceDesc> instances = new(Map<RaytracingInstanceDesc>(frame.RayInstances), Scene.Objects.Length);

        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Scene.Objects[i], (uint)i, bottomLevels[i].Handle->GetGPUVirtualAddress());
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
            NumDescs = (uint)Scene.Objects.Length,
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
}
