using System.Numerics;
using System.Runtime.InteropServices;
using Hexa.NET.ImGui;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core;
using Silk.NET.Core.Contexts;
using Silk.NET.Maths;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using Silk.NET.Windowing;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

namespace Showcase.Vulkan;

internal unsafe class VulkanRHI(IWindow window, ImGuiHandler ui) : RHI(window, ui)
{
    private static readonly string[] RayExtensions = ["VK_KHR_acceleration_structure", "VK_KHR_ray_query", "VK_KHR_deferred_host_operations"];

    private readonly VkFrame[] slots = new VkFrame[RenderLayout.FramesInFlight];
    private readonly VkBufferResource[] sceneBuffers = new VkBufferResource[4];
    private readonly List<VkBufferResource> uploads = [];
    private readonly Dictionary<ComputePass, Pipeline> pipelines = [];
    private readonly Lock queueSync = new();
    private readonly List<VkAcceleration> bottomLevels = [];

    private CommandBuffer commandBuffer;
    private int uniformStride = RenderLayout.UniformStride;
    private int constantIndex;
    private bool recording;
    private DescriptorSetLayout descriptorLayout;
    private DescriptorPool descriptorPool;
    private Sampler sampler;
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
    private PipelineLayout pipelineLayout;
    private Pipeline scenePipeline;
    private Pipeline depthPipeline;
    private Pipeline uiPipeline;
    private Pipeline shadowPipeline;
    private CommandPool presentPool;
    private CommandBuffer presentCommand;
    private Fence presentFence;
    private Semaphore presentAcquire;
    private bool presentationPending;
    private uint scratchAlignment;
    private ulong maxRayInstances;
    private ulong maxRayPrimitives;
    private SwapchainKHR swapChain;
    private Image[] backBuffers = [];
    private ImageLayout[] backLayouts = [];
    private Semaphore[] presentSemaphores = [];
    private Extent2D swapChainExtent;
    private VkTexture font = null!;

    public override nint Command => commandBuffer.Handle;

    public override void UpdateRayTracingScene()
    {
        UpdateAccelerationStructure(slots[Frame.Slot]);
    }

    public override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(PipelineBindPoint.Compute, pipelines[pass], constants);
        api.CmdDispatch(commandBuffer, (uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    public override void Transition(GpuImage image, ImageUse use)
    {
        VkTexture texture = (VkTexture)image;
        ImageLayout layout = use switch
        {
            ImageUse.Storage => ImageLayout.General,
            ImageUse.ColorAttachment => ImageLayout.ColorAttachmentOptimal,
            ImageUse.DepthAttachment => ImageLayout.DepthStencilAttachmentOptimal,
            ImageUse.CopySource => ImageLayout.TransferSrcOptimal,
            ImageUse.CopyDestination => ImageLayout.TransferDstOptimal,
            _ => ImageLayout.ShaderReadOnlyOptimal
        };
        if (texture.Layout != layout)
        {
            Barrier(texture.Texture, texture.Layout, layout, Range(texture.Format, texture.Layers));
        }

        texture.Layout = layout;
    }

    public override void SubmitFrame()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer");
        recording = false;
        CommandBuffer command = commandBuffer;
        Semaphore complete = slots[Frame.Slot].RenderComplete;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &complete
        };

        using Lock.Scope _ = queueSync.EnterScope();

        Check(api.QueueSubmit(queue, 1, &submit, slots[Frame.Slot].Fence), "vkQueueSubmit(frame)");
    }

    public override void WaitIdle()
    {
        if (device.Handle is not 0)
        {
            Check(api.DeviceWaitIdle(device), "vkDeviceWaitIdle");
        }
    }

    public override void UpdateDescriptors()
    {
        for (int index = 0; index < slots.Length; index++)
        {
            VkFrame frame = slots[index];
            void Buffer(uint binding, DescriptorType type, VkBufferResource buffer, ulong range)
            {
                DescriptorBufferInfo info = new()
                {
                    Buffer = buffer.Buffer,
                    Range = range
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = frame.Descriptors,
                    DstBinding = binding,
                    DescriptorCount = 1,
                    DescriptorType = type,
                    PBufferInfo = &info
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            void Texture(uint binding, DescriptorType type, VkTexture texture, ImageLayout layout)
            {
                DescriptorImageInfo info = new()
                {
                    ImageView = texture.View,
                    ImageLayout = layout
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = frame.Descriptors,
                    DstBinding = binding,
                    DescriptorCount = 1,
                    DescriptorType = type,
                    PImageInfo = &info
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            Buffer(0, DescriptorType.UniformBufferDynamic, frame.Constants, (ulong)sizeof(FrameConstants));

            for (uint i = 0; i < 5; i++)
            {
                VkBufferResource buffer = i is 4 ? frame.Objects : sceneBuffers[i];
                Buffer(i + 1, DescriptorType.StorageBuffer, buffer, buffer.Size);
            }

            if (RayQuerySupported)
            {
                AccelerationStructureKHR top = frame.Tlas!.Handle;
                WriteDescriptorSetAccelerationStructureKHR acceleration = new()
                {
                    SType = StructureType.WriteDescriptorSetAccelerationStructureKhr,
                    AccelerationStructureCount = 1,
                    PAccelerationStructures = &top
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    PNext = &acceleration,
                    DstSet = frame.Descriptors,
                    DstBinding = 6,
                    DescriptorCount = 1,
                    DescriptorType = DescriptorType.AccelerationStructureKhr
                };

                api.UpdateDescriptorSets(device, 1, &write, 0, null);
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                Texture(7 + (uint)slot, DescriptorType.SampledImage, (VkTexture)Resources.Frames[index][(int)slot], ImageLayout.ShaderReadOnlyOptimal);
            }

            int previousFrame = (index + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            Texture(RenderLayout.PreviousExposureSrv + 1, DescriptorType.SampledImage, (VkTexture)Resources.Frames[previousFrame][(int)ImageSlot.Exposure], ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.FontSrv + 1, DescriptorType.SampledImage, font, ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.LightingSamplesSrv + 1, DescriptorType.SampledImage, (VkTexture)Resources.LightingSamples, ImageLayout.ShaderReadOnlyOptimal);

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                Texture(32 + (uint)i, DescriptorType.StorageImage, (VkTexture)Resources.Frames[index][(int)RenderLayout.StorageImages[i]], ImageLayout.General);
            }

            Texture(32 + RenderLayout.LightingSamplesUav, DescriptorType.StorageImage, (VkTexture)Resources.LightingSamples, ImageLayout.General);
            DescriptorImageInfo samplerInfo = new()
            {
                Sampler = sampler
            };

            WriteDescriptorSet samplerWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.Descriptors,
                DstBinding = 48,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.Sampler,
                PImageInfo = &samplerInfo
            };

            api.UpdateDescriptorSets(device, 1, &samplerWrite, 0, null);
        }
    }

    public override void DrawShadow()
    {
        VkTexture shadow = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depth = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = shadow.View,
            ImageLayout = shadow.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new()
            {
                DepthStencil = new(1, 0)
            }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)shadow.Width, (uint)shadow.Height)),
            LayerCount = 1,
            PDepthAttachment = &depth
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, shadowPipeline, Frame.Constants);
        Viewport(shadow.Width, shadow.Height);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }

    public override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        RenderingAttachmentInfo* colors = stackalloc RenderingAttachmentInfo[colorTargets.Length];
        for (int i = 0; i < colorTargets.Length; i++)
        {
            VkTexture image = (VkTexture)Resources.Image(Frame.Slot, colorTargets[i]);
            Transition(image, ImageUse.ColorAttachment);
            colors[i] = new()
            {
                SType = StructureType.RenderingAttachmentInfo,
                ImageView = image.View,
                ImageLayout = image.Layout,
                LoadOp = AttachmentLoadOp.Clear,
                StoreOp = AttachmentStoreOp.Store
            };
        }

        VkTexture depth = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depthAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = depth.View,
            ImageLayout = depth.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new()
            {
                DepthStencil = new(0, 0)
            }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)Resources.InputWidth, (uint)Resources.InputHeight)),
            LayerCount = 1,
            PDepthAttachment = &depthAttachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, depthPipeline, Frame.Constants);
        Viewport(Resources.InputWidth, Resources.InputHeight);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);

        // Make prepass depth writes visible to the next rendering scope's tests.
        Barrier(depth.Texture, depth.Layout, depth.Layout, Range(depth.Format));
        depthAttachment.LoadOp = AttachmentLoadOp.Load;
        rendering.ColorAttachmentCount = (uint)colorTargets.Length;
        rendering.PColorAttachments = colors;
        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants colorConstants = Frame.Constants;
        colorConstants.Parameters.W = Resources.Scene.Vertices.Length / 3;
        Bind(PipelineBindPoint.Graphics, scenePipeline, colorConstants);
        api.CmdDraw(commandBuffer, (uint)Resources.Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }

    public override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ImageUsageFlags usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit;
        usage |= format is ImageFormat.Depth ? ImageUsageFlags.DepthStencilAttachmentBit : ImageUsageFlags.ColorAttachmentBit | ImageUsageFlags.StorageBit;
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

    public override void WaitRenderedFrame(int slot)
    {
        Fence fence = slots[slot].Fence;
        Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(render)");
        Semaphore complete = slots[slot].RenderComplete;
        PipelineStageFlags stage = PipelineStageFlags.AllCommandsBit;
        SubmitInfo wait = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &complete,
            PWaitDstStageMask = &stage
        };
        Fence dependencyFence = presentFence;
        Check(api.ResetFences(device, 1, &dependencyFence), "vkResetFences(render dependency)");

        using Lock.Scope _ = queueSync.EnterScope();

        // Track semaphore consumption even when acquire cannot produce an
        // image. The slot must not signal RenderComplete again first.
        Check(api.QueueSubmit(presentQueue, 1, &wait, dependencyFence), "vkQueueSubmit(render dependency)");
        presentationPending = true;
    }

    public override bool PresentImage(GpuImage image)
    {
        WaitPresentation();

        if (swapChain.Handle is 0)
        {
            return false;
        }

        uint index = 0;
        Result acquire = swapChainApi.AcquireNextImage(device, swapChain, ulong.MaxValue, presentAcquire, default, &index);
        if (acquire is Result.ErrorOutOfDateKhr)
        {
            return false;
        }

        if (acquire is not Result.SuboptimalKhr)
        {
            Check(acquire, "vkAcquireNextImageKHR");
        }

        Check(api.ResetCommandPool(device, presentPool, 0), "vkResetCommandPool(present)");
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(presentCommand, &begin), "vkBeginCommandBuffer(present)");
        Barrier(backBuffers[index], backLayouts[index], ImageLayout.TransferDstOptimal, Range(ImageFormat.Rgba8), presentCommand);
        ImageBlit blit = new()
        {
            SrcSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            DstSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1)
        };

        blit.SrcOffsets[1] = new(image.Width, image.Height, 1);
        blit.DstOffsets[1] = new((int)swapChainExtent.Width, (int)swapChainExtent.Height, 1);
        api.CmdBlitImage(presentCommand, ((VkTexture)image).Texture, ImageLayout.TransferSrcOptimal, backBuffers[index], ImageLayout.TransferDstOptimal, 1, &blit, Filter.Nearest);
        Barrier(backBuffers[index], ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr, Range(ImageFormat.Rgba8), presentCommand);
        backLayouts[index] = ImageLayout.PresentSrcKhr;
        Check(api.EndCommandBuffer(presentCommand), "vkEndCommandBuffer(present)");
        CommandBuffer command = presentCommand;
        Semaphore acquireSemaphore = presentAcquire;
        Semaphore complete = presentSemaphores[index];
        Fence fence = presentFence;
        Check(api.ResetFences(device, 1, &fence), "vkResetFences(present)");
        PipelineStageFlags stage = PipelineStageFlags.TransferBit;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &acquireSemaphore,
            PWaitDstStageMask = &stage,
            CommandBufferCount = 1,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &complete
        };

        SwapchainKHR swap = swapChain;
        PresentInfoKHR present = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1,
            PWaitSemaphores = &complete,
            SwapchainCount = 1,
            PSwapchains = &swap,
            PImageIndices = &index
        };

        Result result;
        {
            using Lock.Scope _ = queueSync.EnterScope();

            Check(api.QueueSubmit(presentQueue, 1, &submit, fence), "vkQueueSubmit(present)");
            presentationPending = true;
            result = swapChainApi.QueuePresent(presentQueue, &present);
        }

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
        {
            return false;
        }

        Check(result, "vkQueuePresentKHR");

        return acquire is not Result.SuboptimalKhr;
    }

    public override void WaitPresentation()
    {
        if (presentationPending)
        {
            Fence fence = presentFence;
            Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(present copy)");
            presentationPending = false;
        }
    }

    public override void CreateSwapChain()
    {
        Check(surfaceApi.GetPhysicalDeviceSurfaceCapabilities(physical, surface, out SurfaceCapabilitiesKHR capabilities), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        Extent2D extent = capabilities.CurrentExtent;
        if (extent.Width is uint.MaxValue)
        {
            Vector2D<int> framebufferSize = Window.FramebufferSize;
            extent = new(Math.Clamp((uint)Math.Max(0, framebufferSize.X), capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width), Math.Clamp((uint)Math.Max(0, framebufferSize.Y), capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height));
        }

        // A surface can become minimized between the renderer's size check and
        // this query. Presentation requests recreation when no chain is ready.
        if (extent.Width is 0 || extent.Height is 0)
        {
            return;
        }

        uint count = 0;
        Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, null), "vkGetPhysicalDeviceSurfaceFormatsKHR(count)");
        SurfaceFormatKHR[] formats = new SurfaceFormatKHR[count];

        fixed (SurfaceFormatKHR* pointer = formats)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfaceFormats(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfaceFormatsKHR");
        }

        SurfaceFormatKHR selected = formats.FirstOrDefault(static x => x.Format is Format.R8G8B8A8Unorm && x.ColorSpace is ColorSpaceKHR.SpaceSrgbNonlinearKhr);
        if (formats.Length is 1 && formats[0].Format is Format.Undefined && formats[0].ColorSpace is ColorSpaceKHR.SpaceSrgbNonlinearKhr)
        {
            selected = new(Format.R8G8B8A8Unorm, formats[0].ColorSpace);
        }

        if (selected.Format is Format.Undefined)
        {
            selected = formats.FirstOrDefault(static x => x.Format is Format.B8G8R8A8Unorm && x.ColorSpace is ColorSpaceKHR.SpaceSrgbNonlinearKhr);
        }

        if (selected.Format is Format.Undefined)
        {
            throw new NotSupportedException("The surface must support an SDR UNORM format.");
        }

        if ((capabilities.SupportedUsageFlags & ImageUsageFlags.TransferDstBit) is 0)
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
            ImageExtent = extent,
            ImageArrayLayers = 1,
            ImageUsage = ImageUsageFlags.TransferDstBit,
            ImageSharingMode = SharingMode.Exclusive,
            PreTransform = capabilities.CurrentTransform,
            CompositeAlpha = SelectCompositeAlpha(capabilities.SupportedCompositeAlpha),
            PresentMode = modes switch
            {
                _ when modes.Contains(PresentModeKHR.ImmediateKhr) => PresentModeKHR.ImmediateKhr,
                _ when modes.Contains(PresentModeKHR.MailboxKhr) => PresentModeKHR.MailboxKhr,
                _ => PresentModeKHR.FifoKhr
            },
            Clipped = true
        };

        Check(swapChainApi.CreateSwapchain(device, &create, null, out SwapchainKHR createdSwapChain), "vkCreateSwapchainKHR");
        swapChain = createdSwapChain;
        swapChainExtent = extent;
        Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, null), "vkGetSwapchainImagesKHR(count)");
        backBuffers = new Image[count];
        backLayouts = new ImageLayout[count];
        presentSemaphores = new Semaphore[count];
        fixed (Image* pointer = backBuffers)
        {
            Check(swapChainApi.GetSwapchainImages(device, swapChain, &count, pointer), "vkGetSwapchainImagesKHR");
        }

        SemaphoreCreateInfo semaphore = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        for (int i = 0; i < presentSemaphores.Length; i++)
        {
            Check(api.CreateSemaphore(device, &semaphore, null, out Semaphore createdSemaphore), "vkCreateSemaphore(present)");
            presentSemaphores[i] = createdSemaphore;
        }
    }

    public override void DestroySwapChain()
    {
        if (device.Handle is 0)
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
        swapChainExtent = default;

        if (swapChain.Handle is not 0)
        {
            swapChainApi.DestroySwapchain(device, swapChain, null);
            swapChain = default;
        }
    }

    public override void UpdateFontTexture()
    {
        VkFrame frame = slots[Frame.Slot];
        Check(api.ResetCommandPool(device, frame.Pool, 0), "vkResetCommandPool(font upload)");
        commandBuffer = frame.Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(font upload)");
        recording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    public override void DrawUI(ImDrawDataPtr data)
    {
        VkFrame frame = slots[Frame.Slot];

        void Ensure(ref VkBufferResource? buffer, ulong size, BufferUsageFlags usage)
        {
            if (buffer is not null && buffer.Size >= size)
            {
                return;
            }

            VkBufferResource replacement = CreateBuffer(Math.Max(4096, size * 2), usage, true);
            buffer?.Dispose();
            buffer = replacement;
        }

        Ensure(ref frame.Vertices, (ulong)(data.TotalVtxCount * sizeof(ImDrawVert)), BufferUsageFlags.VertexBufferBit);
        Ensure(ref frame.Indices, (ulong)(data.TotalIdxCount * sizeof(ushort)), BufferUsageFlags.IndexBufferBit);
        int vertexOffset = 0;
        int indexOffset = 0;
        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            frame.Vertices!.Write(new ReadOnlySpan<ImDrawVert>(list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            frame.Indices!.Write(new ReadOnlySpan<ushort>(list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        VkTexture image = (VkTexture)Resources.Image(Frame.Slot, ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        RenderingAttachmentInfo attachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = image.View,
            ImageLayout = image.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)Resources.OutputWidth, (uint)Resources.OutputHeight)),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &attachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants uiConstants = Frame.Constants;
        uiConstants.Size.Z = data.DisplaySize.X;
        uiConstants.Size.W = data.DisplaySize.Y;
        Bind(PipelineBindPoint.Graphics, uiPipeline, uiConstants);
        Viewport(Resources.OutputWidth, Resources.OutputHeight);
        Buffer vertexBuffer = frame.Vertices!.Buffer;
        ulong offset = 0;
        api.CmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
        api.CmdBindIndexBuffer(commandBuffer, frame.Indices!.Buffer, 0, IndexType.Uint16);
        vertexOffset = 0;
        indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            for (int c = 0; c < list.CmdBuffer.Size; c++)
            {
                ref readonly ImDrawCmd draw = ref list.CmdBuffer.Data[c];
                if (draw.UserCallback != null)
                {
                    throw new NotSupportedException("Unexpected UI draw callback.");
                }

                Vector4 clip = draw.ClipRect * new Vector4(data.FramebufferScale, data.FramebufferScale.X, data.FramebufferScale.Y);
                int left = Math.Max(0, (int)clip.X);
                int top = Math.Max(0, (int)clip.Y);
                int right = Math.Min(Resources.OutputWidth, (int)clip.Z);
                int bottom = Math.Min(Resources.OutputHeight, (int)clip.W);
                if (right <= left || bottom <= top)
                {
                    continue;
                }

                Rect2D scissor = new(new(left, top), new((uint)(right - left), (uint)(bottom - top)));
                api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
                api.CmdDrawIndexed(commandBuffer, draw.ElemCount, 1, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        api.CmdEndRendering(commandBuffer);
    }

    protected override void BeginCommands()
    {
        VkFrame frame = slots[Frame.Slot];
        Fence fence = frame.Fence;
        Check(api.WaitForFences(device, 1, &fence, true, ulong.MaxValue), "vkWaitForFences(frame)");
        Check(api.ResetFences(device, 1, &fence), "vkResetFences");
        Check(api.ResetCommandPool(device, frame.Pool, 0), "vkResetCommandPool");
        commandBuffer = frame.Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer");
        recording = true;
        frame.Objects.Write<SceneObject>(Resources.Scene.Objects);
        constantIndex = 0;
    }

    protected override void InitializeDevice()
    {
        api = Vk.GetApi();
        IVkSurface windowSurface = Window.VkSurface ?? throw new NotSupportedException("The window does not support Vulkan surfaces.");
        byte** requiredExtensions = windowSurface.GetRequiredExtensions(out uint requiredExtensionCount);
        List<string> instanceExtensions = [];
        for (int i = 0; i < requiredExtensionCount; i++)
        {
            instanceExtensions.Add(Marshal.PtrToStringUTF8((nint)requiredExtensions[i])!);
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
            int score = properties switch
            {
                { VendorID: 0x10DE } => 2,
                { DeviceType: PhysicalDeviceType.DiscreteGpu } => 1,
                _ => 0
            };
            if (properties.ApiVersion < Vk.Version13 || score <= bestScore)
            {
                continue;
            }

            PhysicalDeviceRayQueryFeaturesKHR queryFeatures = new()
            {
                SType = StructureType.PhysicalDeviceRayQueryFeaturesKhr
            };
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

            if (!features11.ShaderDrawParameters || !features13.DynamicRendering || !features13.Synchronization2 || !features.Features.ShaderStorageImageReadWithoutFormat || !features.Features.ShaderStorageImageWriteWithoutFormat || !features.Features.ShaderStorageImageExtendedFormats)
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
                extensionNames.Add(Marshal.PtrToStringUTF8((nint)extension.ExtensionName)!);
            }

            bool rayQuery = queryFeatures.RayQuery && accelerationFeatures.AccelerationStructure && features12.BufferDeviceAddress && RayExtensions.All(extensionNames.Contains);
            api.GetPhysicalDeviceFormatProperties(candidate, Format.R32G32B32Sfloat, out FormatProperties vertexFormat);
            rayQuery &= (vertexFormat.BufferFeatures & FormatFeatureFlags.AccelerationStructureVertexBufferBitKhr) is not 0;
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

                if (!present || (families[i].QueueFlags & (QueueFlags.GraphicsBit | QueueFlags.ComputeBit)) is not (QueueFlags.GraphicsBit | QueueFlags.ComputeBit))
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
                AdapterName = Marshal.PtrToStringUTF8((nint)properties.DeviceName) ?? "Vulkan GPU";
                ulong alignment = properties.Limits.MinUniformBufferOffsetAlignment;
                uniformStride = (int)(((ulong)RenderLayout.UniformStride + alignment - 1) / alignment * alignment);

                break;
            }
        }

        if (physical.Handle is 0)
        {
            throw new NotSupportedException("A Vulkan 1.3 graphics/compute/present queue and storage-image support are required.");
        }

        api.GetPhysicalDeviceMemoryProperties(physical, out memoryProperties);
        float* priorities = stackalloc float[] { 1.0f, 1.0f };
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

        HashSet<string> requestedExtensions = ["VK_KHR_swapchain"];
        if (RayQuerySupported)
        {
            requestedExtensions.UnionWith(RayExtensions);
            PhysicalDeviceAccelerationStructurePropertiesKHR accelerationProperties = new()
            {
                SType = StructureType.PhysicalDeviceAccelerationStructurePropertiesKhr
            };
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

            requestedExtensions.Add(extension);
        }

        using NativeNames deviceExtensions = new([.. requestedExtensions]);
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
        NGX.Initialize(device.Handle, instance.Handle, physical.Handle, api.Context.GetProcAddress("vkGetInstanceProcAddr"), api.Context.GetProcAddress("vkGetDeviceProcAddr"));
        InitializePresentation();
    }

    protected override void DisposeDevice()
    {
        if (device.Handle is not 0)
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

        if (instance.Handle is not 0)
        {
            if (surface.Handle is not 0)
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

    protected override void InitializeRendererCore()
    {
        InitializeFrames();
        UploadScene();
        InitializeDescriptors();
        InitializePipelines();
    }

    private void Bind(PipelineBindPoint point, Pipeline pipeline, FrameConstants constants)
    {
        VkFrame frame = slots[Frame.Slot];
        uint offset = (uint)(constantIndex++ * uniformStride);

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        *(FrameConstants*)((byte*)frame.Constants.Mapped + offset) = constants;
        api.CmdBindPipeline(commandBuffer, point, pipeline);
        DescriptorSet descriptors = frame.Descriptors;
        api.CmdBindDescriptorSets(commandBuffer, point, pipelineLayout, 0, 1, &descriptors, 1, &offset);
    }

    private void Viewport(int width, int height)
    {
        Viewport viewport = new(0, 0, width, height, 0, 1);
        Rect2D scissor = new(new(0, 0), new((uint)width, (uint)height));
        api.CmdSetViewport(commandBuffer, 0, 1, &viewport);
        api.CmdSetScissor(commandBuffer, 0, 1, &scissor);
    }

    private void Barrier(Image image, ImageLayout oldLayout, ImageLayout newLayout, ImageSubresourceRange range, CommandBuffer target = default)
    {
        ImageMemoryBarrier2 barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = oldLayout is ImageLayout.Undefined ? PipelineStageFlags2.None : PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = oldLayout is ImageLayout.Undefined ? AccessFlags2.None : AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit,
            OldLayout = oldLayout,
            NewLayout = newLayout,
            SrcQueueFamilyIndex = Vk.QueueFamilyIgnored,
            DstQueueFamilyIndex = Vk.QueueFamilyIgnored,
            Image = image,
            SubresourceRange = range
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            ImageMemoryBarrierCount = 1,
            PImageMemoryBarriers = &barrier
        };

        api.CmdPipelineBarrier2(target.Handle is 0 ? commandBuffer : target, &dependency);
    }

    private void InitializeDescriptors()
    {
        List<DescriptorSetLayoutBinding> bindings =
        [
            new()
            {
                Binding = 0,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            }
        ];
        for (uint i = 0; i < RenderLayout.SrvCount; i++)
        {
            if (i is 5 && !RayQuerySupported)
            {
                continue;
            }

            bindings.Add(new()
            {
                Binding = i + 1,
                DescriptorType = i switch
                {
                    _ when i < 5 => DescriptorType.StorageBuffer,
                    5 => DescriptorType.AccelerationStructureKhr,
                    _ => DescriptorType.SampledImage
                },
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            });
        }

        for (uint i = 0; i < RenderLayout.UavCount; i++)
        {
            bindings.Add(new()
            {
                Binding = 32 + i,
                DescriptorType = DescriptorType.StorageImage,
                DescriptorCount = 1,
                StageFlags = ShaderStageFlags.All
            });
        }

        bindings.Add(new()
        {
            Binding = 48,
            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = 1,
            StageFlags = ShaderStageFlags.All
        });
        DescriptorSetLayoutBinding[] layoutBindings = [.. bindings];

        fixed (DescriptorSetLayoutBinding* pointer = layoutBindings)
        {
            DescriptorSetLayoutCreateInfo create = new()
            {
                SType = StructureType.DescriptorSetLayoutCreateInfo,
                BindingCount = (uint)layoutBindings.Length,
                PBindings = pointer
            };

            Check(api.CreateDescriptorSetLayout(device, &create, null, out DescriptorSetLayout createdLayout), "vkCreateDescriptorSetLayout");
            descriptorLayout = createdLayout;
        }

        List<DescriptorPoolSize> sizes = [new(DescriptorType.UniformBufferDynamic, RenderLayout.FramesInFlight), new(DescriptorType.StorageBuffer, RenderLayout.FramesInFlight * 5), new(DescriptorType.SampledImage, RenderLayout.FramesInFlight * (RenderLayout.SrvCount - 6)), new(DescriptorType.StorageImage, RenderLayout.FramesInFlight * RenderLayout.UavCount), new(DescriptorType.Sampler, RenderLayout.FramesInFlight)];
        if (RayQuerySupported)
        {
            sizes.Add(new(DescriptorType.AccelerationStructureKhr, RenderLayout.FramesInFlight));
        }

        DescriptorPoolSize[] poolSizes = [.. sizes];

        fixed (DescriptorPoolSize* pointer = poolSizes)
        {
            DescriptorPoolCreateInfo poolInfo = new()
            {
                SType = StructureType.DescriptorPoolCreateInfo,
                MaxSets = RenderLayout.FramesInFlight,
                PoolSizeCount = (uint)poolSizes.Length,
                PPoolSizes = pointer
            };

            Check(api.CreateDescriptorPool(device, &poolInfo, null, out DescriptorPool createdPool), "vkCreateDescriptorPool");
            descriptorPool = createdPool;
        }

        DescriptorSetLayout setLayout = descriptorLayout;
        foreach (VkFrame frame in slots)
        {
            DescriptorSetAllocateInfo allocate = new()
            {
                SType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = 1,
                PSetLayouts = &setLayout
            };

            Check(api.AllocateDescriptorSets(device, in allocate, out frame.Descriptors), "vkAllocateDescriptorSets");
        }

        SamplerCreateInfo samplerInfo = new()
        {
            SType = StructureType.SamplerCreateInfo,
            MagFilter = Filter.Linear,
            MinFilter = Filter.Linear,
            MipmapMode = SamplerMipmapMode.Linear,
            AddressModeU = SamplerAddressMode.ClampToEdge,
            AddressModeV = SamplerAddressMode.ClampToEdge,
            AddressModeW = SamplerAddressMode.ClampToEdge,
            MaxLod = 1
        };

        Check(api.CreateSampler(device, &samplerInfo, null, out Sampler createdSampler), "vkCreateSampler");
        sampler = createdSampler;
    }

    private void InitializeFrames()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            VkFrame frame = slots[i] = new()
            {
                Api = api,
                Device = device
            };

            frame.Constants = CreateBuffer((ulong)(uniformStride * RenderLayout.UniformSlots), BufferUsageFlags.UniformBufferBit, true);
            frame.Objects = CreateBuffer((ulong)(Resources.Scene.Objects.Length * sizeof(SceneObject)), BufferUsageFlags.StorageBufferBit, true);
            CommandPoolCreateInfo pool = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamily
            };

            Check(api.CreateCommandPool(device, &pool, null, out CommandPool createdPool), "vkCreateCommandPool");
            frame.Pool = createdPool;
            CommandBufferAllocateInfo allocate = new()
            {
                SType = StructureType.CommandBufferAllocateInfo,
                CommandPool = frame.Pool,
                Level = CommandBufferLevel.Primary,
                CommandBufferCount = 1
            };

            CommandBuffer allocated;
            Check(api.AllocateCommandBuffers(device, &allocate, &allocated), "vkAllocateCommandBuffers");
            frame.Command = allocated;
            FenceCreateInfo fence = new()
            {
                SType = StructureType.FenceCreateInfo,
                Flags = FenceCreateFlags.SignaledBit
            };

            Check(api.CreateFence(device, &fence, null, out Fence createdFence), "vkCreateFence");
            frame.Fence = createdFence;
            SemaphoreCreateInfo semaphore = new()
            {
                SType = StructureType.SemaphoreCreateInfo
            };
            Check(api.CreateSemaphore(device, &semaphore, null, out Semaphore createdSemaphore), "vkCreateSemaphore(render complete)");
            frame.RenderComplete = createdSemaphore;
        }
    }

    private void UploadScene()
    {
        commandBuffer = slots[0].Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(upload)");
        recording = true;
        sceneBuffers[0] = StaticBuffer<SceneVertex>(Resources.Scene.Vertices, rayGeometry: true);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);
        UploadFont();
        MemoryBarrier2 memory = new()
        {
            SType = StructureType.MemoryBarrier2Khr,
            SrcStageMask = PipelineStageFlags2.TransferBit,
            SrcAccessMask = AccessFlags2.TransferWriteBit,
            DstStageMask = PipelineStageFlags2.AllCommandsBit,
            DstAccessMask = AccessFlags2.ShaderReadBit
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            MemoryBarrierCount = 1,
            PMemoryBarriers = &memory
        };

        api.CmdPipelineBarrier2(commandBuffer, &dependency);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        ExecuteUploads();
    }

    private void ExecuteUploads()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer(upload)");
        recording = false;
        CommandBuffer uploadCommand = commandBuffer;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &uploadCommand
        };

        Check(api.QueueSubmit(queue, 1, &submit, default), "vkQueueSubmit(upload)");
        WaitIdle();

        foreach (VkBufferResource upload in uploads)
        {
            upload.Dispose();
        }
        uploads.Clear();
    }

    private uint MemoryType(uint bits, MemoryPropertyFlags flags)
    {
        for (uint i = 0; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((bits & (1u << (int)i)) is not 0 && (memoryProperties.MemoryTypes[(int)i].PropertyFlags & flags) == flags)
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
            bool addressable = (usage & BufferUsageFlags.ShaderDeviceAddressBit) is not 0;
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

    private void InitializePipelines()
    {
        DescriptorSetLayout setLayout = descriptorLayout;
        PipelineLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout
        };

        Check(api.CreatePipelineLayout(device, &layoutInfo, null, out PipelineLayout createdPipelineLayout), "vkCreatePipelineLayout");
        pipelineLayout = createdPipelineLayout;
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in RenderLayout.ComputePasses)
        {
            string entry = pass.ToString();
            using NativeNames name = new([entry]);
            ShaderModule shader = Shader(entry, "compute");
            ComputePipelineCreateInfo create = new()
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Layout = pipelineLayout,
                Stage = new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.ComputeBit,
                    Module = shader,
                    PName = name.Pointer[0]
                }
            };

            try
            {
                Check(api.CreateComputePipelines(device, default, 1, &create, null, out Pipeline pipeline), $"vkCreateComputePipelines({entry})");
                pipelines.Add(pass, pipeline);
            }
            finally
            {
                api.DestroyShaderModule(device, shader, null);
            }
        }
    }

    private ShaderModule Shader(string entry, string stage)
    {
        byte[] bytes = ShaderCompiler.Compile("Scene.slang", entry, stage, true, RayQuerySupported);

        fixed (byte* code = bytes)
        {
            ShaderModuleCreateInfo create = new()
            {
                SType = StructureType.ShaderModuleCreateInfo,
                CodeSize = (nuint)bytes.Length,
                PCode = (uint*)code
            };

            Check(api.CreateShaderModule(device, &create, null, out ShaderModule module), $"vkCreateShaderModule({entry})");

            return module;
        }
    }

    private Pipeline GraphicsPipeline(GraphicsPass pass)
    {
        bool ui = pass is GraphicsPass.UI;
        bool depthOnly = pass is GraphicsPass.Depth or GraphicsPass.Shadow;
        (string vertexName, string fragmentName) = RenderLayout.Shaders(pass);
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        using NativeNames names = new([vertexName, fragmentName]);
        ShaderModule vertex = Shader(vertexName, "vertex");
        ShaderModule fragment = default;
        try
        {
            fragment = Shader(fragmentName, "fragment");
            PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.VertexBit,
                    Module = vertex,
                    PName = names.Pointer[0]
                },
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.FragmentBit,
                    Module = fragment,
                    PName = names.Pointer[1]
                }
            };

            VertexInputBindingDescription binding = new()
            {
                Binding = 0,
                Stride = (uint)sizeof(ImDrawVert),
                InputRate = VertexInputRate.Vertex
            };
            VertexInputAttributeDescription* attributes = stackalloc VertexInputAttributeDescription[3] { new(0, 0, Format.R32G32Sfloat, 0), new(1, 0, Format.R32G32Sfloat, 8), new(2, 0, Format.R8G8B8A8Unorm, 16) };

            PipelineVertexInputStateCreateInfo input = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = ui ? 1u : 0,
                PVertexBindingDescriptions = &binding,
                VertexAttributeDescriptionCount = ui ? 3u : 0,
                PVertexAttributeDescriptions = attributes
            };

            PipelineInputAssemblyStateCreateInfo assembly = new()
            {
                SType = StructureType.PipelineInputAssemblyStateCreateInfo,
                Topology = PrimitiveTopology.TriangleList
            };

            PipelineViewportStateCreateInfo viewport = new()
            {
                SType = StructureType.PipelineViewportStateCreateInfo,
                ViewportCount = 1,
                ScissorCount = 1
            };
            PipelineRasterizationStateCreateInfo raster = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModeFlags.None,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1
            };

            PipelineMultisampleStateCreateInfo samples = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };

            PipelineDepthStencilStateCreateInfo depth = new()
            {
                SType = StructureType.PipelineDepthStencilStateCreateInfo,
                DepthTestEnable = !ui,
                DepthWriteEnable = depthOnly,
                DepthCompareOp = pass switch
                {
                    GraphicsPass.Scene => CompareOp.Equal,
                    GraphicsPass.Depth => CompareOp.Greater,
                    _ => CompareOp.Less
                },
                MaxDepthBounds = 1
            };

            int targetCount = targets.Length;
            PipelineColorBlendAttachmentState* blendAttachments = stackalloc PipelineColorBlendAttachmentState[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                blendAttachments[i] = new()
                {
                    ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit,
                    BlendEnable = ui,
                    SrcColorBlendFactor = BlendFactor.One,
                    DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    ColorBlendOp = BlendOp.Add,
                    SrcAlphaBlendFactor = BlendFactor.One,
                    DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                    AlphaBlendOp = BlendOp.Add
                };
            }

            PipelineColorBlendStateCreateInfo blend = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = (uint)targetCount,
                PAttachments = blendAttachments
            };

            DynamicState* states = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };

            PipelineDynamicStateCreateInfo dynamic = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2,
                PDynamicStates = states
            };

            Format* formats = stackalloc Format[targetCount];
            for (int i = 0; i < targetCount; i++)
            {
                formats[i] = NativeFormat(RenderLayout.Format(targets[i]));
            }

            PipelineRenderingCreateInfo rendering = new()
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = (uint)targetCount,
                PColorAttachmentFormats = formats,
                DepthAttachmentFormat = ui ? Format.Undefined : Format.D32Sfloat
            };

            GraphicsPipelineCreateInfo create = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &rendering,
                StageCount = 2,
                PStages = stages,
                PVertexInputState = &input,
                PInputAssemblyState = &assembly,
                PViewportState = &viewport,
                PRasterizationState = &raster,
                PMultisampleState = &samples,
                PDepthStencilState = &depth,
                PColorBlendState = &blend,
                PDynamicState = &dynamic,
                Layout = pipelineLayout
            };

            Check(api.CreateGraphicsPipelines(device, default, 1, &create, null, out Pipeline pipeline), $"vkCreateGraphicsPipelines({vertexName})");

            return pipeline;
        }
        finally
        {
            api.DestroyShaderModule(device, vertex, null);
            api.DestroyShaderModule(device, fragment, null);
        }
    }

    private void InitializePresentation()
    {
        CommandPoolCreateInfo poolInfo = new()
        {
            SType = StructureType.CommandPoolCreateInfo,
            QueueFamilyIndex = queueFamily
        };

        Check(api.CreateCommandPool(device, &poolInfo, null, out CommandPool createdPool), "vkCreateCommandPool(present)");
        presentPool = createdPool;
        CommandBufferAllocateInfo commandInfo = new()
        {
            SType = StructureType.CommandBufferAllocateInfo,
            CommandPool = presentPool,
            Level = CommandBufferLevel.Primary,
            CommandBufferCount = 1
        };

        CommandBuffer command = default;
        Check(api.AllocateCommandBuffers(device, &commandInfo, &command), "vkAllocateCommandBuffers(present)");
        presentCommand = command;
        FenceCreateInfo fenceInfo = new()
        {
            SType = StructureType.FenceCreateInfo
        };
        Check(api.CreateFence(device, &fenceInfo, null, out Fence createdFence), "vkCreateFence(present)");
        presentFence = createdFence;
        SemaphoreCreateInfo semaphoreInfo = new()
        {
            SType = StructureType.SemaphoreCreateInfo
        };
        Check(api.CreateSemaphore(device, &semaphoreInfo, null, out Semaphore createdSemaphore), "vkCreateSemaphore(acquire)");
        presentAcquire = createdSemaphore;
    }

    private void DisposePresentation()
    {
        api.DestroySemaphore(device, presentAcquire, null);
        api.DestroyFence(device, presentFence, null);
        api.DestroyCommandPool(device, presentPool, null);
    }

    private VkAcceleration CreateAcceleration(AccelerationStructureTypeKHR type, ulong size)
    {
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

        return acceleration;
    }

    private VkBufferResource CreateRayScratch(ulong size)
    {
        return CreateBuffer(size + scratchAlignment - 1, BufferUsageFlags.StorageBufferBit | BufferUsageFlags.ShaderDeviceAddressBit, false);
    }

    private ulong ScratchAddress(VkBufferResource scratch)
    {
        return (scratch.Address + scratchAlignment - 1) / scratchAlignment * scratchAlignment;
    }

    private void RayBarrier(PipelineStageFlags2 sourceStage, AccessFlags2 sourceAccess, PipelineStageFlags2 destinationStage, AccessFlags2 destinationAccess)
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
        if ((ulong)Resources.Scene.Objects.Length > maxRayInstances)
        {
            throw new NotSupportedException("Scene exceeds Vulkan maxInstanceCount.");
        }

        foreach (SceneObject instance in Resources.Scene.Objects)
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
                Flags = range.Opaque is not 0 ? GeometryFlagsKHR.OpaqueBitKhr : GeometryFlagsKHR.None,
                Geometry = new()
                {
                    Triangles = new()
                    {
                        SType = StructureType.AccelerationStructureGeometryTrianglesDataKhr,
                        VertexFormat = Format.R32G32B32Sfloat,
                        VertexData = new()
                        {
                            DeviceAddress = sceneBuffers[0].Address + ((ulong)range.FirstVertex * (uint)sizeof(SceneVertex))
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
            AccelerationStructureBuildSizesInfoKHR sizes = new()
            {
                SType = StructureType.AccelerationStructureBuildSizesInfoKhr
            };
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

        RayBarrier(PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureReadBitKhr);
        uint count = (uint)Resources.Scene.Objects.Length;
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
        AccelerationStructureBuildSizesInfoKHR topSizes = new()
        {
            SType = StructureType.AccelerationStructureBuildSizesInfoKhr
        };
        accelerationApi.GetAccelerationStructureBuildSizes(device, AccelerationStructureBuildTypeKHR.DeviceKhr, &topBuild, &count, &topSizes);

        foreach (VkFrame frame in slots)
        {
            frame.Tlas = CreateAcceleration(AccelerationStructureTypeKHR.TopLevelKhr, topSizes.AccelerationStructureSize);
            frame.RayScratch = CreateRayScratch(Math.Max(topSizes.BuildScratchSize, topSizes.UpdateScratchSize));
            frame.RayInstances = CreateBuffer((ulong)(Resources.Scene.Objects.Length * sizeof(AccelerationStructureInstanceKHR)), BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr | BufferUsageFlags.ShaderDeviceAddressBit, true);
        }
    }

    private void UpdateAccelerationStructure(VkFrame frame)
    {
        // BeginCommands has waited for this slot's fence; each slot owns independent
        // TLAS, scratch and instance allocations, including during in-place updates.
        Span<AccelerationStructureInstanceKHR> instances = new(frame.RayInstances!.Mapped, Resources.Scene.Objects.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Resources.Scene.Objects[i], (uint)i, bottomLevels[i].Address);
        }

        RayBarrier(PipelineStageFlags2.HostBit | PipelineStageFlags2.ComputeShaderBit | PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.HostWriteBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.ShaderReadBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr);
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
            PrimitiveCount = (uint)Resources.Scene.Objects.Length
        };

        AccelerationStructureBuildRangeInfoKHR* ranges = &range;
        accelerationApi.CmdBuildAccelerationStructures(commandBuffer, 1, &build, &ranges);
        RayBarrier(PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.ComputeShaderBit, AccessFlags2.AccelerationStructureReadBitKhr);
        frame.TlasBuilt = true;
    }

    private void UploadFont()
    {
        VkTexture replacement = (VkTexture)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        VkBufferResource fontUpload = CreateBuffer((ulong)UI.FontPixels.Length, BufferUsageFlags.TransferSrcBit, true);
        uploads.Add(fontUpload);
        fontUpload.Write<byte>(UI.FontPixels);
        Transition(font, ImageUse.CopyDestination);
        BufferImageCopy copy = new()
        {
            ImageSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageExtent = new((uint)UI.FontWidth, (uint)UI.FontHeight, 1)
        };

        api.CmdCopyBufferToImage(commandBuffer, fontUpload.Buffer, font.Texture, ImageLayout.TransferDstOptimal, 1, &copy);
        Transition(font, ImageUse.ShaderRead);
    }

    private static void Check(Result result, string operation)
    {
        if (result is not Result.Success)
        {
            throw new InvalidOperationException($"{operation}: {result}");
        }
    }

    private static Format NativeFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba16 => Format.R16G16B16A16Sfloat,
            ImageFormat.Rgba32 => Format.R32G32B32A32Sfloat,
            ImageFormat.Rg16 => Format.R16G16Sfloat,
            ImageFormat.Float => Format.R32Sfloat,
            ImageFormat.Depth => Format.D32Sfloat,
            _ => Format.R8G8B8A8Unorm
        };
    }

    private static ImageSubresourceRange Range(ImageFormat format, int layers = 1)
    {
        return new(format is ImageFormat.Depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit, 0, 1, 0, (uint)layers);
    }

    private static AccelerationStructureGeometryKHR InstanceGeometry(ulong address)
    {
        return new()
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
    }

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
            Flags = instance.Geometry.DoubleSided is not 0 ? GeometryInstanceFlagsKHR.TriangleFacingCullDisableBitKhr : GeometryInstanceFlagsKHR.None,
            AccelerationStructureReference = address
        };
    }

    private static CompositeAlphaFlagsKHR SelectCompositeAlpha(CompositeAlphaFlagsKHR supported)
    {
        ReadOnlySpan<CompositeAlphaFlagsKHR> modes = [CompositeAlphaFlagsKHR.OpaqueBitKhr, CompositeAlphaFlagsKHR.PreMultipliedBitKhr, CompositeAlphaFlagsKHR.PostMultipliedBitKhr, CompositeAlphaFlagsKHR.InheritBitKhr];
        foreach (CompositeAlphaFlagsKHR mode in modes)
        {
            if ((supported & mode) is not 0)
            {
                return mode;
            }
        }

        throw new NotSupportedException("The surface does not expose a supported composite alpha mode.");
    }

    private class VkAcceleration : IDisposable
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

    private class VkBufferResource : IDisposable
    {
        public required Vk Api;

        public required Device Device;

        public Buffer Buffer;

        public DeviceMemory Memory;

        public ulong Size;

        public ulong Address;

        public void* Mapped;

        public void Write<T>(ReadOnlySpan<T> values, int offset = 0)
            where T : unmanaged
        {
            MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>((byte*)Mapped + offset, values.Length * sizeof(T)));
        }

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

    private class VkTexture : GpuImage
    {
        public required Vk Api;

        public required Device Device;

        public Image Texture;

        public ImageView View;

        public DeviceMemory Memory;

        public ImageLayout Layout;

        public ImageUsageFlags Usage;

        public override NativeImage Describe()
        {
            return new()
            {
                Vulkan = new()
                {
                    Type = NGXResourceVKType.VkImageView,
                    ReadWrite = (Usage & ImageUsageFlags.StorageBit) is not 0,
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
                                AspectMask = (uint)(Format is ImageFormat.Depth ? ImageAspectFlags.DepthBit : ImageAspectFlags.ColorBit),
                                LevelCount = 1,
                                LayerCount = (uint)Layers
                            }
                        }
                    }
                }
            };
        }

        public override void Dispose()
        {
            Api.DestroyImageView(Device, View, null);
            Api.DestroyImage(Device, Texture, null);
            Api.FreeMemory(Device, Memory, null);
        }
    }

    private class VkFrame : IDisposable
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
