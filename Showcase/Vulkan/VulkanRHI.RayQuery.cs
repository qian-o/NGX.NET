using Showcase.Models;
using Vortice.Vulkan;

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

    private VkAcceleration CreateAcceleration(VkAccelerationStructureTypeKHR type, ulong size)
    {
        if (size == 0)
        {
            throw new InvalidOperationException("Vulkan returned an empty acceleration-structure allocation size.");
        }

        VkBufferResource storage = CreateBuffer(size, VkBufferUsageFlags.AccelerationStructureStorageKHR | VkBufferUsageFlags.ShaderDeviceAddress, false);
        VkAccelerationStructureCreateInfoKHR create = new()
        {
            buffer = storage.Buffer,
            size = size,
            type = type
        };

        VkAccelerationStructureKHR handle;

        try
        {
            Check(api.vkCreateAccelerationStructureKHR(&create, null, &handle), "vkCreateAccelerationStructureKHR");
        }
        catch
        {
            storage.Dispose();

            throw;
        }

        VkAcceleration acceleration = new()
        {
            Api = api,
            Storage = storage,
            Handle = handle
        };

        VkAccelerationStructureDeviceAddressInfoKHR address = new()
        {
            accelerationStructure = handle
        };

        acceleration.Address = api.vkGetAccelerationStructureDeviceAddressKHR(&address);

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

        return CreateBuffer(checked(size + scratchAlignment - 1), VkBufferUsageFlags.StorageBuffer | VkBufferUsageFlags.ShaderDeviceAddress, false);
    }

    private ulong ScratchAddress(VkBufferResource scratch) => (scratch.Address + scratchAlignment - 1) / scratchAlignment * scratchAlignment;

    private void RayBarrier(
        VkPipelineStageFlags2 sourceStage,
        VkAccessFlags2 sourceAccess,
        VkPipelineStageFlags2 destinationStage,
        VkAccessFlags2 destinationAccess)
    {
        VkMemoryBarrier2 memory = new()
        {
            srcStageMask = sourceStage,
            srcAccessMask = sourceAccess,
            dstStageMask = destinationStage,
            dstAccessMask = destinationAccess
        };

        VkDependencyInfo dependency = new()
        {
            memoryBarrierCount = 1,
            pMemoryBarriers = &memory
        };

        api.vkCmdPipelineBarrier2(commandBuffer, &dependency);
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

            VkAccelerationStructureGeometryKHR geometry = new()
            {
                geometryType = VkGeometryTypeKHR.Triangles,
                flags = range.Opaque != 0 ? VkGeometryFlagsKHR.Opaque : VkGeometryFlagsKHR.None,
                geometry = new()
                {
                    triangles = new()
                    {
                        vertexFormat = VkFormat.R32G32B32Sfloat,
                        vertexData = new()
                        {
                            deviceAddress = sceneBuffers[0].Address + (ulong)range.FirstVertex * (uint)sizeof(SceneVertex)
                        },
                        vertexStride = (uint)sizeof(SceneVertex),
                        maxVertex = range.VertexCount - 1,
                        indexType = VkIndexType.NoneKHR
                    }
                }
            };

            VkAccelerationStructureBuildGeometryInfoKHR build = new()
            {
                type = VkAccelerationStructureTypeKHR.BottomLevel,
                flags = VkBuildAccelerationStructureFlagsKHR.PreferFastTrace,
                mode = VkBuildAccelerationStructureModeKHR.Build,
                geometryCount = 1,
                pGeometries = &geometry
            };

            VkAccelerationStructureBuildSizesInfoKHR sizes = new();
            api.vkGetAccelerationStructureBuildSizesKHR(VkAccelerationStructureBuildTypeKHR.Device, &build, &primitiveCount, &sizes);
            VkAcceleration bottom = CreateAcceleration(VkAccelerationStructureTypeKHR.BottomLevel, sizes.accelerationStructureSize);
            bottomLevels.Add(bottom);
            VkBufferResource scratch = CreateRayScratch(sizes.buildScratchSize);

            // Initial upload submission retains each scratch allocation until its GPU fence completes.
            uploads.Add(scratch);
            build.dstAccelerationStructure = bottom.Handle;
            build.scratchData.deviceAddress = ScratchAddress(scratch);
            VkAccelerationStructureBuildRangeInfoKHR rangeInfo = new()
            {
                primitiveCount = primitiveCount
            };

            VkAccelerationStructureBuildRangeInfoKHR* ranges = &rangeInfo;
            api.vkCmdBuildAccelerationStructuresKHR(commandBuffer, 1, &build, &ranges);
        }

        RayBarrier(
            VkPipelineStageFlags2.AccelerationStructureBuildKHR,
            VkAccessFlags2.AccelerationStructureWriteKHR,
            VkPipelineStageFlags2.AccelerationStructureBuildKHR,
            VkAccessFlags2.AccelerationStructureReadKHR);
        uint count = (uint)Scene.Objects.Length;
        VkAccelerationStructureGeometryKHR instances = InstanceGeometry(0);
        VkAccelerationStructureBuildGeometryInfoKHR topBuild = new()
        {
            type = VkAccelerationStructureTypeKHR.TopLevel,
            flags = VkBuildAccelerationStructureFlagsKHR.AllowUpdate | VkBuildAccelerationStructureFlagsKHR.PreferFastTrace,
            mode = VkBuildAccelerationStructureModeKHR.Build,
            geometryCount = 1,
            pGeometries = &instances
        };

        VkAccelerationStructureBuildSizesInfoKHR topSizes = new();
        api.vkGetAccelerationStructureBuildSizesKHR(VkAccelerationStructureBuildTypeKHR.Device, &topBuild, &count, &topSizes);

        foreach (VkFrame frame in slots)
        {
            frame.Tlas = CreateAcceleration(VkAccelerationStructureTypeKHR.TopLevel, topSizes.accelerationStructureSize);
            frame.RayScratch = CreateRayScratch(Math.Max(topSizes.buildScratchSize, topSizes.updateScratchSize));
            frame.RayInstances = CreateBuffer(
                (ulong)(Scene.Objects.Length * sizeof(VkAccelerationStructureInstanceKHR)),
                VkBufferUsageFlags.AccelerationStructureBuildInputReadOnlyKHR | VkBufferUsageFlags.ShaderDeviceAddress,
                true);
        }
    }

    private static VkAccelerationStructureGeometryKHR InstanceGeometry(ulong address) => new()
    {
        geometryType = VkGeometryTypeKHR.Instances,
        geometry = new()
        {
            instances = new()
            {
                data = new()
                {
                    deviceAddress = address
                }
            }
        }
    };

    private static VkAccelerationStructureInstanceKHR CreateRayInstance(SceneObject instance, uint id, ulong address)
    {
        System.Numerics.Vector4 offset = instance.Offset;

        // Vortice exposes overlapping native bitfields. Set the custom index
        // before the mask and the address last; the packed GPU record is 64 bytes.
        return new()
        {
            transform = new(1, 0, 0, offset.X, 0, 1, 0, offset.Y, 0, 0, 1, offset.Z),
            instanceCustomIndex = id,
            mask = byte.MaxValue,
            flags = instance.Geometry.DoubleSided != 0 ? VkGeometryInstanceFlagsKHR.TriangleFacingCullDisable : VkGeometryInstanceFlagsKHR.None,
            accelerationStructureReference = address
        };
    }

    private void UpdateAccelerationStructure(VkFrame frame)
    {
        // BeginCommands has waited for this slot's fence; each slot owns independent
        // TLAS, scratch and instance allocations, including during in-place updates.
        Span<VkAccelerationStructureInstanceKHR> instances = new(frame.RayInstances!.Mapped, Scene.Objects.Length);

        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Scene.Objects[i], (uint)i, bottomLevels[i].Address);
        }

        RayBarrier(
            VkPipelineStageFlags2.Host | VkPipelineStageFlags2.ComputeShader | VkPipelineStageFlags2.AccelerationStructureBuildKHR,
            VkAccessFlags2.HostWrite | VkAccessFlags2.AccelerationStructureReadKHR | VkAccessFlags2.AccelerationStructureWriteKHR,
            VkPipelineStageFlags2.AccelerationStructureBuildKHR,
            VkAccessFlags2.ShaderRead | VkAccessFlags2.AccelerationStructureReadKHR | VkAccessFlags2.AccelerationStructureWriteKHR);
        VkAccelerationStructureGeometryKHR geometry = InstanceGeometry(frame.RayInstances.Address);
        VkAccelerationStructureBuildGeometryInfoKHR build = new()
        {
            type = VkAccelerationStructureTypeKHR.TopLevel,
            flags = VkBuildAccelerationStructureFlagsKHR.AllowUpdate | VkBuildAccelerationStructureFlagsKHR.PreferFastTrace,
            mode = frame.TlasBuilt ? VkBuildAccelerationStructureModeKHR.Update : VkBuildAccelerationStructureModeKHR.Build,
            srcAccelerationStructure = frame.TlasBuilt ? frame.Tlas!.Handle : default,
            dstAccelerationStructure = frame.Tlas!.Handle,
            geometryCount = 1,
            pGeometries = &geometry,
            scratchData = new()
            {
                deviceAddress = ScratchAddress(frame.RayScratch!)
            }
        };

        VkAccelerationStructureBuildRangeInfoKHR range = new()
        {
            primitiveCount = (uint)Scene.Objects.Length
        };

        VkAccelerationStructureBuildRangeInfoKHR* ranges = &range;
        api.vkCmdBuildAccelerationStructuresKHR(commandBuffer, 1, &build, &ranges);
        RayBarrier(
            VkPipelineStageFlags2.AccelerationStructureBuildKHR,
            VkAccessFlags2.AccelerationStructureWriteKHR,
            VkPipelineStageFlags2.ComputeShader,
            VkAccessFlags2.AccelerationStructureReadKHR);
        frame.TlasBuilt = true;
    }
}
