using Showcase.Models;
using Silk.NET.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private static readonly string[] RayExtensions =
    [
        "VK_KHR_acceleration_structure",
        "VK_KHR_ray_query",
        "VK_KHR_deferred_host_operations"
    ];
    private readonly List<VkAcceleration> bottomLevels = [];
    private uint scratchAlignment;
    private ulong maxRayInstances;
    private ulong maxRayPrimitives;

    private VkAcceleration CreateAcceleration(AccelerationStructureTypeKHR type, ulong size)
    {
        if (size == 0)
        {
            throw new InvalidOperationException("Vulkan returned an empty acceleration-structure allocation size.");
        }

        VkBufferResource storage = CreateBuffer(size, BufferUsageFlags.AccelerationStructureStorageBitKhr | BufferUsageFlags.ShaderDeviceAddressBit, false);
        AccelerationStructureCreateInfoKHR create = new()
        {
            SType = StructureType.AccelerationStructureCreateInfoKhr,
            Buffer = storage.Buffer,
            Size = size,
            Type = type
        };

        AccelerationStructureKHR handle;

        try
        {
            Check(accelerationApi.CreateAccelerationStructure(device, &create, null, &handle), "vkCreateAccelerationStructureKHR");
        }
        catch
        {
            storage.Dispose();

            throw;
        }

        VkAcceleration acceleration = new()
        {
            Api = accelerationApi,
            Device = device,
            Storage = storage,
            Handle = handle
        };

        AccelerationStructureDeviceAddressInfoKHR address = new()
        {
            SType = StructureType.AccelerationStructureDeviceAddressInfoKhr,
            AccelerationStructure = handle
        };

        acceleration.Address = accelerationApi.GetAccelerationStructureDeviceAddress(device, &address);

        if (acceleration.Address == 0)
        {
            acceleration.Dispose();

            throw new InvalidOperationException("Vulkan returned a null acceleration-structure address.");
        }

        return acceleration;
    }

    private VkBufferResource CreateRayScratch(ulong size)
    {
        if (size == 0 || scratchAlignment == 0)
        {
            throw new InvalidOperationException("Invalid Vulkan ray-tracing scratch requirements.");
        }

        return CreateBuffer(checked(size + scratchAlignment - 1), BufferUsageFlags.StorageBufferBit | BufferUsageFlags.ShaderDeviceAddressBit, false);
    }

    private ulong ScratchAddress(VkBufferResource scratch) => (scratch.Address + scratchAlignment - 1) / scratchAlignment * scratchAlignment;

    private void RayBarrier(
        PipelineStageFlags2 sourceStage,
        AccessFlags2 sourceAccess,
        PipelineStageFlags2 destinationStage,
        AccessFlags2 destinationAccess)
    {
        MemoryBarrier2 memory = new()
        {
            SType = StructureType.MemoryBarrier2Khr,
            SrcStageMask = sourceStage,
            SrcAccessMask = sourceAccess,
            DstStageMask = destinationStage,
            DstAccessMask = destinationAccess
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &memory
        };

        api.CmdPipelineBarrier2(commandBuffer, &dependency);
    }

    private void InitializeAccelerationStructures()
    {
        if ((ulong)Scene.Objects.Length > maxRayInstances)
        {
            throw new NotSupportedException("Scene exceeds Vulkan maxInstanceCount.");
        }

        foreach (SceneObject instance in Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            uint primitiveCount = range.VertexCount / 3;

            if (primitiveCount > maxRayPrimitives)
            {
                throw new NotSupportedException("Scene exceeds Vulkan maxPrimitiveCount.");
            }

            AccelerationStructureGeometryKHR geometry = new()
            {
                SType = StructureType.AccelerationStructureGeometryKhr,
                GeometryType = GeometryTypeKHR.TrianglesKhr,
                Flags = range.Opaque != 0 ? GeometryFlagsKHR.OpaqueBitKhr : GeometryFlagsKHR.None,
                Geometry = new()
                {
                    Triangles = new()
                    {
                        SType = StructureType.AccelerationStructureGeometryTrianglesDataKhr,
                        VertexFormat = Format.R32G32B32Sfloat,
                        VertexData = new()
                        {
                            DeviceAddress = sceneBuffers[0].Address + (ulong)range.FirstVertex * (uint)sizeof(SceneVertex)
                        },
                        VertexStride = (uint)sizeof(SceneVertex),
                        MaxVertex = range.VertexCount - 1,
                        IndexType = IndexType.NoneKhr
                    }
                }
            };

            AccelerationStructureBuildGeometryInfoKHR build = new()
            {
                SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
                Type = AccelerationStructureTypeKHR.BottomLevelKhr,
                Flags = BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
                Mode = BuildAccelerationStructureModeKHR.BuildKhr,
                GeometryCount = 1,
                PGeometries = &geometry
            };

            AccelerationStructureBuildSizesInfoKHR sizes = new() { SType = StructureType.AccelerationStructureBuildSizesInfoKhr };
            accelerationApi.GetAccelerationStructureBuildSizes(device, AccelerationStructureBuildTypeKHR.DeviceKhr, &build, &primitiveCount, &sizes);
            VkAcceleration bottom = CreateAcceleration(AccelerationStructureTypeKHR.BottomLevelKhr, sizes.AccelerationStructureSize);
            bottomLevels.Add(bottom);
            VkBufferResource scratch = CreateRayScratch(sizes.BuildScratchSize);

            // Initial upload submission retains each scratch allocation until its GPU fence completes.
            uploads.Add(scratch);
            build.DstAccelerationStructure = bottom.Handle;
            build.ScratchData.DeviceAddress = ScratchAddress(scratch);
            AccelerationStructureBuildRangeInfoKHR rangeInfo = new()
            {
                PrimitiveCount = primitiveCount
            };

            AccelerationStructureBuildRangeInfoKHR* ranges = &rangeInfo;
            accelerationApi.CmdBuildAccelerationStructures(commandBuffer, 1, &build, &ranges);
        }

        RayBarrier(
            PipelineStageFlags2.AccelerationStructureBuildBitKhr,
            AccessFlags2.AccelerationStructureWriteBitKhr,
            PipelineStageFlags2.AccelerationStructureBuildBitKhr,
            AccessFlags2.AccelerationStructureReadBitKhr);
        uint count = (uint)Scene.Objects.Length;
        AccelerationStructureGeometryKHR instances = InstanceGeometry(0);
        AccelerationStructureBuildGeometryInfoKHR topBuild = new()
        {
            SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
            Type = AccelerationStructureTypeKHR.TopLevelKhr,
            Flags = BuildAccelerationStructureFlagsKHR.AllowUpdateBitKhr | BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
            Mode = BuildAccelerationStructureModeKHR.BuildKhr,
            GeometryCount = 1,
            PGeometries = &instances
        };

        AccelerationStructureBuildSizesInfoKHR topSizes = new() { SType = StructureType.AccelerationStructureBuildSizesInfoKhr };
        accelerationApi.GetAccelerationStructureBuildSizes(device, AccelerationStructureBuildTypeKHR.DeviceKhr, &topBuild, &count, &topSizes);

        foreach (VkFrame frame in slots)
        {
            frame.Tlas = CreateAcceleration(AccelerationStructureTypeKHR.TopLevelKhr, topSizes.AccelerationStructureSize);
            frame.RayScratch = CreateRayScratch(Math.Max(topSizes.BuildScratchSize, topSizes.UpdateScratchSize));
            frame.RayInstances = CreateBuffer(
                (ulong)(Scene.Objects.Length * sizeof(AccelerationStructureInstanceKHR)),
                BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr | BufferUsageFlags.ShaderDeviceAddressBit,
                true);
        }
    }

    private static AccelerationStructureGeometryKHR InstanceGeometry(ulong address) => new()
    {
        SType = StructureType.AccelerationStructureGeometryKhr,
        GeometryType = GeometryTypeKHR.InstancesKhr,
        Geometry = new()
        {
            Instances = new()
            {
                SType = StructureType.AccelerationStructureGeometryInstancesDataKhr,
                Data = new()
                {
                    DeviceAddress = address
                }
            }
        }
    };

    private static AccelerationStructureInstanceKHR CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        System.Numerics.Vector4 offset = instance.Offset;

        TransformMatrixKHR transform = default;
        transform.Matrix[0] = 1;
        transform.Matrix[3] = offset.X;
        transform.Matrix[5] = 1;
        transform.Matrix[7] = offset.Y;
        transform.Matrix[10] = 1;
        transform.Matrix[11] = offset.Z;

        return new()
        {
            Transform = transform,
            InstanceCustomIndex = id,
            Mask = byte.MaxValue,
            Flags = instance.Geometry.DoubleSided != 0 ? GeometryInstanceFlagsKHR.TriangleFacingCullDisableBitKhr : GeometryInstanceFlagsKHR.None,
            AccelerationStructureReference = address
        };
    }

    private void UpdateAccelerationStructure(VkFrame frame)
    {
        // BeginCommands has waited for this slot's fence; each slot owns independent
        // TLAS, scratch and instance allocations, including during in-place updates.
        Span<AccelerationStructureInstanceKHR> instances = new(frame.RayInstances!.Mapped, Scene.Objects.Length);

        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Scene.Objects[i], (uint)i, bottomLevels[i].Address);
        }

        RayBarrier(
            PipelineStageFlags2.HostBit | PipelineStageFlags2.ComputeShaderBit | PipelineStageFlags2.AccelerationStructureBuildBitKhr,
            AccessFlags2.HostWriteBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr,
            PipelineStageFlags2.AccelerationStructureBuildBitKhr,
            AccessFlags2.ShaderReadBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr);
        AccelerationStructureGeometryKHR geometry = InstanceGeometry(frame.RayInstances.Address);
        AccelerationStructureBuildGeometryInfoKHR build = new()
        {
            SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
            Type = AccelerationStructureTypeKHR.TopLevelKhr,
            Flags = BuildAccelerationStructureFlagsKHR.AllowUpdateBitKhr | BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
            Mode = frame.TlasBuilt ? BuildAccelerationStructureModeKHR.UpdateKhr : BuildAccelerationStructureModeKHR.BuildKhr,
            SrcAccelerationStructure = frame.TlasBuilt ? frame.Tlas!.Handle : default,
            DstAccelerationStructure = frame.Tlas!.Handle,
            GeometryCount = 1,
            PGeometries = &geometry,
            ScratchData = new()
            {
                DeviceAddress = ScratchAddress(frame.RayScratch!)
            }
        };

        AccelerationStructureBuildRangeInfoKHR range = new()
        {
            PrimitiveCount = (uint)Scene.Objects.Length
        };

        AccelerationStructureBuildRangeInfoKHR* ranges = &range;
        accelerationApi.CmdBuildAccelerationStructures(commandBuffer, 1, &build, &ranges);
        RayBarrier(
            PipelineStageFlags2.AccelerationStructureBuildBitKhr,
            AccessFlags2.AccelerationStructureWriteBitKhr,
            PipelineStageFlags2.ComputeShaderBit,
            AccessFlags2.AccelerationStructureReadBitKhr);
        frame.TlasBuilt = true;
    }
}
