using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI(Window window, UserInterface ui) : RHI(window, ui)
{
    protected override nint Command => commandBuffer.Handle;

    private Instance instance;
    private Device device;
    private Vk api = null!;
    private KhrSurface surfaceApi = null!;
    private KhrSwapchain swapChainApi = null!;
    private KhrAccelerationStructure accelerationApi = null!;
    private PhysicalDevice physical;
    private PhysicalDeviceMemoryProperties memoryProperties;
    private Queue queue;
    private Queue presentQueue;
    private bool separatePresentQueue;
    private uint queueFamily;
    private SurfaceKHR surface;
    private SwapchainKHR swapChain;
    private Image[] backBuffers = [];
    private ImageLayout[] backLayouts = [];
    private Semaphore[] presentSemaphores = [];
    private CommandBuffer commandBuffer;
    private readonly VkFrame[] slots = new VkFrame[RenderLayout.FramesInFlight];
    private readonly VkBufferResource[] sceneBuffers = new VkBufferResource[4];
    private readonly List<VkBufferResource> uploads = [];
    private VkTexture font = null!;
    private DescriptorSetLayout descriptorLayout;
    private DescriptorPool descriptorPool;
    private PipelineLayout pipelineLayout;
    private Pipeline scenePipeline;
    private Pipeline depthPipeline;
    private Pipeline uiPipeline;
    private Pipeline shadowPipeline;
    private readonly Dictionary<ComputePass, Pipeline> pipelines = [];
    private Sampler sampler;
    private int uniformStride = RenderLayout.UniformStride;
    private int constantIndex;
    private bool recording;

    private static void Check(Result result, string operation)
    {
        if (result != Result.Success)
        {
            throw new InvalidOperationException($"{operation}: {result}");
        }
    }

    protected override void InitializeDevice()
    {
        api = Vk.GetApi();
        IVkSurface windowSurface = Window.SurfaceWindow.VkSurface
            ?? throw new NotSupportedException("The window does not support Vulkan surfaces.");
        byte** requiredExtensions = windowSurface.GetRequiredExtensions(out uint requiredExtensionCount);
        List<string> instanceExtensions = [];

        for (int i = 0; i < requiredExtensionCount; i++)
        {
            instanceExtensions.Add(NGXMarshal.PtrToString(requiredExtensions[i], NGXEncoding.Utf8)!);
        }

        instanceExtensions.AddRange(NGX.VulkanExtensions());
        ApplicationInfo application = new()
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version13
        };

        using NativeNames extensions = new([.. instanceExtensions.Distinct()]);
        InstanceCreateInfo create = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &application,
            EnabledExtensionCount = extensions.Length,
            PpEnabledExtensionNames = extensions.Pointer
        };

        Check(api.CreateInstance(&create, null, out Instance createdInstance), "vkCreateInstance");
        instance = createdInstance;

        if (!api.TryGetInstanceExtension(instance, out surfaceApi))
        {
            throw new NotSupportedException("VK_KHR_surface is unavailable.");
        }

        surface = windowSurface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        uint count = 0;
        Check(api.EnumeratePhysicalDevices(instance, &count, null), "vkEnumeratePhysicalDevices(count)");
        PhysicalDevice[] devices = new PhysicalDevice[count];

        fixed (PhysicalDevice* pointer = devices)
        {
            Check(api.EnumeratePhysicalDevices(instance, &count, pointer), "vkEnumeratePhysicalDevices");
        }

        int bestScore = -1;
        HashSet<string> supportedNames = [];

        foreach (PhysicalDevice candidate in devices)
        {
            api.GetPhysicalDeviceProperties(candidate, out PhysicalDeviceProperties properties);

            if (properties.ApiVersion < Vk.Version13)
            {
                continue;
            }

            PhysicalDeviceRayQueryFeaturesKHR queryFeatures = new() { SType = StructureType.PhysicalDeviceRayQueryFeaturesKhr };
            PhysicalDeviceAccelerationStructureFeaturesKHR accelerationFeatures = new()
            {
                SType = StructureType.PhysicalDeviceAccelerationStructureFeaturesKhr,
                PNext = &queryFeatures
            };

            PhysicalDeviceVulkan13Features features13 = new()
            {
                SType = StructureType.PhysicalDeviceVulkan13Features,
                PNext = &accelerationFeatures
            };

            PhysicalDeviceVulkan12Features features12 = new()
            {
                SType = StructureType.PhysicalDeviceVulkan12Features,
                PNext = &features13
            };

            PhysicalDeviceVulkan11Features features11 = new()
            {
                SType = StructureType.PhysicalDeviceVulkan11Features,
                PNext = &features12
            };

            PhysicalDeviceFeatures2 features = new()
            {
                SType = StructureType.PhysicalDeviceFeatures2,
                PNext = &features11
            };

            api.GetPhysicalDeviceFeatures2(candidate, &features);

            if (!features11.ShaderDrawParameters || !features13.DynamicRendering || !features13.Synchronization2 ||
                !features.Features.ShaderStorageImageReadWithoutFormat || !features.Features.ShaderStorageImageWriteWithoutFormat ||
                !features.Features.ShaderStorageImageExtendedFormats)
            {
                continue;
            }

            uint extensionCount = 0;
            Check(api.EnumerateDeviceExtensionProperties(candidate, (byte*)null, &extensionCount, null), "vkEnumerateDeviceExtensionProperties(count)");
            ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionCount];

            fixed (ExtensionProperties* pointer = availableExtensions)
            {
                Check(api.EnumerateDeviceExtensionProperties(candidate, (byte*)null, &extensionCount, pointer), "vkEnumerateDeviceExtensionProperties");
            }

            HashSet<string> extensionNames = [];

            foreach (ExtensionProperties extension in availableExtensions)
            {
                extensionNames.Add(NGXMarshal.PtrToString(extension.ExtensionName, NGXEncoding.Utf8)!);
            }

            bool rayQuery = queryFeatures.RayQuery && accelerationFeatures.AccelerationStructure
                && features12.BufferDeviceAddress && RayExtensions.All(extensionNames.Contains);
            api.GetPhysicalDeviceFormatProperties(candidate, Format.R32G32B32Sfloat, out FormatProperties vertexFormat);
            rayQuery &= (vertexFormat.BufferFeatures & FormatFeatureFlags.AccelerationStructureVertexBufferBitKhr) != 0;
            uint familyCount = 0;
            api.GetPhysicalDeviceQueueFamilyProperties(candidate, &familyCount, null);
            QueueFamilyProperties[] families = new QueueFamilyProperties[familyCount];

            fixed (QueueFamilyProperties* pointer = families)
            {
                api.GetPhysicalDeviceQueueFamilyProperties(candidate, &familyCount, pointer);
            }

            for (uint i = 0; i < familyCount; i++)
            {
                Check(surfaceApi.GetPhysicalDeviceSurfaceSupport(candidate, i, surface, out Bool32 present), "vkGetPhysicalDeviceSurfaceSupportKHR");

                if (!present || (families[i].QueueFlags & (QueueFlags.GraphicsBit | QueueFlags.ComputeBit)) != (QueueFlags.GraphicsBit | QueueFlags.ComputeBit))
                {
                    continue;
                }

                int score = properties.VendorID == 0x10DE ? 2 : properties.DeviceType == PhysicalDeviceType.DiscreteGpu ? 1 : 0;

                if (score <= bestScore)
                {
                    continue;
                }

                bestScore = score;
                supportedNames = extensionNames;
                physical = candidate;
                queueFamily = i;
                separatePresentQueue = families[i].QueueCount > 1;
                RayQuerySupported = rayQuery;
                RayQueryStatus = rayQuery ? "VK_KHR_ray_query" : "Requires Vulkan rayQuery, accelerationStructure and bufferDeviceAddress";
                AdapterName = NGXMarshal.PtrToString(properties.DeviceName, NGXEncoding.Utf8) ?? "Vulkan GPU";
                ulong alignment = properties.Limits.MinUniformBufferOffsetAlignment;
                uniformStride = (int)(((ulong)RenderLayout.UniformStride + alignment - 1) / alignment * alignment);
            }
        }

        if (physical.Handle == 0)
        {
            throw new NotSupportedException("A Vulkan 1.3 graphics/compute/present queue and storage-image support are required.");
        }

        api.GetPhysicalDeviceMemoryProperties(physical, out memoryProperties);
        float* priorities = stackalloc float[] { 1, 1 };
        DeviceQueueCreateInfo queueInfo = new()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = queueFamily,
            QueueCount = separatePresentQueue ? 2u : 1u,
            PQueuePriorities = priorities
        };

        PhysicalDeviceRayQueryFeaturesKHR enabledRayQuery = new()
        {
            SType = StructureType.PhysicalDeviceRayQueryFeaturesKhr,
            RayQuery = true
        };

        PhysicalDeviceAccelerationStructureFeaturesKHR enabledAcceleration = new()
        {
            SType = StructureType.PhysicalDeviceAccelerationStructureFeaturesKhr,
            AccelerationStructure = true,
            PNext = &enabledRayQuery
        };

        PhysicalDeviceVulkan13Features enabled13 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            DynamicRendering = true,
            Synchronization2 = true,
            PNext = RayQuerySupported ? &enabledAcceleration : null
        };

        PhysicalDeviceVulkan12Features enabled12 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan12Features,
            PNext = &enabled13,
            BufferDeviceAddress = RayQuerySupported
        };

        PhysicalDeviceVulkan11Features enabled11 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan11Features,
            PNext = &enabled12,
            ShaderDrawParameters = true
        };

        PhysicalDeviceFeatures2 enabled = new()
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &enabled11,
            Features = new()
            {
                ShaderStorageImageReadWithoutFormat = true,
                ShaderStorageImageWriteWithoutFormat = true,
                ShaderStorageImageExtendedFormats = true
            }
        };

        List<string> requestedExtensions = ["VK_KHR_swapchain"];

        if (RayQuerySupported)
        {
            requestedExtensions.AddRange(RayExtensions);
            PhysicalDeviceAccelerationStructurePropertiesKHR accelerationProperties = new() { SType = StructureType.PhysicalDeviceAccelerationStructurePropertiesKhr };
            PhysicalDeviceProperties2 properties = new()
            {
                SType = StructureType.PhysicalDeviceProperties2,
                PNext = &accelerationProperties
            };

            api.GetPhysicalDeviceProperties2(physical, &properties);
            scratchAlignment = accelerationProperties.MinAccelerationStructureScratchOffsetAlignment;
            maxRayInstances = accelerationProperties.MaxInstanceCount;
            maxRayPrimitives = accelerationProperties.MaxPrimitiveCount;
        }

        foreach (string extension in NGX.VulkanExtensions(instance.Handle, physical.Handle))
        {
            if (!supportedNames.Contains(extension))
            {
                throw new NotSupportedException($"NGX requires Vulkan device extension {extension}.");
            }

            if (!requestedExtensions.Contains(extension))
            {
                requestedExtensions.Add(extension);
            }
        }

        using NativeNames deviceExtensions = new([.. requestedExtensions.Distinct()]);
        DeviceCreateInfo deviceInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = &enabled,
            QueueCreateInfoCount = 1,
            PQueueCreateInfos = &queueInfo,
            EnabledExtensionCount = deviceExtensions.Length,
            PpEnabledExtensionNames = deviceExtensions.Pointer
        };

        Check(api.CreateDevice(physical, &deviceInfo, null, out Device createdDevice), "vkCreateDevice");
        device = createdDevice;
        if (!api.TryGetDeviceExtension(instance, device, out swapChainApi))
        {
            throw new NotSupportedException("VK_KHR_swapchain is unavailable.");
        }

        if (RayQuerySupported && !api.TryGetDeviceExtension(instance, device, out accelerationApi))
        {
            throw new NotSupportedException("VK_KHR_acceleration_structure is unavailable.");
        }

        api.GetDeviceQueue(device, queueFamily, 0, out queue);
        api.GetDeviceQueue(device, queueFamily, separatePresentQueue ? 1u : 0u, out presentQueue);
        NGX.Initialize(device.Handle, instance.Handle, physical.Handle,
            api.Context.GetProcAddress("vkGetInstanceProcAddr"), api.Context.GetProcAddress("vkGetDeviceProcAddr"));
        InitializePresentation();
    }

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
                MemoryTypeIndex = MemoryType(
                    requirements.MemoryTypeBits,
                    host ? MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit : MemoryPropertyFlags.DeviceLocalBit)
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

                if (resource.Address == 0)
                {
                    throw new InvalidOperationException("Vulkan returned a null buffer device address.");
                }
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

    private VkBufferResource StaticBuffer<T>(ReadOnlySpan<T> data, bool rayGeometry = false)
        where T : unmanaged
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

    protected override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
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

    protected override void CreateSwapChain()
    {
        Check(
            surfaceApi.GetPhysicalDeviceSurfaceCapabilities(physical, surface, out SurfaceCapabilitiesKHR capabilities),
            "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        uint count = 0;
        Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, null), "vkGetPhysicalDeviceSurfaceFormatsKHR(count)");
        SurfaceFormatKHR[] formats = new SurfaceFormatKHR[count];

        fixed (SurfaceFormatKHR* pointer = formats)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfaceFormatsKHR");
        }
        SurfaceFormatKHR selected = formats.FirstOrDefault(x => x.Format == Format.R8G8B8A8Unorm && x.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);

        if (selected.Format == Format.Undefined)
        {
            selected = formats.FirstOrDefault(x => x.Format == Format.B8G8R8A8Unorm && x.ColorSpace == ColorSpaceKHR.SpaceSrgbNonlinearKhr);
        }

        if (selected.Format == Format.Undefined)
        {
            throw new NotSupportedException("The surface must support an SDR UNORM format.");
        }

        if ((capabilities.SupportedUsageFlags & ImageUsageFlags.TransferDstBit) == 0)
        {
            throw new NotSupportedException("The swap chain does not support transfer destinations.");
        }

        Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, null), "vkGetPhysicalDeviceSurfacePresentModesKHR(count)");
        PresentModeKHR[] modes = new PresentModeKHR[count];

        fixed (PresentModeKHR* pointer = modes)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfacePresentModesKHR");
        }
        uint imageCount = Math.Max(capabilities.MinImageCount, RenderLayout.FramesInFlight);

        if (capabilities.MaxImageCount > 0)
        {
            imageCount = Math.Min(imageCount, capabilities.MaxImageCount);
        }

        SwapchainCreateInfoKHR create = new()
        {
            SType = StructureType.SwapchainCreateInfoKhr,
            Surface = surface,
            MinImageCount = imageCount,
            ImageFormat = selected.Format,
            ImageColorSpace = selected.ColorSpace,
            ImageExtent = new((uint)Window.Width, (uint)Window.Height),
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.TransferDstBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = CompositeAlphaFlagsKHR.OpaqueBitKhr,
            PresentMode = modes.Contains(PresentModeKHR.ImmediateKhr) ? PresentModeKHR.ImmediateKhr
                : modes.Contains(PresentModeKHR.MailboxKhr) ? PresentModeKHR.MailboxKhr
                : PresentModeKHR.FifoKhr,
            Clipped = true
        };

        Check(swapChainApi.CreateSwapchain(device, &create, null, out SwapchainKHR createdSwapChain), "vkCreateSwapchainKHR");
        swapChain = createdSwapChain;
        Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, null), "vkGetSwapchainImagesKHR(count)");
        backBuffers = new Image[count];
        backLayouts = new ImageLayout[count];
        presentSemaphores = new Semaphore[count];
        fixed (Image* pointer = backBuffers)
        {
            Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, pointer), "vkGetSwapchainImagesKHR");
        }
        SemaphoreCreateInfo semaphore = new() { SType = StructureType.SemaphoreCreateInfo };

        for (int i = 0; i < presentSemaphores.Length; i++)
        {
            Check(api.CreateSemaphore(device, &semaphore, null, out Semaphore createdSemaphore), "vkCreateSemaphore(present)");
            presentSemaphores[i] = createdSemaphore;
        }
    }

    protected override void DestroySwapChain()
    {
        if (device.Handle == 0)
        {
            return;
        }

        foreach (Semaphore semaphore in presentSemaphores)
        {
            api.DestroySemaphore(device, semaphore, null);
        }

        presentSemaphores = [];
        backBuffers = [];
        backLayouts = [];

        if (swapChain.Handle != 0)
        {
            swapChainApi.DestroySwapchain(device, swapChain, null);
            swapChain = default;
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

    private static ImageSubresourceRange Range(ImageFormat format, int layers = 1) => new(
        format == ImageFormat.Depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit,
        0,
        1,
        0,
        (uint)layers);
}
