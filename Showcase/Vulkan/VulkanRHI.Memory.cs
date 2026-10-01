using Showcase.Models;
using Silk.NET.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private uint MemoryType(uint bits, MemoryPropertyFlags flags)
    {
        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((bits & (1u << (int)i)) != 0 && (memoryProperties.MemoryTypes[(int)i].PropertyFlags & flags) == flags)
            {
                return i;
            }
        }

        throw new NotSupportedException($"No Vulkan memory type supports {flags}.");
    }

    private VkBufferResource CreateBuffer(ulong size, BufferUsageFlags usage, bool host)
    {
        VkBufferResource resource = new()
        {
            Api = api,
            Device = device,
            Size = Math.Max(size, 4)
        };

        BufferCreateInfo info = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = resource.Size,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        Check(api.CreateBuffer(device, &info, null, out resource.Buffer), "vkCreateBuffer");
        try
        {
            api.GetBufferMemoryRequirements(device, resource.Buffer, out MemoryRequirements requirements);
            bool addressable = (usage & BufferUsageFlags.ShaderDeviceAddressBit) != 0;
            MemoryAllocateFlagsInfo flags = new()
            {
                SType = StructureType.MemoryAllocateFlagsInfo,
                Flags = MemoryAllocateFlags.DeviceAddressBit
            };

            MemoryAllocateInfo allocation = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                PNext = addressable ? &flags : null,
                AllocationSize = requirements.Size,
                MemoryTypeIndex = MemoryType(requirements.MemoryTypeBits, host ? MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit : MemoryPropertyFlags.DeviceLocalBit)
            };

            Check(api.AllocateMemory(device, &allocation, null, out DeviceMemory memory), "vkAllocateMemory(buffer)");
            resource.Memory = memory;
            Check(api.BindBufferMemory(device, resource.Buffer, resource.Memory, 0), "vkBindBufferMemory");

            if (addressable)
            {
                BufferDeviceAddressInfo address = new()
                {
                    SType = StructureType.BufferDeviceAddressInfoKhr,
                    Buffer = resource.Buffer
                };

                resource.Address = api.GetBufferDeviceAddress(device, &address);
            }

            if (host)
            {
                void* mapped;
                Check(api.MapMemory(device, resource.Memory, 0, resource.Size, 0, &mapped), "vkMapMemory");
                resource.Mapped = mapped;
            }

            return resource;
        }
        catch
        {
            resource.Dispose();

            throw;
        }
    }

    private VkBufferResource StaticBuffer<T>(ReadOnlySpan<T> data, bool rayGeometry = false) where T : unmanaged
    {
        ulong size = (ulong)(data.Length * sizeof(T));
        BufferUsageFlags usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit;

        if (rayGeometry && RayQuerySupported)
        {
            usage |= BufferUsageFlags.ShaderDeviceAddressBit | BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr;
        }

        VkBufferResource buffer = CreateBuffer(size, usage, false);
        try
        {
            VkBufferResource upload = CreateBuffer(size, BufferUsageFlags.TransferSrcBit, true);
            uploads.Add(upload);
            upload.Write(data);
            BufferCopy copy = new()
            {
                Size = size
            };

            api.CmdCopyBuffer(commandBuffer, upload.Buffer, buffer.Buffer, 1, &copy);

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
        ImageUsageFlags usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit;
        usage |= format == ImageFormat.Depth ? ImageUsageFlags.DepthStencilAttachmentBit : ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.StorageBit;
        VkTexture texture = new()
        {
            Api = api,
            Device = device,
            Width = width,
            Height = height,
            Layers = layers,
            Format = format,
            Usage = usage
        };

        ImageCreateInfo info = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = NativeFormat(format),
            Extent = new((uint)width, (uint)height, 1),
            MipLevels = 1,
            ArrayLayers = (uint)layers,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = usage
        };

        Check(api.CreateImage(device, &info, null, out texture.Texture), "vkCreateImage");
        try
        {
            api.GetImageMemoryRequirements(device, texture.Texture, out MemoryRequirements requirements);
            MemoryAllocateInfo allocation = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = requirements.Size,
                MemoryTypeIndex = MemoryType(requirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };

            Check(api.AllocateMemory(device, &allocation, null, out DeviceMemory memory), "vkAllocateMemory(image)");
            texture.Memory = memory;
            Check(api.BindImageMemory(device, texture.Texture, texture.Memory, 0), "vkBindImageMemory");
            ImageViewCreateInfo view = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = texture.Texture,
                ViewType = layers > 1 ? ImageViewType.Type2DArray : ImageViewType.Type2D,
                Format = NativeFormat(format),
                SubresourceRange = Range(format, layers)
            };

            Check(api.CreateImageView(device, &view, null, out ImageView imageView), "vkCreateImageView");
            texture.View = imageView;

            return texture;
        }
        catch
        {
            texture.Dispose();

            throw;
        }
    }

    private static Format NativeFormat(ImageFormat format) => format switch
    {
        ImageFormat.Rgba16 => Format.R16G16B16A16Sfloat,
        ImageFormat.Rgba32 => Format.R32G32B32A32Sfloat,
        ImageFormat.Rg16 => Format.R16G16Sfloat,
        ImageFormat.Float => Format.R32Sfloat,
        ImageFormat.Depth => Format.D32Sfloat,
        _ => Format.R8G8B8A8Unorm
    };

    private static ImageSubresourceRange Range(ImageFormat format, int layers = 1) => new(format == ImageFormat.Depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit, 0, 1, 0, (uint)layers);
}
