using NGX.NET;
using Showcase.Helpers;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
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

    protected override void InitializeDevice()
    {
        api = Vk.GetApi();
        IVkSurface windowSurface = Window.VkSurface
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

    protected override void DisposeDevice()
    {
        if (device.Handle != 0)
        {
            if (recording)
            {
                api.EndCommandBuffer(commandBuffer);
            }

            DisposePresentation();
            DestroySwapChain();

            foreach (Pipeline pipeline in pipelines.Values)
            {
                api.DestroyPipeline(device, pipeline, null);
            }

            api.DestroyPipeline(device, shadowPipeline, null);
            api.DestroyPipeline(device, scenePipeline, null);
            api.DestroyPipeline(device, depthPipeline, null);
            api.DestroyPipeline(device, uiPipeline, null);
            api.DestroyPipelineLayout(device, pipelineLayout, null);
            api.DestroyDescriptorPool(device, descriptorPool, null);
            api.DestroyDescriptorSetLayout(device, descriptorLayout, null);
            api.DestroySampler(device, sampler, null);

            foreach (VkFrame? frame in slots)
            {
                frame?.Dispose();
            }

            foreach (VkAcceleration bottom in bottomLevels)
            {
                bottom.Dispose();
            }

            foreach (VkBufferResource? buffer in sceneBuffers)
            {
                buffer?.Dispose();
            }

            foreach (VkBufferResource upload in uploads)
            {
                upload.Dispose();
            }

            font?.Dispose();
            api.DestroyDevice(device, null);
            device = default;
        }

        if (instance.Handle != 0)
        {
            if (surface.Handle != 0)
            {
                surfaceApi.DestroySurface(instance, surface, null);
            }

            api.DestroyInstance(instance, null);
            instance = default;
        }

        accelerationApi?.Dispose();
        swapChainApi?.Dispose();
        surfaceApi?.Dispose();
        api?.Dispose();
    }
}
