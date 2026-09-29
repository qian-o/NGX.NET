using System.Runtime.InteropServices;
using NGX.NET;
using Showcase.Models;
using Vortice.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private sealed class NativeText(string text) : IDisposable
    {
        private readonly nint memory = Marshal.StringToCoTaskMemUTF8(text);

        public byte* Pointer => (byte*)memory;

        public void Dispose() => Marshal.FreeCoTaskMem(memory);
    }

    private sealed class VkAcceleration : IDisposable
    {
        public required VkDeviceApi Api;
        public required VkBufferResource Storage;
        public VkAccelerationStructureKHR Handle;
        public ulong Address;

        public void Dispose()
        {
            Api.vkDestroyAccelerationStructureKHR(Handle);
            Storage.Dispose();
        }
    }

    private sealed class VkBufferResource : IDisposable
    {
        public required VkDeviceApi Api;
        public VkBuffer Buffer;
        public VkDeviceMemory Memory;
        public ulong Size;
        public ulong Address;
        public void* Mapped;

        public void Write<T>(ReadOnlySpan<T> values, int offset = 0)
            where T : unmanaged => MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>((byte*)Mapped + offset, values.Length * sizeof(T)));

        public void Dispose()
        {
            if (Mapped != null)
            {
                Api.vkUnmapMemory(Memory);
            }

            Api.vkDestroyBuffer(Buffer);
            Api.vkFreeMemory(Memory);
        }
    }

    private sealed class VkTexture : GpuImage
    {
        public required VkDeviceApi Api;
        public VkImage Texture;
        public VkImageView View;
        public VkDeviceMemory Memory;
        public VkImageLayout Layout;
        public VkImageUsageFlags Usage;

        public override NativeImage Describe() => new()
        {
            Vulkan = new()
            {
                Type = NGXResourceVKType.VKImageview,
                ReadWrite = (Usage & VkImageUsageFlags.Storage) != 0,
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
                            AspectMask = (uint)(Format == ImageFormat.Depth ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color),
                            LevelCount = 1,
                            LayerCount = (uint)Layers
                        }
                    }
                }
            }
        };

        public override void Dispose()
        {
            Api.vkDestroyImageView(View);
            Api.vkDestroyImage(Texture);
            Api.vkFreeMemory(Memory);
        }
    }

    private sealed class VkFrame : IDisposable
    {
        public required VkDeviceApi Api;
        public VkCommandPool Pool;
        public VkCommandBuffer Command;
        public VkFence Fence;
        public VkSemaphore RenderComplete;
        public VkDescriptorSet Descriptors;
        public required VkBufferResource Constants;
        public required VkBufferResource Objects;
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
            Constants.Dispose();
            Objects.Dispose();
            Api.vkDestroySemaphore(RenderComplete);
            Api.vkDestroyFence(Fence);
            Api.vkDestroyCommandPool(Pool);
        }
    }
}
