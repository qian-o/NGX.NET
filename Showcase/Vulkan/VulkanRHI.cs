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
    private readonly VkBufferResource[] sceneBuffers = new VkBufferResource[5];
    private readonly List<VkBufferResource> uploads = [];
    private readonly Dictionary<ComputePass, Pipeline> pipelines = [];
    private readonly Lock queueSync = new();
    private readonly List<VkAcceleration> bottomLevels = [];

    private CommandBuffer commandBuffer;
    private int uniformStrideInBytes = RenderLayout.UniformStride;
    private int constantIndex;
    private bool isRecording;
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
    private bool hasSeparatePresentQueue;
    private uint queueFamily;
    private SurfaceKHR surface;
    private PipelineLayout pipelineLayout;
    private Pipeline uiPipeline;
    private CommandPool presentPool;
    private CommandBuffer presentCommand;
    private Fence presentFence;
    private Semaphore presentAcquire;
    private bool isPresentationPending;
    private uint scratchAlignmentInBytes;
    private ulong maxRayInstanceCount;
    private ulong maxRayPrimitiveCount;
    private SwapchainKHR swapChain;
    private Image[] backBuffers = [];
    private ImageLayout[] backLayouts = [];
    private Semaphore[] presentSemaphores = [];
    private Extent2D swapChainExtent;
    private VkTexture font = null!;

    public override nint Command => commandBuffer.Handle;

    public override void CreateSwapChain()
    {
        Check(surfaceApi.GetPhysicalDeviceSurfaceCapabilities(physical, surface, out SurfaceCapabilitiesKHR capabilities), "vkGetPhysicalDeviceSurfaceCapabilitiesKHR");
        Extent2D extent = capabilities.CurrentExtent;
        if (extent.Width is uint.MaxValue)
        {
            Vector2D<int> framebufferSize = Window.FramebufferSize;
            extent = new(Math.Clamp((uint)Math.Max(0, framebufferSize.X), capabilities.MinImageExtent.Width, capabilities.MaxImageExtent.Width), Math.Clamp((uint)Math.Max(0, framebufferSize.Y), capabilities.MinImageExtent.Height, capabilities.MaxImageExtent.Height));
        }

        // The surface may become minimized after the renderer checks its size.
        if (extent.Width is 0u || extent.Height is 0u)
        {
            return;
        }

        uint count = 0u;
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

        api.GetPhysicalDeviceFormatProperties(physical, selected.Format, out FormatProperties presentationFormat);

        if ((presentationFormat.OptimalTilingFeatures & FormatFeatureFlags.BlitDstBit) is 0)
        {
            throw new NotSupportedException($"The swap chain format {selected.Format} does not support image blits.");
        }

        Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, null), "vkGetPhysicalDeviceSurfacePresentModesKHR(count)");
        PresentModeKHR[] modes = new PresentModeKHR[count];

        fixed (PresentModeKHR* pointer = modes)
        {
            Check(surfaceApi.GetPhysicalDeviceSurfacePresentModes(physical, surface, &count, pointer), "vkGetPhysicalDeviceSurfacePresentModesKHR");
        }

        uint imageCount = Math.Max(capabilities.MinImageCount, RenderLayout.FramesInFlight);
        if (capabilities.MaxImageCount > 0u)
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
            ImageArrayLayers = 1u,
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

        bool isComplete = false;

        try
        {
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

            isComplete = true;
        }
        finally
        {
            if (!isComplete)
            {
                DestroySwapChain();
            }
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

        if (swapChain.Handle is not 0UL)
        {
            swapChainApi.DestroySwapchain(device, swapChain, null);
            swapChain = default;
        }
    }

    public override GpuImage CreateImage(int width, int height, ImageFormat format, int layers = 1)
    {
        ImageUsageFlags usage = ImageUsageFlags.SampledBit | ImageUsageFlags.TransferSrcBit | ImageUsageFlags.TransferDstBit | ImageUsageFlags.StorageBit;
        if (format is ImageFormat.Rgba8)
        {
            usage |= ImageUsageFlags.ColorAttachmentBit;
        }

        VkTexture texture = new(api, device)
        {
            Width = width,
            Height = height,
            Format = format,
            Layers = layers,
            Usage = usage
        };

        ImageCreateInfo info = new()
        {
            SType = StructureType.ImageCreateInfo,
            ImageType = ImageType.Type2D,
            Format = ToNativeFormat(format),
            Extent = new((uint)width, (uint)height, 1),
            MipLevels = 1u,
            ArrayLayers = (uint)layers,
            Samples = SampleCountFlags.Count1Bit,
            Tiling = ImageTiling.Optimal,
            Usage = usage
        };

        bool isComplete = false;

        try
        {
            Check(api.CreateImage(device, &info, null, out Image image), "vkCreateImage");
            texture.Texture = image;
            api.GetImageMemoryRequirements(device, texture.Texture, out MemoryRequirements requirements);
            MemoryAllocateInfo allocation = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = requirements.Size,
                MemoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, MemoryPropertyFlags.DeviceLocalBit)
            };

            Check(api.AllocateMemory(device, &allocation, null, out DeviceMemory memory), "vkAllocateMemory(image)");
            texture.Memory = memory;
            Check(api.BindImageMemory(device, texture.Texture, texture.Memory, 0UL), "vkBindImageMemory");
            ImageViewCreateInfo view = new()
            {
                SType = StructureType.ImageViewCreateInfo,
                Image = texture.Texture,
                ViewType = layers > 1 ? ImageViewType.Type2DArray : ImageViewType.Type2D,
                Format = ToNativeFormat(format),
                SubresourceRange = GetSubresourceRange(layers)
            };

            Check(api.CreateImageView(device, &view, null, out ImageView imageView), "vkCreateImageView");
            texture.View = imageView;

            isComplete = true;

            return texture;
        }
        finally
        {
            if (!isComplete)
            {
                texture.Dispose();
            }
        }
    }

    public override void UpdateDescriptors()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            VkFrame frame = slots[i];
            WriteBuffer(0u, DescriptorType.UniformBufferDynamic, frame.Constants, (ulong)sizeof(FrameConstants));

            for (uint j = 0u; j < 6u; j++)
            {
                VkBufferResource buffer = j is 4u ? frame.Objects : sceneBuffers[j is 5u ? 4u : j];
                WriteBuffer(j + 1u, DescriptorType.StorageBuffer, buffer, buffer.SizeInBytes);
            }

            AccelerationStructureKHR top = frame.Tlas!.Handle;
            WriteDescriptorSetAccelerationStructureKHR acceleration = new()
            {
                SType = StructureType.WriteDescriptorSetAccelerationStructureKhr,
                AccelerationStructureCount = 1u,
                PAccelerationStructures = &top
            };

            WriteDescriptorSet accelerationWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                PNext = &acceleration,
                DstSet = frame.Descriptors,
                DstBinding = 7u,
                DescriptorCount = 1u,
                DescriptorType = DescriptorType.AccelerationStructureKhr
            };

            api.UpdateDescriptorSets(device, 1u, &accelerationWrite, 0u, null);

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                WriteTexture(8u + (uint)slot, DescriptorType.SampledImage, (VkTexture)Resources.Frames[i][(int)slot], ImageLayout.ShaderReadOnlyOptimal);
            }

            WriteTexture(RenderLayout.FontSrv + 1, DescriptorType.SampledImage, font, ImageLayout.ShaderReadOnlyOptimal);

            for (AtmosphereImage slot = 0; slot < AtmosphereImage.Count; slot++)
            {
                VkTexture texture = (VkTexture)Resources.AtmosphereTexture(slot);
                WriteTexture((uint)(RenderLayout.AtmosphereSrv + 1 + (int)slot), DescriptorType.SampledImage, texture, ImageLayout.ShaderReadOnlyOptimal);
                WriteTexture((uint)(32 + RenderLayout.AtmosphereUav + (int)slot), DescriptorType.StorageImage, texture, ImageLayout.General);
            }

            for (int j = 0; j < RenderLayout.StorageImages.Length; j++)
            {
                WriteTexture(32u + (uint)j, DescriptorType.StorageImage, (VkTexture)Resources.Frames[i][(int)RenderLayout.StorageImages[j]], ImageLayout.General);
            }

            WriteTexture(32 + RenderLayout.FilmUav, DescriptorType.StorageImage, (VkTexture)Resources.Film, ImageLayout.General);
            WriteTexture(32 + RenderLayout.ExposureUav, DescriptorType.StorageImage, (VkTexture)Resources.Exposure, ImageLayout.General);
            DescriptorImageInfo samplerInfo = new()
            {
                Sampler = sampler
            };

            WriteDescriptorSet samplerWrite = new()
            {
                SType = StructureType.WriteDescriptorSet,
                DstSet = frame.Descriptors,
                DstBinding = RenderLayout.VulkanSamplerBinding,
                DescriptorCount = 1u,
                DescriptorType = DescriptorType.Sampler,
                PImageInfo = &samplerInfo
            };

            api.UpdateDescriptorSets(device, 1u, &samplerWrite, 0u, null);

            void WriteBuffer(uint binding, DescriptorType type, VkBufferResource buffer, ulong rangeInBytes)
            {
                DescriptorBufferInfo info = new()
                {
                    Buffer = buffer.Buffer,
                    Range = rangeInBytes
                };

                WriteDescriptorSet write = new()
                {
                    SType = StructureType.WriteDescriptorSet,
                    DstSet = frame.Descriptors,
                    DstBinding = binding,
                    DescriptorCount = 1u,
                    DescriptorType = type,
                    PBufferInfo = &info
                };

                api.UpdateDescriptorSets(device, 1u, &write, 0u, null);
            }

            void WriteTexture(uint binding, DescriptorType type, VkTexture texture, ImageLayout layout)
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
                    DescriptorCount = 1u,
                    DescriptorType = type,
                    PImageInfo = &info
                };

                api.UpdateDescriptorSets(device, 1u, &write, 0u, null);
            }
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
        isRecording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    public override void UpdateRayTracingScene()
    {
        UpdateAccelerationStructure(slots[Frame.Slot]);
    }

    public override void DrawUI(ImDrawDataPtr data)
    {
        VkFrame frame = slots[Frame.Slot];

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
            LayerCount = 1u,
            ColorAttachmentCount = 1u,
            PColorAttachments = &attachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants uiConstants = Frame.Constants;
        uiConstants.Size.Z = data.DisplaySize.X;
        uiConstants.Size.W = data.DisplaySize.Y;
        Bind(PipelineBindPoint.Graphics, uiPipeline, uiConstants);
        SetViewport(Resources.OutputWidth, Resources.OutputHeight);
        Buffer vertexBuffer = frame.Vertices!.Buffer;
        ulong offset = 0UL;
        api.CmdBindVertexBuffers(commandBuffer, 0u, 1u, &vertexBuffer, &offset);
        api.CmdBindIndexBuffer(commandBuffer, frame.Indices!.Buffer, 0UL, IndexType.Uint16);
        vertexOffset = 0;
        indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            for (int j = 0; j < list.CmdBuffer.Size; j++)
            {
                ref readonly ImDrawCmd draw = ref list.CmdBuffer.Data[j];
                if (draw.UserCallback is not null)
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
                api.CmdSetScissor(commandBuffer, 0u, 1u, &scissor);
                api.CmdDrawIndexed(commandBuffer, draw.ElemCount, 1u, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0u);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        api.CmdEndRendering(commandBuffer);

        void Ensure(ref VkBufferResource? buffer, ulong sizeInBytes, BufferUsageFlags usage)
        {
            if (buffer is not null && buffer.SizeInBytes >= sizeInBytes)
            {
                return;
            }

            VkBufferResource replacement = CreateBuffer(Math.Max(4096UL, sizeInBytes * 2UL), usage, isHostVisible: true);
            buffer?.Dispose();
            buffer = replacement;
        }
    }

    public override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(PipelineBindPoint.Compute, pipelines[pass], constants);
        api.CmdDispatch(commandBuffer, (uint)(width + 7) / 8u, (uint)(height + 7) / 8u, (uint)groupsZ);
    }

    public override void Transition(GpuImage image, ImageUse use)
    {
        VkTexture texture = (VkTexture)image;
        ImageLayout layout = use switch
        {
            ImageUse.Storage => ImageLayout.General,
            ImageUse.ColorAttachment => ImageLayout.ColorAttachmentOptimal,
            ImageUse.CopySource => ImageLayout.TransferSrcOptimal,
            ImageUse.CopyDestination => ImageLayout.TransferDstOptimal,
            _ => ImageLayout.ShaderReadOnlyOptimal
        };
        if (texture.Layout != layout || use is ImageUse.Storage)
        {
            // GENERAL-to-GENERAL orders the shared film's successive read/writes.
            InsertImageBarrier(texture.Texture, texture.Layout, layout, GetSubresourceRange(texture.Layers));
        }

        texture.Layout = layout;
    }

    public override void SubmitFrame()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer");
        isRecording = false;
        CommandBuffer command = commandBuffer;
        Semaphore complete = slots[Frame.Slot].RenderComplete;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1u,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1u,
            PSignalSemaphores = &complete
        };

        using Lock.Scope _ = queueSync.EnterScope();

        Check(api.QueueSubmit(queue, 1u, &submit, slots[Frame.Slot].Fence), "vkQueueSubmit(frame)");
    }

    public override bool PresentImage(GpuImage image)
    {
        WaitPresentation();

        if (swapChain.Handle is 0UL)
        {
            return false;
        }

        uint index = 0u;
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
        InsertImageBarrier(backBuffers[index], backLayouts[index], ImageLayout.TransferDstOptimal, GetSubresourceRange(), presentCommand);
        ImageBlit blit = new()
        {
            SrcSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            DstSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1)
        };

        blit.SrcOffsets[1] = new(image.Width, image.Height, 1);
        blit.DstOffsets[1] = new((int)swapChainExtent.Width, (int)swapChainExtent.Height, 1);
        api.CmdBlitImage(presentCommand, ((VkTexture)image).Texture, ImageLayout.TransferSrcOptimal, backBuffers[index], ImageLayout.TransferDstOptimal, 1u, &blit, Filter.Nearest);
        InsertImageBarrier(backBuffers[index], ImageLayout.TransferDstOptimal, ImageLayout.PresentSrcKhr, GetSubresourceRange(), presentCommand);
        backLayouts[index] = ImageLayout.PresentSrcKhr;
        Check(api.EndCommandBuffer(presentCommand), "vkEndCommandBuffer(present)");
        CommandBuffer command = presentCommand;
        Semaphore acquireSemaphore = presentAcquire;
        Semaphore complete = presentSemaphores[index];
        Fence fence = presentFence;
        Check(api.ResetFences(device, 1u, &fence), "vkResetFences(present)");
        PipelineStageFlags stage = PipelineStageFlags.TransferBit;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1u,
            PWaitSemaphores = &acquireSemaphore,
            PWaitDstStageMask = &stage,
            CommandBufferCount = 1u,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1u,
            PSignalSemaphores = &complete
        };

        SwapchainKHR swap = swapChain;
        PresentInfoKHR present = new()
        {
            SType = StructureType.PresentInfoKhr,
            WaitSemaphoreCount = 1u,
            PWaitSemaphores = &complete,
            SwapchainCount = 1u,
            PSwapchains = &swap,
            PImageIndices = &index
        };

        Result result;
        {
            using Lock.Scope _ = queueSync.EnterScope();

            Check(api.QueueSubmit(presentQueue, 1u, &submit, fence), "vkQueueSubmit(present)");
            isPresentationPending = true;
            result = swapChainApi.QueuePresent(presentQueue, &present);
        }

        if (result is Result.ErrorOutOfDateKhr or Result.SuboptimalKhr)
        {
            return false;
        }

        Check(result, "vkQueuePresentKHR");

        return acquire is not Result.SuboptimalKhr;
    }

    public override void WaitRenderedFrame(int slot)
    {
        Fence fence = slots[slot].Fence;
        Check(api.WaitForFences(device, 1u, &fence, true, ulong.MaxValue), "vkWaitForFences(render)");
        Semaphore complete = slots[slot].RenderComplete;
        PipelineStageFlags stage = PipelineStageFlags.AllCommandsBit;
        SubmitInfo wait = new()
        {
            SType = StructureType.SubmitInfo,
            WaitSemaphoreCount = 1u,
            PWaitSemaphores = &complete,
            PWaitDstStageMask = &stage
        };

        Fence dependencyFence = presentFence;
        Check(api.ResetFences(device, 1u, &dependencyFence), "vkResetFences(render dependency)");

        using Lock.Scope _ = queueSync.EnterScope();

        // Consume the semaphore even when acquisition cannot produce an image before this slot signals it again.
        Check(api.QueueSubmit(presentQueue, 1u, &wait, dependencyFence), "vkQueueSubmit(render dependency)");
        isPresentationPending = true;
    }

    public override void WaitPresentation()
    {
        if (isPresentationPending)
        {
            Fence fence = presentFence;
            Check(api.WaitForFences(device, 1u, &fence, true, ulong.MaxValue), "vkWaitForFences(present copy)");
            isPresentationPending = false;
        }
    }

    public override void WaitIdle()
    {
        if (device.Handle is not 0)
        {
            Check(api.DeviceWaitIdle(device), "vkDeviceWaitIdle");
        }
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

        instanceExtensions.AddRange(NGX.GetVulkanExtensions());
        ApplicationInfo application = new()
        {
            SType = StructureType.ApplicationInfo,
            ApiVersion = Vk.Version13
        };

        using NGXStrings strings = new();
        string[] instanceExtensionNames = [.. instanceExtensions.Distinct()];
        nint[] extensions = new nint[instanceExtensionNames.Length];
        for (int i = 0; i < instanceExtensionNames.Length; i++)
        {
            extensions[i] = (nint)strings.AllocUtf8(instanceExtensionNames[i]);
        }

        InstanceCreateInfo create = new()
        {
            SType = StructureType.InstanceCreateInfo,
            PApplicationInfo = &application,
            EnabledExtensionCount = (uint)extensions.Length
        };

        fixed (nint* names = extensions)
        {
            create.PpEnabledExtensionNames = (byte**)names;
            Check(api.CreateInstance(&create, null, out Instance createdInstance), "vkCreateInstance");
            instance = createdInstance;
        }

        if (!api.TryGetInstanceExtension(instance, out surfaceApi))
        {
            throw new NotSupportedException("VK_KHR_surface is unavailable.");
        }

        surface = windowSurface.Create<AllocationCallbacks>(instance.ToHandle(), null).ToSurface();
        uint count = 0u;
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
                { VendorID: 0x10DEu } => 2,
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

            PhysicalDeviceFeatures2 features = new()
            {
                SType = StructureType.PhysicalDeviceFeatures2,
                PNext = &features12
            };

            api.GetPhysicalDeviceFeatures2(candidate, &features);

            if (!features13.DynamicRendering || !features13.Synchronization2 || !features.Features.ShaderStorageImageReadWithoutFormat || !features.Features.ShaderStorageImageWriteWithoutFormat || !features.Features.ShaderStorageImageExtendedFormats || !SupportsImageFormats(candidate))
            {
                continue;
            }

            uint extensionCount = 0u;
            Check(api.EnumerateDeviceExtensionProperties(candidate, default(byte*), &extensionCount, null), "vkEnumerateDeviceExtensionProperties(count)");
            ExtensionProperties[] availableExtensions = new ExtensionProperties[extensionCount];

            fixed (ExtensionProperties* pointer = availableExtensions)
            {
                Check(api.EnumerateDeviceExtensionProperties(candidate, default(byte*), &extensionCount, pointer), "vkEnumerateDeviceExtensionProperties");
            }

            HashSet<string> extensionNames = [];
            foreach (ExtensionProperties extension in availableExtensions)
            {
                extensionNames.Add(Marshal.PtrToStringUTF8((nint)extension.ExtensionName)!);
            }

            bool isRayQuerySupported = queryFeatures.RayQuery && accelerationFeatures.AccelerationStructure && features12.BufferDeviceAddress && RayExtensions.All(extensionNames.Contains);
            api.GetPhysicalDeviceFormatProperties(candidate, Format.R32G32B32Sfloat, out FormatProperties vertexFormat);
            isRayQuerySupported &= (vertexFormat.BufferFeatures & FormatFeatureFlags.AccelerationStructureVertexBufferBitKhr) is not 0;

            if (!isRayQuerySupported)
            {
                continue;
            }

            uint familyCount = 0u;
            api.GetPhysicalDeviceQueueFamilyProperties(candidate, &familyCount, null);
            QueueFamilyProperties[] families = new QueueFamilyProperties[familyCount];

            fixed (QueueFamilyProperties* pointer = families)
            {
                api.GetPhysicalDeviceQueueFamilyProperties(candidate, &familyCount, pointer);
            }

            for (uint i = 0u; i < familyCount; i++)
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
                hasSeparatePresentQueue = families[i].QueueCount > 1u;
                RayQuerySupported = true;
                RayQueryStatus = "VK_KHR_ray_query";
                AdapterName = Marshal.PtrToStringUTF8((nint)properties.DeviceName) ?? "Vulkan GPU";
                ulong alignment = properties.Limits.MinUniformBufferOffsetAlignment;
                uniformStrideInBytes = (int)(((ulong)RenderLayout.UniformStride + alignment - 1UL) / alignment * alignment);

                break;
            }
        }

        if (physical.Handle is 0)
        {
            throw new NotSupportedException("The path tracer requires Vulkan 1.3, rayQuery, accelerationStructure, bufferDeviceAddress, storage images and a graphics/compute/present queue.");
        }

        api.GetPhysicalDeviceMemoryProperties(physical, out memoryProperties);
        float* priorities = stackalloc float[] { 1.0f, 1.0f };
        DeviceQueueCreateInfo queueInfo = new()
        {
            SType = StructureType.DeviceQueueCreateInfo,
            QueueFamilyIndex = queueFamily,
            QueueCount = hasSeparatePresentQueue ? 2u : 1u,
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
            PNext = &enabledRayQuery,
            AccelerationStructure = true
        };

        PhysicalDeviceVulkan13Features enabled13 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan13Features,
            PNext = &enabledAcceleration,
            Synchronization2 = true,
            DynamicRendering = true
        };

        PhysicalDeviceVulkan12Features enabled12 = new()
        {
            SType = StructureType.PhysicalDeviceVulkan12Features,
            PNext = &enabled13,
            BufferDeviceAddress = true
        };

        PhysicalDeviceFeatures2 enabled = new()
        {
            SType = StructureType.PhysicalDeviceFeatures2,
            PNext = &enabled12,
            Features = new()
            {
                ShaderStorageImageExtendedFormats = true,
                ShaderStorageImageReadWithoutFormat = true,
                ShaderStorageImageWriteWithoutFormat = true
            }
        };

        HashSet<string> requestedExtensions = ["VK_KHR_swapchain"];
        requestedExtensions.UnionWith(RayExtensions);
        PhysicalDeviceAccelerationStructurePropertiesKHR accelerationProperties = new()
        {
            SType = StructureType.PhysicalDeviceAccelerationStructurePropertiesKhr
        };

        PhysicalDeviceProperties2 accelerationDeviceProperties = new()
        {
            SType = StructureType.PhysicalDeviceProperties2,
            PNext = &accelerationProperties
        };

        api.GetPhysicalDeviceProperties2(physical, &accelerationDeviceProperties);
        scratchAlignmentInBytes = accelerationProperties.MinAccelerationStructureScratchOffsetAlignment;
        maxRayInstanceCount = accelerationProperties.MaxInstanceCount;
        maxRayPrimitiveCount = accelerationProperties.MaxPrimitiveCount;

        foreach (string extension in NGX.GetVulkanExtensions(instance.Handle, physical.Handle))
        {
            if (!supportedNames.Contains(extension))
            {
                throw new NotSupportedException($"NGX requires Vulkan device extension {extension}.");
            }

            requestedExtensions.Add(extension);
        }

        nint[] deviceExtensions = new nint[requestedExtensions.Count];
        int extensionIndex = 0;
        foreach (string name in requestedExtensions)
        {
            deviceExtensions[extensionIndex++] = (nint)strings.AllocUtf8(name);
        }

        DeviceCreateInfo deviceInfo = new()
        {
            SType = StructureType.DeviceCreateInfo,
            PNext = &enabled,
            QueueCreateInfoCount = 1u,
            PQueueCreateInfos = &queueInfo,
            EnabledExtensionCount = (uint)deviceExtensions.Length
        };

        fixed (nint* names = deviceExtensions)
        {
            deviceInfo.PpEnabledExtensionNames = (byte**)names;
            Check(api.CreateDevice(physical, &deviceInfo, null, out Device createdDevice), "vkCreateDevice");
            device = createdDevice;
        }

        if (!api.TryGetDeviceExtension(instance, device, out swapChainApi))
        {
            throw new NotSupportedException("VK_KHR_swapchain is unavailable.");
        }

        if (!api.TryGetDeviceExtension(instance, device, out accelerationApi))
        {
            throw new NotSupportedException("VK_KHR_acceleration_structure is unavailable.");
        }

        api.GetDeviceQueue(device, queueFamily, 0u, out queue);
        api.GetDeviceQueue(device, queueFamily, hasSeparatePresentQueue ? 1u : 0u, out presentQueue);
        NGX.Initialize(device.Handle, instance.Handle, physical.Handle, api.Context.GetProcAddress("vkGetInstanceProcAddr"), api.Context.GetProcAddress("vkGetDeviceProcAddr"));
        CreatePresentation();
    }

    protected override void InitializeRendererImpl()
    {
        CreateFrames();
        UploadScene();
        CreateDescriptors();
        CreatePipelines();
    }

    protected override void BeginCommands()
    {
        VkFrame frame = slots[Frame.Slot];
        Fence fence = frame.Fence;
        Check(api.WaitForFences(device, 1u, &fence, true, ulong.MaxValue), "vkWaitForFences(frame)");
        Check(api.ResetFences(device, 1u, &fence), "vkResetFences");
        Check(api.ResetCommandPool(device, frame.Pool, 0), "vkResetCommandPool");
        commandBuffer = frame.Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer");
        isRecording = true;
        frame.Objects.Write<SceneObject>(Resources.Scene.Objects);
        constantIndex = 0;
    }

    protected override void DestroyDevice()
    {
        if (device.Handle is not 0)
        {
            if (isRecording)
            {
                api.EndCommandBuffer(commandBuffer);
            }

            DestroySwapChain();

            foreach (Pipeline pipeline in pipelines.Values)
            {
                api.DestroyPipeline(device, pipeline, null);
            }
            pipelines.Clear();

            api.DestroyPipeline(device, uiPipeline, null);
            api.DestroyPipelineLayout(device, pipelineLayout, null);
            api.DestroySampler(device, sampler, null);
            api.DestroyDescriptorPool(device, descriptorPool, null);
            api.DestroyDescriptorSetLayout(device, descriptorLayout, null);

            for (int i = uploads.Count - 1; i >= 0; i--)
            {
                uploads[i].Dispose();
            }
            uploads.Clear();

            for (int i = slots.Length - 1; i >= 0; i--)
            {
                slots[i]?.Dispose();
            }

            for (int i = bottomLevels.Count - 1; i >= 0; i--)
            {
                bottomLevels[i].Dispose();
            }
            bottomLevels.Clear();

            font?.Dispose();

            for (int i = sceneBuffers.Length - 1; i >= 0; i--)
            {
                sceneBuffers[i]?.Dispose();
            }

            DestroyPresentation();
            api.DestroyDevice(device, null);
            device = default;
        }

        if (instance.Handle is not 0)
        {
            if (surface.Handle is not 0UL)
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

    private bool SupportsImageFormats(PhysicalDevice candidate)
    {
        foreach (ImageFormat format in Enum.GetValues<ImageFormat>())
        {
            FormatProperties3 support = new()
            {
                SType = StructureType.FormatProperties3
            };

            FormatProperties2 properties = new()
            {
                SType = StructureType.FormatProperties2,
                PNext = &support
            };
            api.GetPhysicalDeviceFormatProperties2(candidate, ToNativeFormat(format), &properties);
            FormatFeatureFlags2 required = FormatFeatureFlags2.SampledImageBit | FormatFeatureFlags2.StorageImageBit | FormatFeatureFlags2.StorageWriteWithoutFormatBit | FormatFeatureFlags2.TransferSrcBit | FormatFeatureFlags2.TransferDstBit;
            if (format is ImageFormat.Rgba32 or ImageFormat.Float)
            {
                // Film and display exposure both read their previous values.
                required |= FormatFeatureFlags2.StorageReadWithoutFormatBit;
            }

            if (format is ImageFormat.Rgba32)
            {
                required |= FormatFeatureFlags2.SampledImageFilterLinearBit;
            }

            if (format is ImageFormat.Rgba8)
            {
                required |= FormatFeatureFlags2.SampledImageFilterLinearBit | FormatFeatureFlags2.ColorAttachmentBit | FormatFeatureFlags2.ColorAttachmentBlendBit | FormatFeatureFlags2.BlitSrcBit;
            }

            if ((support.OptimalTilingFeatures & required) != required)
            {
                return false;
            }
        }

        return true;
    }

    private void Bind(PipelineBindPoint point, Pipeline pipeline, in FrameConstants constants)
    {
        VkFrame frame = slots[Frame.Slot];
        uint offsetInBytes = (uint)(constantIndex++ * uniformStrideInBytes);

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        *(FrameConstants*)((byte*)frame.Constants.Mapped + offsetInBytes) = constants;
        api.CmdBindPipeline(commandBuffer, point, pipeline);
        DescriptorSet descriptors = frame.Descriptors;
        api.CmdBindDescriptorSets(commandBuffer, point, pipelineLayout, 0u, 1u, &descriptors, 1u, &offsetInBytes);
    }

    private void SetViewport(int width, int height)
    {
        Viewport viewport = new(0.0f, 0.0f, width, height, 0.0f, 1.0f);
        Rect2D scissor = new(new(0, 0), new((uint)width, (uint)height));
        api.CmdSetViewport(commandBuffer, 0u, 1u, &viewport);
        api.CmdSetScissor(commandBuffer, 0u, 1u, &scissor);
    }

    private void InsertImageBarrier(Image image, ImageLayout oldLayout, ImageLayout newLayout, ImageSubresourceRange range, CommandBuffer target = default)
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
            ImageMemoryBarrierCount = 1u,
            PImageMemoryBarriers = &barrier
        };

        api.CmdPipelineBarrier2(target.Handle is 0 ? commandBuffer : target, &dependency);
    }

    private void CreateDescriptors()
    {
        List<DescriptorSetLayoutBinding> bindings =
        [
            new()
            {
                Binding = 0u,
                DescriptorType = DescriptorType.UniformBufferDynamic,
                DescriptorCount = 1u,
                StageFlags = ShaderStageFlags.All
            }
        ];
        for (uint i = 0u; i < RenderLayout.SrvCount; i++)
        {
            bindings.Add(new()
            {
                Binding = i + 1u,
                DescriptorType = i switch
                {
                    _ when i < 6u => DescriptorType.StorageBuffer,
                    6u => DescriptorType.AccelerationStructureKhr,
                    _ => DescriptorType.SampledImage
                },
                DescriptorCount = 1u,
                StageFlags = ShaderStageFlags.All
            });
        }

        for (uint i = 0u; i < RenderLayout.UavCount; i++)
        {
            bindings.Add(new()
            {
                Binding = 32u + i,
                DescriptorType = DescriptorType.StorageImage,
                DescriptorCount = 1u,
                StageFlags = ShaderStageFlags.All
            });
        }

        bindings.Add(new()
        {
            Binding = RenderLayout.VulkanSamplerBinding,
            DescriptorType = DescriptorType.Sampler,
            DescriptorCount = 1u,
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

        List<DescriptorPoolSize> sizes = [new(DescriptorType.UniformBufferDynamic, RenderLayout.FramesInFlight), new(DescriptorType.StorageBuffer, RenderLayout.FramesInFlight * 6), new(DescriptorType.SampledImage, RenderLayout.FramesInFlight * (RenderLayout.SrvCount - 7)), new(DescriptorType.StorageImage, RenderLayout.FramesInFlight * RenderLayout.UavCount), new(DescriptorType.Sampler, RenderLayout.FramesInFlight)];
        sizes.Add(new(DescriptorType.AccelerationStructureKhr, RenderLayout.FramesInFlight));

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
                DescriptorSetCount = 1u,
                PSetLayouts = &setLayout
            };

            Check(api.AllocateDescriptorSets(device, in allocate, out DescriptorSet descriptors), "vkAllocateDescriptorSets");
            frame.Descriptors = descriptors;
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
            MaxLod = 1.0f
        };

        Check(api.CreateSampler(device, &samplerInfo, null, out Sampler createdSampler), "vkCreateSampler");
        sampler = createdSampler;
    }

    private void CreateFrames()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            VkFrame frame = new(api, device);
            slots[i] = frame;

            frame.Constants = CreateBuffer((ulong)(uniformStrideInBytes * RenderLayout.UniformSlots), BufferUsageFlags.UniformBufferBit, isHostVisible: true);
            frame.Objects = CreateBuffer((ulong)(Resources.Scene.Objects.Length * sizeof(SceneObject)), BufferUsageFlags.StorageBufferBit, isHostVisible: true);
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
                CommandBufferCount = 1u
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
        isRecording = true;
        sceneBuffers[0] = CreateStaticBuffer<SceneVertex>(Resources.Scene.Vertices, isRayGeometry: true);
        sceneBuffers[1] = CreateStaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = CreateStaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = CreateStaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);
        SceneAreaLight[] areaLights = Resources.Scene.AreaLights.Length is 0 ? [default] : Resources.Scene.AreaLights;
        sceneBuffers[4] = CreateStaticBuffer<SceneAreaLight>(areaLights);
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
            MemoryBarrierCount = 1u,
            PMemoryBarriers = &memory
        };

        api.CmdPipelineBarrier2(commandBuffer, &dependency);
        CreateAccelerationStructures();
        ExecuteUploads();
    }

    private void ExecuteUploads()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer(upload)");
        isRecording = false;
        CommandBuffer uploadCommand = commandBuffer;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1u,
            PCommandBuffers = &uploadCommand
        };

        Check(api.QueueSubmit(queue, 1u, &submit, default), "vkQueueSubmit(upload)");
        WaitIdle();

        foreach (VkBufferResource upload in uploads)
        {
            upload.Dispose();
        }
        uploads.Clear();
    }

    private uint FindMemoryType(uint bits, MemoryPropertyFlags flags)
    {
        for (uint i = 0u; i < memoryProperties.MemoryTypeCount; i++)
        {
            if ((bits & (1u << (int)i)) is not 0u && (memoryProperties.MemoryTypes[(int)i].PropertyFlags & flags) == flags)
            {
                return i;
            }
        }

        throw new NotSupportedException($"No Vulkan memory type supports {flags}.");
    }

    private VkBufferResource CreateBuffer(ulong sizeInBytes, BufferUsageFlags usage, bool isHostVisible)
    {
        VkBufferResource resource = new(api, device, Math.Max(sizeInBytes, 4UL));

        BufferCreateInfo info = new()
        {
            SType = StructureType.BufferCreateInfo,
            Size = resource.SizeInBytes,
            Usage = usage,
            SharingMode = SharingMode.Exclusive
        };

        bool isComplete = false;

        try
        {
            Check(api.CreateBuffer(device, &info, null, out Buffer buffer), "vkCreateBuffer");
            resource.Buffer = buffer;
            api.GetBufferMemoryRequirements(device, resource.Buffer, out MemoryRequirements requirements);
            bool isAddressable = (usage & BufferUsageFlags.ShaderDeviceAddressBit) is not 0;
            MemoryAllocateFlagsInfo flags = new()
            {
                SType = StructureType.MemoryAllocateFlagsInfo,
                Flags = MemoryAllocateFlags.DeviceAddressBit
            };

            MemoryAllocateInfo allocation = new()
            {
                SType = StructureType.MemoryAllocateInfo,
                PNext = isAddressable ? &flags : null,
                AllocationSize = requirements.Size,
                MemoryTypeIndex = FindMemoryType(requirements.MemoryTypeBits, isHostVisible ? MemoryPropertyFlags.HostVisibleBit | MemoryPropertyFlags.HostCoherentBit : MemoryPropertyFlags.DeviceLocalBit)
            };

            Check(api.AllocateMemory(device, &allocation, null, out DeviceMemory memory), "vkAllocateMemory(buffer)");
            resource.Memory = memory;
            Check(api.BindBufferMemory(device, resource.Buffer, resource.Memory, 0UL), "vkBindBufferMemory");

            if (isAddressable)
            {
                BufferDeviceAddressInfo address = new()
                {
                    SType = StructureType.BufferDeviceAddressInfoKhr,
                    Buffer = resource.Buffer
                };

                resource.Address = api.GetBufferDeviceAddress(device, &address);
            }

            if (isHostVisible)
            {
                void* mapped;
                Check(api.MapMemory(device, resource.Memory, 0UL, resource.SizeInBytes, 0, &mapped), "vkMapMemory");
                resource.Mapped = mapped;
            }

            isComplete = true;

            return resource;
        }
        finally
        {
            if (!isComplete)
            {
                resource.Dispose();
            }
        }
    }

    private VkBufferResource CreateStaticBuffer<T>(ReadOnlySpan<T> data, bool isRayGeometry = false)
        where T : unmanaged
    {
        ulong sizeInBytes = (ulong)(data.Length * sizeof(T));
        BufferUsageFlags usage = BufferUsageFlags.StorageBufferBit | BufferUsageFlags.TransferDstBit;
        if (isRayGeometry)
        {
            usage |= BufferUsageFlags.ShaderDeviceAddressBit | BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr;
        }

        VkBufferResource buffer = CreateBuffer(sizeInBytes, usage, isHostVisible: false);
        bool isComplete = false;

        try
        {
            VkBufferResource upload = CreateBuffer(sizeInBytes, BufferUsageFlags.TransferSrcBit, isHostVisible: true);
            uploads.Add(upload);
            upload.Write(data);
            BufferCopy copy = new()
            {
                Size = sizeInBytes
            };

            api.CmdCopyBuffer(commandBuffer, upload.Buffer, buffer.Buffer, 1u, &copy);

            isComplete = true;

            return buffer;
        }
        finally
        {
            if (!isComplete)
            {
                buffer.Dispose();
            }
        }
    }

    private void CreatePipelines()
    {
        DescriptorSetLayout setLayout = descriptorLayout;
        PipelineLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1u,
            PSetLayouts = &setLayout
        };

        Check(api.CreatePipelineLayout(device, &layoutInfo, null, out PipelineLayout createdPipelineLayout), "vkCreatePipelineLayout");
        pipelineLayout = createdPipelineLayout;
        uiPipeline = CreateUiPipeline();

        foreach (ComputePass pass in RenderLayout.ComputePasses)
        {
            string entry = pass.ToString();
            using NGXStrings strings = new();
            ShaderModule shader = CreateShaderModule(entry, "compute");
            Pipeline pipeline = default;
            bool isComplete = false;

            try
            {
                ComputePipelineCreateInfo create = new()
                {
                    SType = StructureType.ComputePipelineCreateInfo,
                    Stage = new()
                    {
                        SType = StructureType.PipelineShaderStageCreateInfo,
                        Stage = ShaderStageFlags.ComputeBit,
                        Module = shader,
                        PName = strings.AllocUtf8(entry)
                    },
                    Layout = pipelineLayout
                };

                Check(api.CreateComputePipelines(device, default, 1u, &create, null, out pipeline), $"vkCreateComputePipelines({entry})");
                pipelines.Add(pass, pipeline);
                isComplete = true;
            }
            finally
            {
                if (!isComplete && pipeline.Handle is not 0UL)
                {
                    api.DestroyPipeline(device, pipeline, null);
                }

                api.DestroyShaderModule(device, shader, null);
            }
        }
    }

    private ShaderModule CreateShaderModule(string entry, string stage)
    {
        byte[] bytes = ShaderCompiler.Compile("Scene.slang", entry, stage, true);

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

    private Pipeline CreateUiPipeline()
    {
        const string VertexName = "UiVS";
        const string FragmentName = "UiPS";
        using NGXStrings strings = new();
        ShaderModule vertex = CreateShaderModule(VertexName, "vertex");
        ShaderModule fragment = default;
        Pipeline pipeline = default;
        bool isComplete = false;

        try
        {
            fragment = CreateShaderModule(FragmentName, "fragment");
            PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.VertexBit,
                    Module = vertex,
                    PName = strings.AllocUtf8(VertexName)
                },
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.FragmentBit,
                    Module = fragment,
                    PName = strings.AllocUtf8(FragmentName)
                }
            };

            VertexInputBindingDescription binding = new()
            {
                Binding = 0u,
                Stride = (uint)sizeof(ImDrawVert),
                InputRate = VertexInputRate.Vertex
            };
            VertexInputAttributeDescription* attributes = stackalloc VertexInputAttributeDescription[3] { new(0, 0, Format.R32G32Sfloat, 0), new(1, 0, Format.R32G32Sfloat, 8), new(2, 0, Format.R8G8B8A8Unorm, 16) };

            PipelineVertexInputStateCreateInfo input = new()
            {
                SType = StructureType.PipelineVertexInputStateCreateInfo,
                VertexBindingDescriptionCount = 1u,
                PVertexBindingDescriptions = &binding,
                VertexAttributeDescriptionCount = 3u,
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
                ViewportCount = 1u,
                ScissorCount = 1u
            };

            PipelineRasterizationStateCreateInfo raster = new()
            {
                SType = StructureType.PipelineRasterizationStateCreateInfo,
                PolygonMode = PolygonMode.Fill,
                CullMode = CullModeFlags.None,
                FrontFace = FrontFace.CounterClockwise,
                LineWidth = 1.0f
            };

            PipelineMultisampleStateCreateInfo samples = new()
            {
                SType = StructureType.PipelineMultisampleStateCreateInfo,
                RasterizationSamples = SampleCountFlags.Count1Bit
            };

            PipelineColorBlendAttachmentState blendAttachment = new()
            {
                BlendEnable = true,
                SrcColorBlendFactor = BlendFactor.One,
                DstColorBlendFactor = BlendFactor.OneMinusSrcAlpha,
                ColorBlendOp = BlendOp.Add,
                SrcAlphaBlendFactor = BlendFactor.One,
                DstAlphaBlendFactor = BlendFactor.OneMinusSrcAlpha,
                AlphaBlendOp = BlendOp.Add,
                ColorWriteMask = ColorComponentFlags.RBit | ColorComponentFlags.GBit | ColorComponentFlags.BBit | ColorComponentFlags.ABit
            };

            PipelineColorBlendStateCreateInfo blend = new()
            {
                SType = StructureType.PipelineColorBlendStateCreateInfo,
                AttachmentCount = 1u,
                PAttachments = &blendAttachment
            };

            DynamicState* states = stackalloc DynamicState[2] { DynamicState.Viewport, DynamicState.Scissor };

            PipelineDynamicStateCreateInfo dynamic = new()
            {
                SType = StructureType.PipelineDynamicStateCreateInfo,
                DynamicStateCount = 2u,
                PDynamicStates = states
            };

            Format colorFormat = ToNativeFormat(RenderLayout.Format(ImageSlot.UI));

            PipelineRenderingCreateInfo rendering = new()
            {
                SType = StructureType.PipelineRenderingCreateInfo,
                ColorAttachmentCount = 1u,
                PColorAttachmentFormats = &colorFormat
            };

            GraphicsPipelineCreateInfo create = new()
            {
                SType = StructureType.GraphicsPipelineCreateInfo,
                PNext = &rendering,
                StageCount = 2u,
                PStages = stages,
                PVertexInputState = &input,
                PInputAssemblyState = &assembly,
                PViewportState = &viewport,
                PRasterizationState = &raster,
                PMultisampleState = &samples,
                PColorBlendState = &blend,
                PDynamicState = &dynamic,
                Layout = pipelineLayout
            };

            Check(api.CreateGraphicsPipelines(device, default, 1u, &create, null, out pipeline), $"vkCreateGraphicsPipelines({VertexName})");
            isComplete = true;

            return pipeline;
        }
        finally
        {
            if (!isComplete && pipeline.Handle is not 0UL)
            {
                api.DestroyPipeline(device, pipeline, null);
            }

            api.DestroyShaderModule(device, fragment, null);
            api.DestroyShaderModule(device, vertex, null);
        }
    }

    private void CreatePresentation()
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
            CommandBufferCount = 1u
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

    private void DestroyPresentation()
    {
        api.DestroySemaphore(device, presentAcquire, null);
        presentAcquire = default;
        api.DestroyFence(device, presentFence, null);
        presentFence = default;
        api.DestroyCommandPool(device, presentPool, null);
        presentPool = default;
        presentCommand = default;
        isPresentationPending = false;
    }

    private VkAcceleration CreateAcceleration(AccelerationStructureTypeKHR type, ulong sizeInBytes)
    {
        VkAcceleration acceleration = new(accelerationApi, device);
        bool isComplete = false;

        try
        {
            acceleration.Storage = CreateBuffer(sizeInBytes, BufferUsageFlags.AccelerationStructureStorageBitKhr | BufferUsageFlags.ShaderDeviceAddressBit, isHostVisible: false);
            AccelerationStructureCreateInfoKHR create = new()
            {
                SType = StructureType.AccelerationStructureCreateInfoKhr,
                Buffer = acceleration.Storage.Buffer,
                Size = sizeInBytes,
                Type = type
            };

            AccelerationStructureKHR handle;
            Check(accelerationApi.CreateAccelerationStructure(device, &create, null, &handle), "vkCreateAccelerationStructureKHR");
            acceleration.Handle = handle;
            AccelerationStructureDeviceAddressInfoKHR address = new()
            {
                SType = StructureType.AccelerationStructureDeviceAddressInfoKhr,
                AccelerationStructure = handle
            };

            acceleration.Address = accelerationApi.GetAccelerationStructureDeviceAddress(device, &address);
            isComplete = true;

            return acceleration;
        }
        finally
        {
            if (!isComplete)
            {
                acceleration.Dispose();
            }
        }
    }

    private VkBufferResource CreateRayScratch(ulong sizeInBytes)
    {
        return CreateBuffer(sizeInBytes + scratchAlignmentInBytes - 1UL, BufferUsageFlags.StorageBufferBit | BufferUsageFlags.ShaderDeviceAddressBit, isHostVisible: false);
    }

    private ulong GetScratchAddress(VkBufferResource scratch)
    {
        return (scratch.Address + scratchAlignmentInBytes - 1UL) / scratchAlignmentInBytes * scratchAlignmentInBytes;
    }

    private void InsertRayBarrier(PipelineStageFlags2 srcStage, AccessFlags2 srcAccess, PipelineStageFlags2 dstStage, AccessFlags2 dstAccess)
    {
        MemoryBarrier2 memory = new()
        {
            SType = StructureType.MemoryBarrier2Khr,
            SrcStageMask = srcStage,
            SrcAccessMask = srcAccess,
            DstStageMask = dstStage,
            DstAccessMask = dstAccess
        };

        DependencyInfo dependency = new()
        {
            SType = StructureType.DependencyInfoKhr,
            MemoryBarrierCount = 1u,
            PMemoryBarriers = &memory
        };

        api.CmdPipelineBarrier2(commandBuffer, &dependency);
    }

    private void CreateAccelerationStructures()
    {
        if ((ulong)Resources.Scene.Objects.Length > maxRayInstanceCount)
        {
            throw new NotSupportedException("Scene exceeds Vulkan maxInstanceCount.");
        }

        foreach (SceneObject instance in Resources.Scene.Objects)
        {
            GeometryRange range = instance.Geometry;
            uint primitiveCount = range.VertexCount / 3u;
            if (primitiveCount > maxRayPrimitiveCount)
            {
                throw new NotSupportedException("Scene exceeds Vulkan maxPrimitiveCount.");
            }

            AccelerationStructureGeometryKHR geometry = new()
            {
                SType = StructureType.AccelerationStructureGeometryKhr,
                GeometryType = GeometryTypeKHR.TrianglesKhr,
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
                        MaxVertex = range.VertexCount - 1u,
                        IndexType = IndexType.NoneKhr
                    }
                },
                Flags = range.Opaque is not 0u ? GeometryFlagsKHR.OpaqueBitKhr : GeometryFlagsKHR.None
            };

            AccelerationStructureBuildGeometryInfoKHR build = new()
            {
                SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
                Type = AccelerationStructureTypeKHR.BottomLevelKhr,
                Flags = BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
                Mode = BuildAccelerationStructureModeKHR.BuildKhr,
                GeometryCount = 1u,
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
            build.ScratchData.DeviceAddress = GetScratchAddress(scratch);
            AccelerationStructureBuildRangeInfoKHR rangeInfo = new()
            {
                PrimitiveCount = primitiveCount
            };

            AccelerationStructureBuildRangeInfoKHR* ranges = &rangeInfo;
            accelerationApi.CmdBuildAccelerationStructures(commandBuffer, 1u, &build, &ranges);
        }

        InsertRayBarrier(PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureReadBitKhr);
        uint count = (uint)Resources.Scene.Objects.Length;
        AccelerationStructureGeometryKHR instances = CreateInstanceGeometry(0UL);
        AccelerationStructureBuildGeometryInfoKHR topBuild = new()
        {
            SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
            Type = AccelerationStructureTypeKHR.TopLevelKhr,
            Flags = BuildAccelerationStructureFlagsKHR.AllowUpdateBitKhr | BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
            Mode = BuildAccelerationStructureModeKHR.BuildKhr,
            GeometryCount = 1u,
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
            frame.RayInstances = CreateBuffer((ulong)(Resources.Scene.Objects.Length * sizeof(AccelerationStructureInstanceKHR)), BufferUsageFlags.AccelerationStructureBuildInputReadOnlyBitKhr | BufferUsageFlags.ShaderDeviceAddressBit, isHostVisible: true);
        }
    }

    private void UpdateAccelerationStructure(VkFrame frame)
    {
        ulong transformVersion = Resources.Scene.TransformVersion;
        if (frame.IsTlasBuilt && frame.TlasVersion == transformVersion)
        {
            return;
        }

        // Each slot owns its TLAS, scratch and instance allocations until its fence completes.
        Span<AccelerationStructureInstanceKHR> instances = new(frame.RayInstances!.Mapped, Resources.Scene.Objects.Length);
        for (int i = 0; i < instances.Length; i++)
        {
            instances[i] = CreateRayInstance(Resources.Scene.Objects[i], (uint)i, bottomLevels[i].Address);
        }

        InsertRayBarrier(PipelineStageFlags2.HostBit | PipelineStageFlags2.ComputeShaderBit | PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.HostWriteBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.ShaderReadBit | AccessFlags2.AccelerationStructureReadBitKhr | AccessFlags2.AccelerationStructureWriteBitKhr);
        AccelerationStructureGeometryKHR geometry = CreateInstanceGeometry(frame.RayInstances.Address);
        AccelerationStructureBuildGeometryInfoKHR build = new()
        {
            SType = StructureType.AccelerationStructureBuildGeometryInfoKhr,
            Type = AccelerationStructureTypeKHR.TopLevelKhr,
            Flags = BuildAccelerationStructureFlagsKHR.AllowUpdateBitKhr | BuildAccelerationStructureFlagsKHR.PreferFastTraceBitKhr,
            Mode = frame.IsTlasBuilt ? BuildAccelerationStructureModeKHR.UpdateKhr : BuildAccelerationStructureModeKHR.BuildKhr,
            SrcAccelerationStructure = frame.IsTlasBuilt ? frame.Tlas!.Handle : default,
            DstAccelerationStructure = frame.Tlas!.Handle,
            GeometryCount = 1u,
            PGeometries = &geometry,
            ScratchData = new()
            {
                DeviceAddress = GetScratchAddress(frame.RayScratch!)
            }
        };

        AccelerationStructureBuildRangeInfoKHR range = new()
        {
            PrimitiveCount = (uint)Resources.Scene.Objects.Length
        };

        AccelerationStructureBuildRangeInfoKHR* ranges = &range;
        accelerationApi.CmdBuildAccelerationStructures(commandBuffer, 1u, &build, &ranges);
        InsertRayBarrier(PipelineStageFlags2.AccelerationStructureBuildBitKhr, AccessFlags2.AccelerationStructureWriteBitKhr, PipelineStageFlags2.ComputeShaderBit, AccessFlags2.AccelerationStructureReadBitKhr);
        frame.IsTlasBuilt = true;
        frame.TlasVersion = transformVersion;
    }

    private void UploadFont()
    {
        VkTexture replacement = (VkTexture)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        VkBufferResource fontUpload = CreateBuffer((ulong)UI.FontPixels.Length, BufferUsageFlags.TransferSrcBit, isHostVisible: true);
        uploads.Add(fontUpload);
        fontUpload.Write<byte>(UI.FontPixels);
        Transition(font, ImageUse.CopyDestination);
        BufferImageCopy copy = new()
        {
            ImageSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageExtent = new((uint)UI.FontWidth, (uint)UI.FontHeight, 1)
        };

        api.CmdCopyBufferToImage(commandBuffer, fontUpload.Buffer, font.Texture, ImageLayout.TransferDstOptimal, 1u, &copy);
        Transition(font, ImageUse.ShaderRead);
    }

    private static void Check(Result result, string operation)
    {
        if (result is not Result.Success)
        {
            throw new InvalidOperationException($"{operation} failed with {result}.");
        }
    }

    private static Format ToNativeFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Rgba16 => Format.R16G16B16A16Sfloat,
            ImageFormat.Rgba32 => Format.R32G32B32A32Sfloat,
            ImageFormat.Rg16 => Format.R16G16Sfloat,
            ImageFormat.Float => Format.R32Sfloat,
            _ => Format.R8G8B8A8Unorm
        };
    }

    private static ImageSubresourceRange GetSubresourceRange(int layers = 1)
    {
        return new(ImageAspectFlags.ColorBit, 0, 1, 0, (uint)layers);
    }

    private static AccelerationStructureGeometryKHR CreateInstanceGeometry(ulong address)
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
        Vector4 offset = instance.Offset;

        TransformMatrixKHR transform = default;
        transform.Matrix[0] = 1.0f;
        transform.Matrix[3] = offset.X;
        transform.Matrix[5] = 1.0f;
        transform.Matrix[7] = offset.Y;
        transform.Matrix[10] = 1.0f;
        transform.Matrix[11] = offset.Z;

        return new()
        {
            Transform = transform,
            InstanceCustomIndex = id,
            Mask = byte.MaxValue,
            Flags = instance.Geometry.DoubleSided is not 0u ? GeometryInstanceFlagsKHR.TriangleFacingCullDisableBitKhr : GeometryInstanceFlagsKHR.None,
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

    private class VkAcceleration(KhrAccelerationStructure api, Device device) : DisposableObject
    {
        public VkBufferResource Storage = null!;

        public AccelerationStructureKHR Handle;

        public ulong Address;

        protected override void Destroy()
        {
            if (api is null)
            {
                return;
            }

            if (Handle.Handle is not 0UL)
            {
                api.DestroyAccelerationStructure(device, Handle, null);
            }

            Storage?.Dispose();
        }
    }

    private class VkBufferResource(Vk api, Device device, ulong sizeInBytes) : DisposableObject
    {
        public Buffer Buffer;

        public DeviceMemory Memory;

        public ulong Address;

        public void* Mapped;

        public ulong SizeInBytes { get; } = sizeInBytes;

        public void Write<T>(ReadOnlySpan<T> values, int offsetInBytes = 0)
            where T : unmanaged
        {
            MemoryMarshal.AsBytes(values).CopyTo(new Span<byte>((byte*)Mapped + offsetInBytes, values.Length * sizeof(T)));
        }

        protected override void Destroy()
        {
            if (api is null)
            {
                return;
            }

            if (Mapped is not null)
            {
                api.UnmapMemory(device, Memory);
            }

            if (Buffer.Handle is not 0UL)
            {
                api.DestroyBuffer(device, Buffer, null);
            }

            if (Memory.Handle is not 0UL)
            {
                api.FreeMemory(device, Memory, null);
            }
        }
    }

    private class VkTexture(Vk api, Device device) : GpuImage
    {
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
                    Resource = new()
                    {
                        ImageViewInfo = new()
                        {
                            ImageView = (nint)View.Handle,
                            Image = (nint)Texture.Handle,
                            SubresourceRange = new()
                            {
                                AspectMask = (uint)ImageAspectFlags.ColorBit,
                                LevelCount = 1u,
                                LayerCount = (uint)Layers
                            },
                            Format = (NGXVkFormat)ToNativeFormat(Format),
                            Width = (uint)Width,
                            Height = (uint)Height
                        }
                    },
                    Type = NGXResourceVKType.VkImageView,
                    ReadWrite = (Usage & ImageUsageFlags.StorageBit) is not 0
                }
            };
        }

        protected override void Destroy()
        {
            if (api is null)
            {
                return;
            }

            if (View.Handle is not 0UL)
            {
                api.DestroyImageView(device, View, null);
            }

            if (Texture.Handle is not 0UL)
            {
                api.DestroyImage(device, Texture, null);
            }

            if (Memory.Handle is not 0UL)
            {
                api.FreeMemory(device, Memory, null);
            }
        }
    }

    private class VkFrame(Vk api, Device device) : DisposableObject
    {
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

        public bool IsTlasBuilt;

        public ulong TlasVersion;

        protected override void Destroy()
        {
            if (api is null)
            {
                return;
            }

            Indices?.Dispose();
            Vertices?.Dispose();
            RayInstances?.Dispose();
            RayScratch?.Dispose();
            Tlas?.Dispose();

            if (RenderComplete.Handle is not 0UL)
            {
                api.DestroySemaphore(device, RenderComplete, null);
            }

            if (Fence.Handle is not 0UL)
            {
                api.DestroyFence(device, Fence, null);
            }

            if (Pool.Handle is not 0UL)
            {
                api.DestroyCommandPool(device, Pool, null);
            }

            Objects?.Dispose();
            Constants?.Dispose();
        }
    }
}
