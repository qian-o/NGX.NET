using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;
using Streamline.NET;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Resource = Streamline.NET.Resource;

namespace Showcase;

internal sealed unsafe partial class VulkanRHI(Window window, UserInterface ui) : RHI(window, ui)
{
    public override string BackendName => "Vulkan";
    protected override nint Command => commandBuffer.Handle;
    private VkInstance instance;
    private VkInstanceApi instanceApi = null!;
    private VkDevice device;
    private VkDeviceApi api = null!;
    private VkPhysicalDevice physical;
    private VkPhysicalDeviceMemoryProperties memoryProperties;
    private VkQueue queue;
    private uint queueFamily;
    private VkSurfaceKHR surface;
    private VkSwapchainKHR swapChain;
    private VkImage[] backBuffers = [];
    private VkImageLayout[] backLayouts = [];
    private VkSemaphore[] presentSemaphores = [];
    private uint imageIndex;
    private VkCommandBuffer commandBuffer;
    private readonly VkFrame[] slots = new VkFrame[RenderLayout.FramesInFlight];
    private readonly VkBufferResource[] sceneBuffers = new VkBufferResource[4];
    private readonly List<VkBufferResource> uploads = [];
    private VkTexture font = null!;
    private VkDescriptorSetLayout descriptorLayout;
    private VkDescriptorPool descriptorPool;
    private VkPipelineLayout pipelineLayout;
    private VkPipeline scenePipeline, uiPipeline, shadowPipeline;
    private readonly Dictionary<ComputePass, VkPipeline> pipelines = [];
    private VkSampler sampler;
    private VkQueryPool queryPool;
    private double timestampPeriod;
    private uint timestampBits;
    private int uniformStride = RenderLayout.UniformStride;
    private int constantIndex;
    private bool recording;

    private sealed class VkBufferResource : IDisposable
    {
        public required VkDeviceApi Api;
        public VkBuffer Buffer;
        public VkDeviceMemory Memory;
        public ulong Size;
        public ulong Address;
        public void* Mapped;
        public void Write<T>(ReadOnlySpan<T> values, int offset = 0) where T : unmanaged => MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>((byte*)Mapped + offset, values.Length * sizeof(T)));
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
        public override Resource Describe() => new()
        {
            Type = ResourceType.Tex2d,
            Native = (void*)Texture.Handle,
            Memory = (void*)Memory.Handle,
            View = (void*)View.Handle,
            State = (uint)Layout,
            NativeFormat = (uint)NativeFormat(Format),
            Width = (uint)Width,
            Height = (uint)Height,
            MipLevels = 1,
            ArrayLayers = 1,
            Usage = (uint)Usage
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
        public VkSemaphore Acquire;
        public VkDescriptorSet Descriptors;
        public required VkBufferResource Constants;
        public required VkBufferResource Objects;
        public VkBufferResource? Vertices, Indices;
        public bool Submitted;
        public bool TimestampsPending;
        public VkAcceleration? Tlas;
        public VkBufferResource? RayScratch, RayInstances;
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
            Api.vkDestroySemaphore(Acquire);
            Api.vkDestroyFence(Fence);
            Api.vkDestroyCommandPool(Pool);
        }
    }

    private static void Check(VkResult result, string operation)
    {
        if (result != VkResult.Success)
        {
            throw new InvalidOperationException($"{operation}: {result}");
        }
    }

    protected override void InitializeDevice()
    {
        NativeLibrary.GetExport(Streamline.Module, "vkGetInstanceProcAddr");
        NativeLibrary.GetExport(Streamline.Module, "vkGetDeviceProcAddr");
        Check(vkInitialize(Path.Combine(AppContext.BaseDirectory, "sl.interposer.dll")), "vkInitialize(interposer)");
        VkApplicationInfo application = new()
        {
            apiVersion = VkVersion.Version_1_3
        };
        using VkStringArray extensions = new(["VK_KHR_surface"u8, "VK_KHR_win32_surface"u8]);
        VkInstanceCreateInfo create = new()
        {
            pApplicationInfo = &application,
            enabledExtensionCount = extensions.Length,
            ppEnabledExtensionNames = extensions
        };
        Check(vkCreateInstance(&create, out instance), "vkCreateInstance");
        instanceApi = GetApi(instance);
        VkWin32SurfaceCreateInfoKHR surfaceInfo = new()
        {
            hwnd = Window.Handle,
            hinstance = Window.Instance
        };
        Check(instanceApi.vkCreateWin32SurfaceKHR(&surfaceInfo, null, out surface), "vkCreateWin32SurfaceKHR");
        Check(instanceApi.vkEnumeratePhysicalDevices(out uint count), "vkEnumeratePhysicalDevices(count)");
        VkPhysicalDevice[] devices = new VkPhysicalDevice[count];
        Check(instanceApi.vkEnumeratePhysicalDevices(devices), "vkEnumeratePhysicalDevices");
        int bestScore = -1;
        foreach (VkPhysicalDevice candidate in devices)
        {
            instanceApi.vkGetPhysicalDeviceProperties(candidate, out VkPhysicalDeviceProperties properties);
            if (properties.apiVersion < VkVersion.Version_1_3)
            {
                continue;
            }
            VkPhysicalDeviceRayQueryFeaturesKHR queryFeatures = new();
            VkPhysicalDeviceAccelerationStructureFeaturesKHR accelerationFeatures = new()
            {
                pNext = &queryFeatures
            };
            Vortice.Vulkan.VkPhysicalDeviceVulkan13Features features13 = new()
            {
                pNext = &accelerationFeatures
            };
            Vortice.Vulkan.VkPhysicalDeviceVulkan12Features features12 = new()
            {
                pNext = &features13
            };
            VkPhysicalDeviceVulkan11Features features11 = new()
            {
                pNext = &features12
            };
            VkPhysicalDeviceFeatures2 features = new()
            {
                pNext = &features11
            };
            instanceApi.vkGetPhysicalDeviceFeatures2(candidate, &features);
            if (!features11.shaderDrawParameters || !features13.dynamicRendering || !features13.synchronization2 || !features.features.shaderStorageImageReadWithoutFormat || !features.features.shaderStorageImageWriteWithoutFormat || !features.features.shaderStorageImageExtendedFormats)
            {
                continue;
            }

            Check(instanceApi.vkEnumerateDeviceExtensionProperties(candidate, out uint extensionCount), "vkEnumerateDeviceExtensionProperties(count)");
            VkExtensionProperties[] availableExtensions = new VkExtensionProperties[extensionCount];
            Check(instanceApi.vkEnumerateDeviceExtensionProperties(candidate, availableExtensions), "vkEnumerateDeviceExtensionProperties");
            HashSet<string> extensionNames = [];
            foreach (VkExtensionProperties extension in availableExtensions)
            {
                extensionNames.Add(Marshal.PtrToStringUTF8((nint)extension.extensionName)!);
            }
            bool rayQuery = queryFeatures.rayQuery && accelerationFeatures.accelerationStructure && features12.bufferDeviceAddress
                && RayExtensions.All(extensionNames.Contains);
            instanceApi.vkGetPhysicalDeviceFormatProperties(candidate, VkFormat.R32G32B32Sfloat, out VkFormatProperties vertexFormat);
            rayQuery &= (vertexFormat.bufferFeatures & VkFormatFeatureFlags.AccelerationStructureVertexBufferKHR) != 0;
            instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(candidate, out uint familyCount);
            VkQueueFamilyProperties[] families = new VkQueueFamilyProperties[familyCount];
            instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(candidate, families);
            for (uint i = 0; i < familyCount; i++)
            {
                Check(instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(candidate, i, surface, out VkBool32 present), "vkGetPhysicalDeviceSurfaceSupportKHR");
                if (!present || (families[i].queueFlags & (VkQueueFlags.Graphics | VkQueueFlags.Compute)) != (VkQueueFlags.Graphics | VkQueueFlags.Compute))
                {
                    continue;
                }

                int score = properties.vendorID == 0x10DE ? 2 : properties.deviceType == VkPhysicalDeviceType.DiscreteGpu ? 1 : 0;
                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                physical = candidate;
                queueFamily = i;
                RayQuerySupported = rayQuery;
                RayQueryStatus = rayQuery ? "VK_KHR_ray_query" : "Requires Vulkan rayQuery, accelerationStructure and bufferDeviceAddress";
                AdapterName = Marshal.PtrToStringUTF8((nint)properties.deviceName) ?? "Vulkan GPU";
                timestampPeriod = properties.limits.timestampPeriod;
                timestampBits = families[i].timestampValidBits;
                ulong alignment = properties.limits.minUniformBufferOffsetAlignment;
                uniformStride = (int)(((ulong)RenderLayout.UniformStride + alignment - 1) / alignment * alignment);
            }
        }
        if (physical.IsNull)
        {
            throw new NotSupportedException("A Vulkan 1.3 graphics/compute/present queue and storage-image support are required.");
        }

        instanceApi.vkGetPhysicalDeviceMemoryProperties(physical, out memoryProperties);
        float priority = 1;
        VkDeviceQueueCreateInfo queueInfo = new()
        {
            queueFamilyIndex = queueFamily,
            queueCount = 1,
            pQueuePriorities = &priority
        };
        VkPhysicalDeviceRayQueryFeaturesKHR enabledRayQuery = new()
        {
            rayQuery = true
        };
        VkPhysicalDeviceAccelerationStructureFeaturesKHR enabledAcceleration = new()
        {
            accelerationStructure = true,
            pNext = &enabledRayQuery
        };
        Vortice.Vulkan.VkPhysicalDeviceVulkan13Features enabled13 = new()
        {
            dynamicRendering = true,
            synchronization2 = true,
            pNext = RayQuerySupported ? &enabledAcceleration : null
        };
        Vortice.Vulkan.VkPhysicalDeviceVulkan12Features enabled12 = new()
        {
            pNext = &enabled13,
            bufferDeviceAddress = RayQuerySupported
        };
        VkPhysicalDeviceVulkan11Features enabled11 = new()
        {
            pNext = &enabled12,
            shaderDrawParameters = true
        };
        VkPhysicalDeviceFeatures2 enabled = new()
        {
            pNext = &enabled11,
            features = new()
            {
                shaderStorageImageReadWithoutFormat = true,
                shaderStorageImageWriteWithoutFormat = true,
                shaderStorageImageExtendedFormats = true
            }
        };
        List<VkUtf8String> requestedExtensions = ["VK_KHR_swapchain"u8];
        if (RayQuerySupported)
        {
            requestedExtensions.AddRange(["VK_KHR_acceleration_structure"u8, "VK_KHR_ray_query"u8, "VK_KHR_deferred_host_operations"u8]);
            VkPhysicalDeviceAccelerationStructurePropertiesKHR accelerationProperties = new();
            VkPhysicalDeviceProperties2 properties = new()
            {
                pNext = &accelerationProperties
            };
            instanceApi.vkGetPhysicalDeviceProperties2(physical, &properties);
            scratchAlignment = accelerationProperties.minAccelerationStructureScratchOffsetAlignment;
            maxRayInstances = accelerationProperties.maxInstanceCount;
            maxRayPrimitives = accelerationProperties.maxPrimitiveCount;
        }
        using VkStringArray deviceExtensions = new(requestedExtensions);
        VkDeviceCreateInfo deviceInfo = new()
        {
            pNext = &enabled,
            queueCreateInfoCount = 1,
            pQueueCreateInfos = &queueInfo,
            enabledExtensionCount = deviceExtensions.Length,
            ppEnabledExtensionNames = deviceExtensions
        };
        // The interposer augments its own required features/extensions and registers the device.
        Check(instanceApi.vkCreateDevice(physical, &deviceInfo, null, out device), "vkCreateDevice");
        api = GetApi(instance, device);
        api.vkGetDeviceQueue(queueFamily, 0, out queue);
        AdapterInfo adapter = new()
        {
            VkPhysicalDevice = (void*)physical.Handle
        };
        Streamline.QueryFeatures(adapter);
    }

    private uint MemoryType(uint bits, VkMemoryPropertyFlags flags)
    {
        for (uint i = 0; i < memoryProperties.memoryTypeCount; i++)
        {
            if ((bits & (1u << (int)i)) != 0 && (memoryProperties.memoryTypes[(int)i].propertyFlags & flags) == flags)
            {
                return i;
            }
        }

        throw new NotSupportedException($"No Vulkan memory type supports {flags}.");
    }
    private VkBufferResource CreateBuffer(ulong size, VkBufferUsageFlags usage, bool host)
    {
        VkBufferResource resource = new()
        {
            Api = api,
            Size = Math.Max(size, 4)
        };
        VkBufferCreateInfo info = new()
        {
            size = resource.Size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };
        Check(api.vkCreateBuffer(&info, null, out resource.Buffer), "vkCreateBuffer");
        api.vkGetBufferMemoryRequirements(resource.Buffer, out VkMemoryRequirements requirements);
        bool addressable = (usage & VkBufferUsageFlags.ShaderDeviceAddress) != 0;
        VkMemoryAllocateFlagsInfo flags = new()
        {
            flags = VkMemoryAllocateFlags.DeviceAddress
        };
        VkMemoryAllocateInfo allocation = new()
        {
            pNext = addressable ? &flags : null,
            allocationSize = requirements.size,
            memoryTypeIndex = MemoryType(requirements.memoryTypeBits, host ? VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent : VkMemoryPropertyFlags.DeviceLocal)
        };
        Check(api.vkAllocateMemory(&allocation, null, out resource.Memory), "vkAllocateMemory(buffer)");
        Check(api.vkBindBufferMemory(resource.Buffer, resource.Memory, 0), "vkBindBufferMemory");
        if (addressable)
        {
            VkBufferDeviceAddressInfo address = new()
            {
                buffer = resource.Buffer
            };
            resource.Address = api.vkGetBufferDeviceAddress(&address);
            if (resource.Address == 0)
            {
                throw new InvalidOperationException("Vulkan returned a null buffer device address.");
            }
        }
        if (host)
        {
            void* mapped;
            Check(api.vkMapMemory(resource.Memory, 0, resource.Size, 0, &mapped), "vkMapMemory");
            resource.Mapped = mapped;
        }
        return resource;
    }
    private VkBufferResource StaticBuffer<T>(ReadOnlySpan<T> data, bool rayGeometry = false) where T : unmanaged
    {
        ulong size = (ulong)(data.Length * sizeof(T));
        VkBufferUsageFlags usage = VkBufferUsageFlags.StorageBuffer | VkBufferUsageFlags.TransferDst;
        if (rayGeometry && RayQuerySupported)
        {
            usage |= VkBufferUsageFlags.ShaderDeviceAddress | VkBufferUsageFlags.AccelerationStructureBuildInputReadOnlyKHR;
        }

        VkBufferResource buffer = CreateBuffer(size, usage, false);
        VkBufferResource upload = CreateBuffer(size, VkBufferUsageFlags.TransferSrc, true);
        upload.Write(data);
        uploads.Add(upload);
        VkBufferCopy copy = new()
        {
            size = size
        };
        api.vkCmdCopyBuffer(commandBuffer, upload.Buffer, buffer.Buffer, 1, &copy);
        return buffer;
    }
    protected override GpuImage CreateImage(int width, int height, ImageFormat format)
    {
        VkImageUsageFlags usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferSrc | VkImageUsageFlags.TransferDst;
        usage |= format == ImageFormat.Depth ? VkImageUsageFlags.DepthStencilAttachment : VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.Storage;
        VkTexture texture = new()
        {
            Api = api,
            Width = width,
            Height = height,
            Format = format,
            Usage = usage
        };
        VkImageCreateInfo info = new()
        {
            imageType = VkImageType.Image2D,
            format = NativeFormat(format),
            extent = new((uint)width, (uint)height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = usage
        };
        Check(api.vkCreateImage(&info, null, out texture.Texture), "vkCreateImage");
        api.vkGetImageMemoryRequirements(texture.Texture, out VkMemoryRequirements requirements);
        VkMemoryAllocateInfo allocation = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = MemoryType(requirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };
        Check(api.vkAllocateMemory(&allocation, null, out texture.Memory), "vkAllocateMemory(image)");
        Check(api.vkBindImageMemory(texture.Texture, texture.Memory, 0), "vkBindImageMemory");
        VkImageViewCreateInfo view = new()
        {
            image = texture.Texture,
            viewType = VkImageViewType.Image2D,
            format = NativeFormat(format),
            subresourceRange = Range(format)
        };
        Check(api.vkCreateImageView(&view, null, out texture.View), "vkCreateImageView");
        return texture;
    }
    protected override void CreateSwapChain()
    {
        Check(instanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physical, surface, out VkSurfaceCapabilitiesKHR capabilities), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        Check(instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(physical, surface, out uint count), "vkGetPhysicalDeviceSurfaceFormatsKHR(count)");
        VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[count];
        Check(instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(physical, surface, formats), "vkGetPhysicalDeviceSurfaceFormatsKHR");
        VkSurfaceFormatKHR selected = formats.FirstOrDefault(x => x.format == VkFormat.R8G8B8A8Unorm && x.colorSpace == VkColorSpaceKHR.SrgbNonLinear);
        if (selected.format == VkFormat.Undefined)
        {
            selected = formats.FirstOrDefault(x => x.format == VkFormat.B8G8R8A8Unorm && x.colorSpace == VkColorSpaceKHR.SrgbNonLinear);
        }

        if (selected.format == VkFormat.Undefined)
        {
            throw new NotSupportedException("The surface must support an SDR UNORM format.");
        }

        if ((capabilities.supportedUsageFlags & VkImageUsageFlags.TransferDst) == 0)
        {
            throw new NotSupportedException("The swap chain does not support transfer destinations.");
        }

        Check(instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(physical, surface, out count), "vkGetPhysicalDeviceSurfacePresentModesKHR(count)");
        VkPresentModeKHR[] modes = new VkPresentModeKHR[count];
        Check(instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(physical, surface, modes), "vkGetPhysicalDeviceSurfacePresentModesKHR");
        uint imageCount = Math.Max(capabilities.minImageCount, RenderLayout.FramesInFlight);
        if (capabilities.maxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.maxImageCount);
        }

        VkSwapchainCreateInfoKHR create = new()
        {
            surface = surface,
            minImageCount = imageCount,
            imageFormat = selected.format,
            imageColorSpace = selected.colorSpace,
            imageExtent = new((uint)Window.Width, (uint)Window.Height),
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst,
            imageSharingMode = VkSharingMode.Exclusive,
            preTransform = capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = modes.Contains(VkPresentModeKHR.Immediate) ? VkPresentModeKHR.Immediate : modes.Contains(VkPresentModeKHR.Mailbox) ? VkPresentModeKHR.Mailbox : VkPresentModeKHR.Fifo,
            clipped = true
        };
        Check(api.vkCreateSwapchainKHR(&create, null, out swapChain), "vkCreateSwapchainKHR");
        Check(api.vkGetSwapchainImagesKHR(swapChain, out count), "vkGetSwapchainImagesKHR(count)");
        backBuffers = new VkImage[count];
        backLayouts = new VkImageLayout[count];
        presentSemaphores = new VkSemaphore[count];
        Check(api.vkGetSwapchainImagesKHR(swapChain, backBuffers), "vkGetSwapchainImagesKHR");
        VkSemaphoreCreateInfo semaphore = new();
        for (int i = 0; i < presentSemaphores.Length; i++)
        {
            Check(api.vkCreateSemaphore(&semaphore, null, out presentSemaphores[i]), "vkCreateSemaphore(present)");
        }
    }
    protected override void DestroySwapChain()
    {
        if (api is null)
        {
            return;
        }

        foreach (VkSemaphore semaphore in presentSemaphores)
        {
            api.vkDestroySemaphore(semaphore);
        }

        presentSemaphores = [];
        backBuffers = [];
        backLayouts = [];
        if (!swapChain.IsNull)
        {
            api.vkDestroySwapchainKHR(swapChain);
            swapChain = default;
        }
    }
    private static VkFormat NativeFormat(ImageFormat format) => format switch
    {
        ImageFormat.Rgba16 => VkFormat.R16G16B16A16Sfloat,
        ImageFormat.Rgba32 => VkFormat.R32G32B32A32Sfloat,
        ImageFormat.Rg16 => VkFormat.R16G16Sfloat,
        ImageFormat.Float => VkFormat.R32Sfloat,
        ImageFormat.Depth => VkFormat.D32Sfloat,
        _ => VkFormat.R8G8B8A8Unorm
    };
    private static VkImageSubresourceRange Range(ImageFormat format) => new(format == ImageFormat.Depth ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color, 0, 1, 0, 1);
}
