using System.Numerics;
using ImGuiNET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    protected override void InitializeRenderer()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            VkFrame frame = new()
            {
                Api = api,
                Constants = CreateBuffer((ulong)(uniformStride * RenderLayout.UniformSlots), VkBufferUsageFlags.UniformBuffer, true),
                Objects = CreateBuffer((ulong)(Scene.Objects.Length * sizeof(SceneObject)), VkBufferUsageFlags.StorageBuffer, true)
            };
            slots[i] = frame;
            VkCommandPoolCreateInfo pool = new()
            {
                queueFamilyIndex = queueFamily
            };
            Check(api.vkCreateCommandPool(&pool, null, out frame.Pool), "vkCreateCommandPool");
            VkCommandBufferAllocateInfo allocate = new()
            {
                commandPool = frame.Pool,
                level = VkCommandBufferLevel.Primary,
                commandBufferCount = 1
            };
            VkCommandBuffer allocated;
            Check(api.vkAllocateCommandBuffers(&allocate, &allocated), "vkAllocateCommandBuffers");
            frame.Command = allocated;
            VkFenceCreateInfo fence = new()
            {
                flags = VkFenceCreateFlags.Signaled
            };
            Check(api.vkCreateFence(&fence, null, out frame.Fence), "vkCreateFence");
            VkSemaphoreCreateInfo semaphore = new();
            Check(api.vkCreateSemaphore(&semaphore, null, out frame.Acquire), "vkCreateSemaphore(acquire)");
        }

        commandBuffer = slots[0].Command;
        VkCommandBufferBeginInfo begin = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        Check(api.vkBeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(upload)");
        recording = true;
        sceneBuffers[0] = StaticBuffer<SceneVertex>(Scene.Vertices, rayGeometry: true);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Scene.TextureInfo);
        font = (VkTexture)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        VkBufferResource fontUpload = CreateBuffer((ulong)UI.FontPixels.Length, VkBufferUsageFlags.TransferSrc, true);
        fontUpload.Write<byte>(UI.FontPixels);
        uploads.Add(fontUpload);
        Transition(font, ImageUse.CopyDestination);
        VkBufferImageCopy copy = new()
        {
            imageSubresource = new(VkImageAspectFlags.Color, 0, 0, 1),
            imageExtent = new((uint)UI.FontWidth, (uint)UI.FontHeight, 1)
        };
        api.vkCmdCopyBufferToImage(commandBuffer, fontUpload.Buffer, font.Texture, VkImageLayout.TransferDstOptimal, 1, &copy);
        Transition(font, ImageUse.ShaderRead);
        VkMemoryBarrier2 memory = new()
        {
            srcStageMask = VkPipelineStageFlags2.Transfer,
            srcAccessMask = VkAccessFlags2.TransferWrite,
            dstStageMask = VkPipelineStageFlags2.AllCommands,
            dstAccessMask = VkAccessFlags2.ShaderRead
        };
        VkDependencyInfo dependency = new()
        {
            memoryBarrierCount = 1,
            pMemoryBarriers = &memory
        };
        api.vkCmdPipelineBarrier2(commandBuffer, &dependency);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        Check(api.vkEndCommandBuffer(commandBuffer), "vkEndCommandBuffer(upload)");
        recording = false;
        VkCommandBuffer uploadCommand = commandBuffer;
        VkSubmitInfo submit = new()
        {
            commandBufferCount = 1,
            pCommandBuffers = &uploadCommand
        };
        Check(api.vkQueueSubmit(queue, 1, &submit, default), "vkQueueSubmit(upload)");
        WaitIdle();

        foreach (VkBufferResource upload in uploads)
        {
            upload.Dispose();
        }

        uploads.Clear();
        List<VkDescriptorSetLayoutBinding> bindings =
        [
            new()
            {
                binding = 0,
                descriptorType = VkDescriptorType.UniformBufferDynamic,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.All
            }
        ];

        for (uint i = 0; i < RenderLayout.SrvCount; i++)
        {
            if (i == 5 && !RayQuerySupported)
            {
                continue;
            }

            bindings.Add(new()
            {
                binding = i + 1,
                descriptorType = i < 5 ? VkDescriptorType.StorageBuffer : i == 5 ? VkDescriptorType.AccelerationStructureKHR : VkDescriptorType.SampledImage,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.All
            });
        }

        for (uint i = 0; i < RenderLayout.UavCount; i++)
        {
            bindings.Add(new()
            {
                binding = 32 + i,
                descriptorType = VkDescriptorType.StorageImage,
                descriptorCount = 1,
                stageFlags = VkShaderStageFlags.All
            });
        }

        bindings.Add(new()
        {
            binding = 48,
            descriptorType = VkDescriptorType.Sampler,
            descriptorCount = 1,
            stageFlags = VkShaderStageFlags.All
        });
        VkDescriptorSetLayoutBinding[] layoutBindings = [.. bindings];

        fixed (VkDescriptorSetLayoutBinding* pointer = layoutBindings)
        {
            VkDescriptorSetLayoutCreateInfo create = new()
            {
                bindingCount = (uint)layoutBindings.Length,
                pBindings = pointer
            };
            Check(api.vkCreateDescriptorSetLayout(&create, null, out descriptorLayout), "vkCreateDescriptorSetLayout");
        }

        List<VkDescriptorPoolSize> sizes =
        [
            new(VkDescriptorType.UniformBufferDynamic, RenderLayout.FramesInFlight),
            new(VkDescriptorType.StorageBuffer, RenderLayout.FramesInFlight * 5),
            new(VkDescriptorType.SampledImage, RenderLayout.FramesInFlight * (RenderLayout.SrvCount - 6)),
            new(VkDescriptorType.StorageImage, RenderLayout.FramesInFlight * RenderLayout.UavCount),
            new(VkDescriptorType.Sampler, RenderLayout.FramesInFlight)
        ];

        if (RayQuerySupported)
        {
            sizes.Add(new(VkDescriptorType.AccelerationStructureKHR, RenderLayout.FramesInFlight));
        }

        VkDescriptorPoolSize[] poolSizes = [.. sizes];

        fixed (VkDescriptorPoolSize* pointer = poolSizes)
        {
            VkDescriptorPoolCreateInfo poolInfo = new()
            {
                maxSets = RenderLayout.FramesInFlight,
                poolSizeCount = (uint)poolSizes.Length,
                pPoolSizes = pointer
            };
            Check(api.vkCreateDescriptorPool(&poolInfo, null, out descriptorPool), "vkCreateDescriptorPool");
        }

        VkDescriptorSetLayout setLayout = descriptorLayout;

        foreach (VkFrame frame in slots)
        {
            VkDescriptorSetAllocateInfo allocate = new()
            {
                descriptorPool = descriptorPool,
                descriptorSetCount = 1,
                pSetLayouts = &setLayout
            };
            Check(api.vkAllocateDescriptorSets(in allocate, out frame.Descriptors), "vkAllocateDescriptorSets");
        }

        VkSamplerCreateInfo samplerInfo = new()
        {
            magFilter = VkFilter.Linear,
            minFilter = VkFilter.Linear,
            mipmapMode = VkSamplerMipmapMode.Linear,
            addressModeU = VkSamplerAddressMode.ClampToEdge,
            addressModeV = VkSamplerAddressMode.ClampToEdge,
            addressModeW = VkSamplerAddressMode.ClampToEdge,
            maxLod = 1
        };
        Check(api.vkCreateSampler(&samplerInfo, null, out sampler), "vkCreateSampler");
        VkPipelineLayoutCreateInfo layoutInfo = new()
        {
            setLayoutCount = 1,
            pSetLayouts = &setLayout
        };
        Check(api.vkCreatePipelineLayout(&layoutInfo, null, out pipelineLayout), "vkCreatePipelineLayout");
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
        {
            string entry = pass.ToString();
            VkShaderModule shader = Shader(entry, "compute");
            using NativeText name = new(entry);
            VkComputePipelineCreateInfo create = new()
            {
                layout = pipelineLayout,
                stage = new()
                {
                    stage = VkShaderStageFlags.Compute,
                    module = shader,
                    pName = name.Pointer
                }
            };

            try
            {
                Check(api.vkCreateComputePipeline(create, out VkPipeline pipeline), $"vkCreateComputePipeline({entry})");
                pipelines.Add(pass, pipeline);
            }
            finally
            {
                api.vkDestroyShaderModule(shader);
            }
        }
    }

    private VkShaderModule Shader(string entry, string stage)
    {
        byte[] bytes = ShaderCompiler.Compile("Scene.slang", entry, stage, true, RayQuerySupported);

        fixed (byte* code = bytes)
        {
            VkShaderModuleCreateInfo create = new()
            {
                codeSize = (nuint)bytes.Length,
                pCode = (uint*)code
            };
            Check(api.vkCreateShaderModule(&create, null, out VkShaderModule module), $"vkCreateShaderModule({entry})");

            return module;
        }
    }

    private VkPipeline GraphicsPipeline(GraphicsPass pass)
    {
        bool ui = pass == GraphicsPass.UI;
        bool depthOnly = pass is GraphicsPass.Depth or GraphicsPass.Shadow;
        (string vertexName, string fragmentName) = RenderLayout.Shaders(pass);
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        VkShaderModule vertex = Shader(vertexName, "vertex"), fragment = Shader(fragmentName, "fragment");

        try
        {
            using NativeText vsName = new(vertexName);
            using NativeText psName = new(fragmentName);
            VkPipelineShaderStageCreateInfo* stages = stackalloc VkPipelineShaderStageCreateInfo[2]
            {
                new()
                {
                    stage = VkShaderStageFlags.Vertex,
                    module = vertex,
                    pName = vsName.Pointer
                },
                new()
                {
                    stage = VkShaderStageFlags.Fragment,
                    module = fragment,
                    pName = psName.Pointer
                }
            };
            VkVertexInputBindingDescription binding = new((uint)sizeof(ImDrawVert));
            VkVertexInputAttributeDescription* attributes = stackalloc VkVertexInputAttributeDescription[3]
            {
                new(0, VkFormat.R32G32Sfloat, 0),
                new(1, VkFormat.R32G32Sfloat, 8),
                new(2, VkFormat.R8G8B8A8Unorm, 16)
            };
            VkPipelineVertexInputStateCreateInfo input = new()
            {
                vertexBindingDescriptionCount = ui ? 1u : 0,
                pVertexBindingDescriptions = &binding,
                vertexAttributeDescriptionCount = ui ? 3u : 0,
                pVertexAttributeDescriptions = attributes
            };
            VkPipelineInputAssemblyStateCreateInfo assembly = new(VkPrimitiveTopology.TriangleList);
            VkPipelineViewportStateCreateInfo viewport = new(1, 1);
            VkPipelineRasterizationStateCreateInfo raster = new()
            {
                polygonMode = VkPolygonMode.Fill,
                cullMode = VkCullModeFlags.None,
                frontFace = VkFrontFace.CounterClockwise,
                lineWidth = 1
            };
            VkPipelineMultisampleStateCreateInfo samples = new()
            {
                rasterizationSamples = VkSampleCountFlags.Count1
            };
            VkPipelineDepthStencilStateCreateInfo depth = new()
            {
                depthTestEnable = !ui,
                depthWriteEnable = depthOnly,
                depthCompareOp = pass switch
                {
                    GraphicsPass.Scene => VkCompareOp.Equal,
                    GraphicsPass.Depth => VkCompareOp.Greater,
                    _ => VkCompareOp.Less
                },
                maxDepthBounds = 1
            };
            int targetCount = targets.Length;
            VkPipelineColorBlendAttachmentState* blendAttachments = stackalloc VkPipelineColorBlendAttachmentState[targetCount];

            for (int i = 0; i < targetCount; i++)
            {
                blendAttachments[i] = new()
                {
                    colorWriteMask = VkColorComponentFlags.All,
                    blendEnable = ui,
                    srcColorBlendFactor = VkBlendFactor.One,
                    dstColorBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    colorBlendOp = VkBlendOp.Add,
                    srcAlphaBlendFactor = VkBlendFactor.One,
                    dstAlphaBlendFactor = VkBlendFactor.OneMinusSrcAlpha,
                    alphaBlendOp = VkBlendOp.Add
                };
            }

            VkPipelineColorBlendStateCreateInfo blend = new()
            {
                attachmentCount = (uint)targetCount,
                pAttachments = blendAttachments
            };
            VkDynamicState* states = stackalloc VkDynamicState[2]
            {
                VkDynamicState.Viewport,
                VkDynamicState.Scissor
            };
            VkPipelineDynamicStateCreateInfo dynamic = new()
            {
                dynamicStateCount = 2,
                pDynamicStates = states
            };
            VkFormat* formats = stackalloc VkFormat[targetCount];

            for (int i = 0; i < targetCount; i++)
            {
                formats[i] = NativeFormat(RenderLayout.Format(targets[i]));
            }

            VkPipelineRenderingCreateInfo rendering = new()
            {
                colorAttachmentCount = (uint)targetCount,
                pColorAttachmentFormats = formats,
                depthAttachmentFormat = ui ? VkFormat.Undefined : VkFormat.D32Sfloat
            };
            VkGraphicsPipelineCreateInfo create = new()
            {
                pNext = &rendering,
                stageCount = 2,
                pStages = stages,
                pVertexInputState = &input,
                pInputAssemblyState = &assembly,
                pViewportState = &viewport,
                pRasterizationState = &raster,
                pMultisampleState = &samples,
                pDepthStencilState = &depth,
                pColorBlendState = &blend,
                pDynamicState = &dynamic,
                layout = pipelineLayout
            };
            Check(api.vkCreateGraphicsPipeline(create, out VkPipeline pipeline), $"vkCreateGraphicsPipeline({vertexName})");

            return pipeline;
        }
        finally
        {
            api.vkDestroyShaderModule(vertex);
            api.vkDestroyShaderModule(fragment);
        }
    }

    protected override void UpdateDescriptors()
    {
        foreach ((VkFrame frame, int index) in slots.Select((frame, index) => (frame, index)))
        {
            void Buffer(uint binding, VkDescriptorType type, VkBufferResource buffer, ulong range)
            {
                VkDescriptorBufferInfo info = new()
                {
                    buffer = buffer.Buffer,
                    range = range
                };
                VkWriteDescriptorSet write = new()
                {
                    dstSet = frame.Descriptors,
                    dstBinding = binding,
                    descriptorCount = 1,
                    descriptorType = type,
                    pBufferInfo = &info
                };
                api.vkUpdateDescriptorSets(1, &write, 0, null);
            }

            void Texture(uint binding, VkDescriptorType type, VkTexture texture, VkImageLayout layout)
            {
                VkDescriptorImageInfo info = new()
                {
                    imageView = texture.View,
                    imageLayout = layout
                };
                VkWriteDescriptorSet write = new()
                {
                    dstSet = frame.Descriptors,
                    dstBinding = binding,
                    descriptorCount = 1,
                    descriptorType = type,
                    pImageInfo = &info
                };
                api.vkUpdateDescriptorSets(1, &write, 0, null);
            }

            Buffer(0, VkDescriptorType.UniformBufferDynamic, frame.Constants, (ulong)sizeof(FrameConstants));

            for (uint i = 0; i < 5; i++)
            {
                VkBufferResource buffer = i == 4 ? frame.Objects : sceneBuffers[i];
                Buffer(i + 1, VkDescriptorType.StorageBuffer, buffer, buffer.Size);
            }

            if (RayQuerySupported)
            {
                VkAccelerationStructureKHR top = frame.Tlas!.Handle;
                VkWriteDescriptorSetAccelerationStructureKHR acceleration = new()
                {
                    accelerationStructureCount = 1,
                    pAccelerationStructures = &top
                };
                VkWriteDescriptorSet write = new()
                {
                    pNext = &acceleration,
                    dstSet = frame.Descriptors,
                    dstBinding = 6,
                    descriptorCount = 1,
                    descriptorType = VkDescriptorType.AccelerationStructureKHR
                };
                api.vkUpdateDescriptorSets(1, &write, 0, null);
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                Texture(7 + (uint)slot, VkDescriptorType.SampledImage, (VkTexture)Frames[index][(int)slot], VkImageLayout.ShaderReadOnlyOptimal);
            }

            int previousFrame = (index + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            Texture(RenderLayout.PreviousExposureSrv + 1, VkDescriptorType.SampledImage, (VkTexture)Frames[previousFrame][(int)ImageSlot.Exposure], VkImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.FontSrv + 1, VkDescriptorType.SampledImage, font, VkImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.LightingSamplesSrv + 1, VkDescriptorType.SampledImage, (VkTexture)LightingSamples, VkImageLayout.ShaderReadOnlyOptimal);

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                Texture(32 + (uint)i, VkDescriptorType.StorageImage, (VkTexture)Frames[index][(int)RenderLayout.StorageImages[i]], VkImageLayout.General);
            }

            Texture(32 + RenderLayout.LightingSamplesUav, VkDescriptorType.StorageImage, (VkTexture)LightingSamples, VkImageLayout.General);
            VkDescriptorImageInfo samplerInfo = new()
            {
                sampler = sampler
            };
            VkWriteDescriptorSet samplerWrite = new()
            {
                dstSet = frame.Descriptors,
                dstBinding = 48,
                descriptorCount = 1,
                descriptorType = VkDescriptorType.Sampler,
                pImageInfo = &samplerInfo
            };
            api.vkUpdateDescriptorSets(1, &samplerWrite, 0, null);
        }
    }

    protected override bool BeginCommands()
    {
        VkFrame frame = slots[FrameSlot];
        VkFence fence = frame.Fence;
        Check(api.vkWaitForFences(1, &fence, true, ulong.MaxValue), "vkWaitForFences(frame)");
        VkResult acquire = api.vkAcquireNextImageKHR(swapChain, ulong.MaxValue, frame.Acquire, default, out imageIndex);

        if (acquire == VkResult.ErrorOutOfDateKHR)
        {
            return false;
        }

        if (acquire != VkResult.SuboptimalKHR)
        {
            Check(acquire, "vkAcquireNextImageKHR");
        }

        Check(api.vkResetFences(1, &fence), "vkResetFences");
        Check(api.vkResetCommandPool(frame.Pool, 0), "vkResetCommandPool");
        commandBuffer = frame.Command;
        VkCommandBufferBeginInfo begin = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };
        Check(api.vkBeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer");
        recording = true;
        frame.Objects.Write<SceneObject>(Scene.Objects);
        constantIndex = 0;

        return true;
    }

    private void Bind(VkPipelineBindPoint point, VkPipeline pipeline, FrameConstants constants)
    {
        VkFrame frame = slots[FrameSlot];
        uint offset = (uint)(constantIndex++ * uniformStride);

        if (constantIndex > RenderLayout.UniformSlots)
        {
            throw new InvalidOperationException("Too many uniform blocks for a frame.");
        }

        *(FrameConstants*)((byte*)frame.Constants.Mapped + offset) = constants;
        api.vkCmdBindPipeline(commandBuffer, point, pipeline);
        VkDescriptorSet descriptors = frame.Descriptors;
        api.vkCmdBindDescriptorSets(commandBuffer, point, pipelineLayout, 0, 1, &descriptors, 1, &offset);
    }

    private void Viewport(int width, int height)
    {
        VkViewport viewport = new(0, 0, width, height, 0, 1);
        VkRect2D scissor = new(0, 0, (uint)width, (uint)height);
        api.vkCmdSetViewport(commandBuffer, 0, 1, &viewport);
        api.vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
    }

    protected override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[FrameSlot]);

    protected override void DrawShadow()
    {
        VkTexture shadow = (VkTexture)Image(ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        VkRenderingAttachmentInfo depth = new()
        {
            imageView = shadow.View,
            imageLayout = shadow.Layout,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            clearValue = new VkClearValue(new VkClearDepthStencilValue(1, 0))
        };
        VkRenderingInfo rendering = new()
        {
            renderArea = new(0, 0, (uint)shadow.Width, (uint)shadow.Height),
            layerCount = 1,
            pDepthAttachment = &depth
        };
        api.vkCmdBeginRendering(commandBuffer, &rendering);
        Bind(VkPipelineBindPoint.Graphics, shadowPipeline, Constants);
        Viewport(shadow.Width, shadow.Height);
        api.vkCmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.vkCmdEndRendering(commandBuffer);
    }

    protected override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        VkRenderingAttachmentInfo* colors = stackalloc VkRenderingAttachmentInfo[colorTargets.Length];

        for (int i = 0; i < colorTargets.Length; i++)
        {
            VkTexture image = (VkTexture)Image(colorTargets[i]);
            Transition(image, ImageUse.ColorAttachment);
            colors[i] = new()
            {
                imageView = image.View,
                imageLayout = image.Layout,
                loadOp = VkAttachmentLoadOp.Clear,
                storeOp = VkAttachmentStoreOp.Store
            };
        }

        VkTexture depth = (VkTexture)Image(ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        VkRenderingAttachmentInfo depthAttachment = new()
        {
            imageView = depth.View,
            imageLayout = depth.Layout,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            clearValue = new VkClearValue(new VkClearDepthStencilValue(0, 0))
        };
        VkRenderingInfo rendering = new()
        {
            renderArea = new(0, 0, (uint)InputWidth, (uint)InputHeight),
            layerCount = 1,
            pDepthAttachment = &depthAttachment
        };
        api.vkCmdBeginRendering(commandBuffer, &rendering);
        Bind(VkPipelineBindPoint.Graphics, depthPipeline, Constants);
        Viewport(InputWidth, InputHeight);
        api.vkCmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.vkCmdEndRendering(commandBuffer);

        // Make prepass depth writes visible to the next rendering scope's tests.
        Barrier(depth.Texture, depth.Layout, depth.Layout, Range(depth.Format));
        depthAttachment.loadOp = VkAttachmentLoadOp.Load;
        rendering.colorAttachmentCount = (uint)colorTargets.Length;
        rendering.pColorAttachments = colors;
        api.vkCmdBeginRendering(commandBuffer, &rendering);
        FrameConstants colorConstants = Constants;
        colorConstants.Parameters.W = Scene.Vertices.Length / 3;
        Bind(VkPipelineBindPoint.Graphics, scenePipeline, colorConstants);
        api.vkCmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.vkCmdEndRendering(commandBuffer);
    }

    protected override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(VkPipelineBindPoint.Compute, pipelines[pass], constants);
        api.vkCmdDispatch(commandBuffer, (uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    protected override void DrawUI(ImDrawDataPtr data)
    {
        VkFrame frame = slots[FrameSlot];

        void Ensure(ref VkBufferResource? buffer, ulong size, VkBufferUsageFlags usage)
        {
            if (buffer is not null && buffer.Size >= size)
            {
                return;
            }

            buffer?.Dispose();
            buffer = CreateBuffer(Math.Max(4096, size * 2), usage, true);
        }

        Ensure(ref frame.Vertices, (ulong)(data.TotalVtxCount * sizeof(ImDrawVert)), VkBufferUsageFlags.VertexBuffer);
        Ensure(ref frame.Indices, (ulong)(data.TotalIdxCount * sizeof(ushort)), VkBufferUsageFlags.IndexBuffer);
        int vertexOffset = 0, indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];
            frame.Vertices!.Write(new ReadOnlySpan<ImDrawVert>((void*)list.VtxBuffer.Data, list.VtxBuffer.Size), vertexOffset * sizeof(ImDrawVert));
            frame.Indices!.Write(new ReadOnlySpan<ushort>((void*)list.IdxBuffer.Data, list.IdxBuffer.Size), indexOffset * sizeof(ushort));
            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        VkTexture image = (VkTexture)Image(ImageSlot.UI);
        Transition(image, ImageUse.ColorAttachment);
        VkRenderingAttachmentInfo attachment = new()
        {
            imageView = image.View,
            imageLayout = image.Layout,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store
        };
        VkRenderingInfo rendering = new()
        {
            renderArea = new(0, 0, (uint)Window.Width, (uint)Window.Height),
            layerCount = 1,
            colorAttachmentCount = 1,
            pColorAttachments = &attachment
        };
        api.vkCmdBeginRendering(commandBuffer, &rendering);
        Bind(VkPipelineBindPoint.Graphics, uiPipeline, Constants);
        Viewport(Window.Width, Window.Height);
        VkBuffer vertexBuffer = frame.Vertices!.Buffer;
        ulong offset = 0;
        api.vkCmdBindVertexBuffers(commandBuffer, 0, 1, &vertexBuffer, &offset);
        api.vkCmdBindIndexBuffer(commandBuffer, frame.Indices!.Buffer, 0, VkIndexType.Uint16);
        vertexOffset = 0;
        indexOffset = 0;

        for (int i = 0; i < data.CmdListsCount; i++)
        {
            ImDrawListPtr list = data.CmdLists[i];

            for (int c = 0; c < list.CmdBuffer.Size; c++)
            {
                ImDrawCmdPtr draw = list.CmdBuffer[c];

                if (draw.UserCallback != 0)
                {
                    throw new NotSupportedException("Unexpected UI draw callback.");
                }

                Vector4 clip = draw.ClipRect;
                int left = Math.Max(0, (int)clip.X), top = Math.Max(0, (int)clip.Y), right = Math.Min(Window.Width, (int)clip.Z), bottom = Math.Min(Window.Height, (int)clip.W);

                if (right <= left || bottom <= top)
                {
                    continue;
                }

                VkRect2D scissor = new(left, top, (uint)(right - left), (uint)(bottom - top));
                api.vkCmdSetScissor(commandBuffer, 0, 1, &scissor);
                api.vkCmdDrawIndexed(commandBuffer, draw.ElemCount, 1, (uint)indexOffset + draw.IdxOffset, vertexOffset + (int)draw.VtxOffset, 0);
            }

            vertexOffset += list.VtxBuffer.Size;
            indexOffset += list.IdxBuffer.Size;
        }

        api.vkCmdEndRendering(commandBuffer);
    }

    protected override void Transition(GpuImage image, ImageUse use)
    {
        VkTexture texture = (VkTexture)image;
        VkImageLayout layout = use switch
        {
            ImageUse.Storage => VkImageLayout.General,
            ImageUse.ColorAttachment => VkImageLayout.ColorAttachmentOptimal,
            ImageUse.DepthAttachment => VkImageLayout.DepthStencilAttachmentOptimal,
            ImageUse.CopySource => VkImageLayout.TransferSrcOptimal,
            ImageUse.CopyDestination => VkImageLayout.TransferDstOptimal,
            _ => VkImageLayout.ShaderReadOnlyOptimal
        };

        if (texture.Layout != layout)
        {
            Barrier(texture.Texture, texture.Layout, layout, Range(texture.Format, texture.Layers));
        }

        texture.Layout = layout;
    }

    private void Barrier(VkImage image, VkImageLayout oldLayout, VkImageLayout newLayout, VkImageSubresourceRange range)
    {
        VkImageMemoryBarrier2 barrier = new()
        {
            srcStageMask = oldLayout == VkImageLayout.Undefined ? VkPipelineStageFlags2.None : VkPipelineStageFlags2.AllCommands,
            srcAccessMask = oldLayout == VkImageLayout.Undefined ? VkAccessFlags2.None : VkAccessFlags2.MemoryRead | VkAccessFlags2.MemoryWrite,
            dstStageMask = VkPipelineStageFlags2.AllCommands,
            dstAccessMask = VkAccessFlags2.MemoryRead | VkAccessFlags2.MemoryWrite,
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
            image = image,
            subresourceRange = range
        };
        VkDependencyInfo dependency = new()
        {
            imageMemoryBarrierCount = 1,
            pImageMemoryBarriers = &barrier
        };
        api.vkCmdPipelineBarrier2(commandBuffer, &dependency);
    }

    protected override void SubmitFrame()
    {
        VkTexture final = (VkTexture)Image(ImageSlot.Final);
        Transition(final, ImageUse.CopySource);
        Barrier(backBuffers[imageIndex], backLayouts[imageIndex], VkImageLayout.TransferDstOptimal, Range(ImageFormat.Rgba8));

        // Blit performs the RGBA/BGRA conversion when the surface only exposes BGRA.
        VkImageBlit blit = new()
        {
            srcSubresource = new(VkImageAspectFlags.Color, 0, 0, 1),
            dstSubresource = new(VkImageAspectFlags.Color, 0, 0, 1)
        };
        blit.srcOffsets[1] = new(Window.Width, Window.Height, 1);
        blit.dstOffsets[1] = new(Window.Width, Window.Height, 1);
        api.vkCmdBlitImage(commandBuffer, final.Texture, VkImageLayout.TransferSrcOptimal, backBuffers[imageIndex], VkImageLayout.TransferDstOptimal, 1, &blit, VkFilter.Nearest);
        Barrier(backBuffers[imageIndex], VkImageLayout.TransferDstOptimal, VkImageLayout.PresentSrcKHR, Range(ImageFormat.Rgba8));
        backLayouts[imageIndex] = VkImageLayout.PresentSrcKHR;
        Check(api.vkEndCommandBuffer(commandBuffer), "vkEndCommandBuffer");
        recording = false;
        VkFrame frame = slots[FrameSlot];
        VkSemaphore acquire = frame.Acquire, present = presentSemaphores[imageIndex];
        VkPipelineStageFlags waitStage = VkPipelineStageFlags.AllCommands;
        VkCommandBuffer command = commandBuffer;
        VkSubmitInfo submit = new()
        {
            waitSemaphoreCount = 1,
            pWaitSemaphores = &acquire,
            pWaitDstStageMask = &waitStage,
            commandBufferCount = 1,
            pCommandBuffers = &command,
            signalSemaphoreCount = 1,
            pSignalSemaphores = &present
        };
        Check(api.vkQueueSubmit(queue, 1, &submit, frame.Fence), "vkQueueSubmit(frame)");
    }

    protected override bool Present()
    {
        VkSemaphore semaphore = presentSemaphores[imageIndex];
        VkSwapchainKHR swap = swapChain;
        uint index = imageIndex;
        VkPresentInfoKHR present = new()
        {
            waitSemaphoreCount = 1,
            pWaitSemaphores = &semaphore,
            swapchainCount = 1,
            pSwapchains = &swap,
            pImageIndices = &index
        };
        VkResult result = api.vkQueuePresentKHR(queue, &present);

        if (result is VkResult.ErrorOutOfDateKHR or VkResult.SuboptimalKHR)
        {
            return false;
        }

        Check(result, "vkQueuePresentKHR");

        return true;
    }

    protected override void FinishFrame()
    {
    }

    protected override void WaitIdle()
    {
        if (api is not null)
        {
            Check(api.vkDeviceWaitIdle(), "vkDeviceWaitIdle");
        }
    }

    protected override void DisposeDevice()
    {
        if (api is not null)
        {
            if (recording)
            {
                api.vkEndCommandBuffer(commandBuffer);
            }

            DestroySwapChain();

            foreach (VkPipeline pipeline in pipelines.Values)
            {
                api.vkDestroyPipeline(pipeline);
            }

            if (!shadowPipeline.IsNull)
            {
                api.vkDestroyPipeline(shadowPipeline);
            }

            if (!scenePipeline.IsNull)
            {
                api.vkDestroyPipeline(scenePipeline);
            }

            if (!depthPipeline.IsNull)
            {
                api.vkDestroyPipeline(depthPipeline);
            }

            if (!uiPipeline.IsNull)
            {
                api.vkDestroyPipeline(uiPipeline);
            }

            if (!pipelineLayout.IsNull)
            {
                api.vkDestroyPipelineLayout(pipelineLayout);
            }

            if (!descriptorPool.IsNull)
            {
                api.vkDestroyDescriptorPool(descriptorPool);
            }

            if (!descriptorLayout.IsNull)
            {
                api.vkDestroyDescriptorSetLayout(descriptorLayout);
            }

            if (!sampler.IsNull)
            {
                api.vkDestroySampler(sampler);
            }

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
            api.vkDestroyDevice();
        }

        if (instanceApi is not null)
        {
            if (!surface.IsNull)
            {
                instanceApi.vkDestroySurfaceKHR(surface);
            }

            instanceApi.vkDestroyInstance();
        }
        // Vortice owns the module reference acquired by vkInitialize for process lifetime.
    }
}
