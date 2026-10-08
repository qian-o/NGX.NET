using System.Runtime.InteropServices;
using NGX.NET;
using Showcase.Models;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private sealed class VkAcceleration : IDisposable
    {
        public required KhrAccelerationStructure Api;

        public required Device Device;

        public required VkBufferResource Storage;

        public AccelerationStructureKHR Handle;

        public ulong Address;

        public void Dispose()
        {
            Api.DestroyAccelerationStructure(Device, Handle, null);
            Storage.Dispose();
        }
    }

    private sealed class VkBufferResource : IDisposable
    {
        public required Vk Api;

        public required Device Device;

        public Buffer Buffer;

        public DeviceMemory Memory;

        public ulong Size;

        public ulong Address;

        public void* Mapped;

        public void Write<T>(ReadOnlySpan<T> values, int offset = 0) where T : unmanaged => MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>((byte*)Mapped + offset, values.Length * sizeof(T)));

        public void Dispose()
        {
            if (Mapped != null)
            {
                Api.UnmapMemory(Device, Memory);
            }

            Api.DestroyBuffer(Device, Buffer, null);
            Api.FreeMemory(Device, Memory, null);
        }
    }

    private sealed class VkTexture : GpuImage
    {
        public required Vk Api;

        public required Device Device;

        public Image Texture;

        public ImageView View;

        public DeviceMemory Memory;

        public ImageLayout Layout;

        public ImageUsageFlags Usage;

        public override NativeImage Describe() => new()
        {
            Vulkan = new()
            {
                Type = NGXResourceVKType.VkImageView,
                ReadWrite = (Usage & ImageUsageFlags.StorageBit) != 0,
                Resource = new()
                {
                    ImageViewInfo = new()
                    {
                        Image = (nint)Texture.Handle,
                        ImageView = (nint)View.Handle,
                        Format = (NGXVkFormat)NativeFormat(Format),
                        Width = (uint)Width,
                        Height = (uint)Height,
                        SubresourceRange = new()
                        {
                            AspectMask = (uint)(Format == ImageFormat.Depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit),
                            LevelCount = 1,
                            LayerCount = (uint)Layers
                        }
                    }
                }
            }
        };

        public override void Dispose()
        {
            Api.DestroyImageView(Device, View, null);
            Api.DestroyImage(Device, Texture, null);
            Api.FreeMemory(Device, Memory, null);
        }
    }

    private sealed class VkFrame : IDisposable
    {
        public required Vk Api;

        public required Device Device;

        public CommandPool Pool;

        public CommandBuffer Command;

        public Fence Fence;

        public Semaphore RenderComplete;

        public DescriptorSet Descriptors;

        public VkBufferResource Constants = null!;

        public VkBufferResource Objects = null!;

        public VkBufferResource? Vertices;

        public VkBufferResource? Indices;

        public VkAcceleration? Tlas;

        public VkBufferResource? RayScratch;

        public VkBufferResource? RayInstances;

        public bool TlasBuilt;

        public void Dispose()
        {
            Tlas?.Dispose();
            RayScratch?.Dispose();
            RayInstances?.Dispose();
            Vertices?.Dispose();
            Indices?.Dispose();
            Constants?.Dispose();
            Objects?.Dispose();
            Api.DestroySemaphore(Device, RenderComplete, null);
            Api.DestroyFence(Device, Fence, null);
            Api.DestroyCommandPool(Device, Pool, null);
        }
    }
}
