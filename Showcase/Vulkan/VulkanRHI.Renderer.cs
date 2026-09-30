using System.Numerics;
using ImGuiNET;
using Showcase.Handlers;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;
using Buffer = Silk.NET.Vulkan.Buffer;
using Semaphore = Silk.NET.Vulkan.Semaphore;

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
                Device = device,
                Constants = CreateBuffer((ulong)(uniformStride * RenderLayout.UniformSlots), BufferUsageFlags.UniformBufferBit, true),
                Objects = CreateBuffer((ulong)(Scene.Objects.Length * sizeof(SceneObject)), BufferUsageFlags.StorageBufferBit, true)
            };

            slots[i] = frame;
            CommandPoolCreateInfo pool = new()
            {
                SType = StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = queueFamily
            };

            Check(api.CreateCommandPool(device, &pool, null, out frame.Pool), "vkCreateCommandPool");
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

            Check(api.CreateFence(device, &fence, null, out frame.Fence), "vkCreateFence");
            SemaphoreCreateInfo semaphore = new() { SType = StructureType.SemaphoreCreateInfo };
            Check(api.CreateSemaphore(device, &semaphore, null, out frame.RenderComplete), "vkCreateSemaphore(render complete)");
        }

        commandBuffer = slots[0].Command;
        CommandBufferBeginInfo begin = new()
        {
            SType = StructureType.CommandBufferBeginInfo,
            Flags = CommandBufferUsageFlags.OneTimeSubmitBit
        };

        Check(api.BeginCommandBuffer(commandBuffer, &begin), "vkBeginCommandBuffer(upload)");
        recording = true;
        sceneBuffers[0] = StaticBuffer<SceneVertex>(Scene.Vertices, rayGeometry: true);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Scene.TextureInfo);
        font = (VkTexture)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        VkBufferResource fontUpload = CreateBuffer((ulong)UI.FontPixels.Length, BufferUsageFlags.TransferSrcBit, true);
        fontUpload.Write<byte>(UI.FontPixels);
        uploads.Add(fontUpload);
        Transition(font, ImageUse.CopyDestination);
        BufferImageCopy copy = new()
        {
            ImageSubresource = new(ImageAspectFlags.ColorBit, 0, 0, 1),
            ImageExtent = new((uint)UI.FontWidth, (uint)UI.FontHeight, 1)
        };

        api.CmdCopyBufferToImage(commandBuffer, fontUpload.Buffer, font.Texture, ImageLayout.TransferDstOptimal, 1, &copy);
        Transition(font, ImageUse.ShaderRead);
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
            if (i == 5 && !RayQuerySupported)
            {
                continue;
            }

            bindings.Add(new()
            {
                Binding = i + 1,
                DescriptorType = i < 5 ? DescriptorType.StorageBuffer : i == 5 ? DescriptorType.AccelerationStructureKhr : DescriptorType.SampledImage,
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

            Check(api.CreateDescriptorSetLayout(device, &create, null, out descriptorLayout), "vkCreateDescriptorSetLayout");
        }

        List<DescriptorPoolSize> sizes =
        [
            new(DescriptorType.UniformBufferDynamic, RenderLayout.FramesInFlight),
            new(DescriptorType.StorageBuffer, RenderLayout.FramesInFlight * 5),
            new(DescriptorType.SampledImage, RenderLayout.FramesInFlight * (RenderLayout.SrvCount - 6)),
            new(DescriptorType.StorageImage, RenderLayout.FramesInFlight * RenderLayout.UavCount),
            new(DescriptorType.Sampler, RenderLayout.FramesInFlight)
        ];

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

            Check(api.CreateDescriptorPool(device, &poolInfo, null, out descriptorPool), "vkCreateDescriptorPool");
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

        Check(api.CreateSampler(device, &samplerInfo, null, out sampler), "vkCreateSampler");
        PipelineLayoutCreateInfo layoutInfo = new()
        {
            SType = StructureType.PipelineLayoutCreateInfo,
            SetLayoutCount = 1,
            PSetLayouts = &setLayout
        };

        Check(api.CreatePipelineLayout(device, &layoutInfo, null, out pipelineLayout), "vkCreatePipelineLayout");
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
        {
            string entry = pass.ToString();
            ShaderModule shader = Shader(entry, "compute");
            using NativeText name = new(entry);
            ComputePipelineCreateInfo create = new()
            {
                SType = StructureType.ComputePipelineCreateInfo,
                Layout = pipelineLayout,
                Stage = new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.ComputeBit,
                    Module = shader,
                    PName = name.Pointer
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
        bool ui = pass == GraphicsPass.UI;
        bool depthOnly = pass is GraphicsPass.Depth or GraphicsPass.Shadow;
        (string vertexName, string fragmentName) = RenderLayout.Shaders(pass);
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        ShaderModule vertex = Shader(vertexName, "vertex"), fragment = Shader(fragmentName, "fragment");

        try
        {
            using NativeText vsName = new(vertexName);
            using NativeText psName = new(fragmentName);
            PipelineShaderStageCreateInfo* stages = stackalloc PipelineShaderStageCreateInfo[2]
            {
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.VertexBit,
                    Module = vertex,
                    PName = vsName.Pointer
                },
                new()
                {
                    SType = StructureType.PipelineShaderStageCreateInfo,
                    Stage = ShaderStageFlags.FragmentBit,
                    Module = fragment,
                    PName = psName.Pointer
                }
            };

            VertexInputBindingDescription binding = new()
            {
                Binding = 0,
                Stride = (uint)sizeof(ImDrawVert),
                InputRate = VertexInputRate.Vertex
            };
            VertexInputAttributeDescription* attributes = stackalloc VertexInputAttributeDescription[3]
            {
                new(0, 0, Format.R32G32Sfloat, 0),
                new(1, 0, Format.R32G32Sfloat, 8),
                new(2, 0, Format.R8G8B8A8Unorm, 16)
            };

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

            DynamicState* states = stackalloc DynamicState[2]
            {
                DynamicState.Viewport,
                DynamicState.Scissor
            };

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

    protected override void UpdateDescriptors()
    {
        foreach ((VkFrame frame, int index) in slots.Select((frame, index) => (frame, index)))
        {
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
                VkBufferResource buffer = i == 4 ? frame.Objects : sceneBuffers[i];
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
                Texture(7 + (uint)slot, DescriptorType.SampledImage, (VkTexture)Frames[index][(int)slot], ImageLayout.ShaderReadOnlyOptimal);
            }

            int previousFrame = (index + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            Texture(
                RenderLayout.PreviousExposureSrv + 1,
                DescriptorType.SampledImage,
                (VkTexture)Frames[previousFrame][(int)ImageSlot.Exposure],
                ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.FontSrv + 1, DescriptorType.SampledImage, font, ImageLayout.ShaderReadOnlyOptimal);
            Texture(RenderLayout.LightingSamplesSrv + 1, DescriptorType.SampledImage, (VkTexture)LightingSamples, ImageLayout.ShaderReadOnlyOptimal);

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                Texture(32 + (uint)i, DescriptorType.StorageImage, (VkTexture)Frames[index][(int)RenderLayout.StorageImages[i]], ImageLayout.General);
            }

            Texture(32 + RenderLayout.LightingSamplesUav, DescriptorType.StorageImage, (VkTexture)LightingSamples, ImageLayout.General);
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

    protected override bool BeginCommands()
    {
        VkFrame frame = slots[FrameSlot];
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
        frame.Objects.Write<SceneObject>(Scene.Objects);
        constantIndex = 0;

        return true;
    }

    private void Bind(PipelineBindPoint point, Pipeline pipeline, FrameConstants constants)
    {
        VkFrame frame = slots[FrameSlot];
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

    protected override void UpdateRayTracingScene() => UpdateAccelerationStructure(slots[FrameSlot]);

    protected override void DrawShadow()
    {
        VkTexture shadow = (VkTexture)Image(ImageSlot.Shadow);
        Transition(shadow, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depth = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = shadow.View,
            ImageLayout = shadow.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new() { DepthStencil = new(1, 0) }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)shadow.Width, (uint)shadow.Height)),
            LayerCount = 1,
            PDepthAttachment = &depth
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, shadowPipeline, Constants);
        Viewport(shadow.Width, shadow.Height);
        api.CmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }

    protected override void DrawScene()
    {
        ReadOnlySpan<ImageSlot> colorTargets = RenderLayout.ColorTargets(GraphicsPass.Scene);
        RenderingAttachmentInfo* colors = stackalloc RenderingAttachmentInfo[colorTargets.Length];

        for (int i = 0; i < colorTargets.Length; i++)
        {
            VkTexture image = (VkTexture)Image(colorTargets[i]);
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

        VkTexture depth = (VkTexture)Image(ImageSlot.Depth);
        Transition(depth, ImageUse.DepthAttachment);
        RenderingAttachmentInfo depthAttachment = new()
        {
            SType = StructureType.RenderingAttachmentInfo,
            ImageView = depth.View,
            ImageLayout = depth.Layout,
            LoadOp = AttachmentLoadOp.Clear,
            StoreOp = AttachmentStoreOp.Store,
            ClearValue = new() { DepthStencil = new(0, 0) }
        };

        RenderingInfo rendering = new()
        {
            SType = StructureType.RenderingInfo,
            RenderArea = new(new(0, 0), new((uint)InputWidth, (uint)InputHeight)),
            LayerCount = 1,
            PDepthAttachment = &depthAttachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, depthPipeline, Constants);
        Viewport(InputWidth, InputHeight);
        api.CmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);

        // Make prepass depth writes visible to the next rendering scope's tests.
        Barrier(depth.Texture, depth.Layout, depth.Layout, Range(depth.Format));
        depthAttachment.LoadOp = AttachmentLoadOp.Load;
        rendering.ColorAttachmentCount = (uint)colorTargets.Length;
        rendering.PColorAttachments = colors;
        api.CmdBeginRendering(commandBuffer, &rendering);
        FrameConstants colorConstants = Constants;
        colorConstants.Parameters.W = Scene.Vertices.Length / 3;
        Bind(PipelineBindPoint.Graphics, scenePipeline, colorConstants);
        api.CmdDraw(commandBuffer, (uint)Scene.Vertices.Length, 1, 0, 0);
        api.CmdEndRendering(commandBuffer);
    }

    protected override void Dispatch(ComputePass pass, int width, int height, in FrameConstants constants, int groupsZ = 1)
    {
        Bind(PipelineBindPoint.Compute, pipelines[pass], constants);
        api.CmdDispatch(commandBuffer, (uint)(width + 7) / 8, (uint)(height + 7) / 8, (uint)groupsZ);
    }

    protected override void DrawUI(ImDrawDataPtr data)
    {
        VkFrame frame = slots[FrameSlot];

        void Ensure(ref VkBufferResource? buffer, ulong size, BufferUsageFlags usage)
        {
            if (buffer is not null && buffer.Size >= size)
            {
                return;
            }

            buffer?.Dispose();
            buffer = CreateBuffer(Math.Max(4096, size * 2), usage, true);
        }

        Ensure(ref frame.Vertices, (ulong)(data.TotalVtxCount * sizeof(ImDrawVert)), BufferUsageFlags.VertexBufferBit);
        Ensure(ref frame.Indices, (ulong)(data.TotalIdxCount * sizeof(ushort)), BufferUsageFlags.IndexBufferBit);
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
            RenderArea = new(new(0, 0), new((uint)Window.Width, (uint)Window.Height)),
            LayerCount = 1,
            ColorAttachmentCount = 1,
            PColorAttachments = &attachment
        };

        api.CmdBeginRendering(commandBuffer, &rendering);
        Bind(PipelineBindPoint.Graphics, uiPipeline, Constants);
        Viewport(Window.Width, Window.Height);
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
                ImDrawCmdPtr draw = list.CmdBuffer[c];

                if (draw.UserCallback != 0)
                {
                    throw new NotSupportedException("Unexpected UI draw callback.");
                }

                Vector4 clip = draw.ClipRect;
                int left = Math.Max(0, (int)clip.X),
                    top = Math.Max(0, (int)clip.Y),
                    right = Math.Min(Window.Width, (int)clip.Z),
                    bottom = Math.Min(Window.Height, (int)clip.W);

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

    protected override void Transition(GpuImage image, ImageUse use)
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

    private void Barrier(Image image, ImageLayout oldLayout, ImageLayout newLayout, ImageSubresourceRange range, CommandBuffer target = default)
    {
        ImageMemoryBarrier2 barrier = new()
        {
            SType = StructureType.ImageMemoryBarrier2,
            SrcStageMask = oldLayout == ImageLayout.Undefined ? PipelineStageFlags2.None : PipelineStageFlags2.AllCommandsBit,
            SrcAccessMask = oldLayout == ImageLayout.Undefined ? AccessFlags2.None : AccessFlags2.MemoryReadBit | AccessFlags2.MemoryWriteBit,
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

        api.CmdPipelineBarrier2(target.Handle == 0 ? commandBuffer : target, &dependency);
    }

    protected override void SubmitFrame()
    {
        Check(api.EndCommandBuffer(commandBuffer), "vkEndCommandBuffer");
        recording = false;
        CommandBuffer command = commandBuffer;
        Semaphore complete = slots[FrameSlot].RenderComplete;
        SubmitInfo submit = new()
        {
            SType = StructureType.SubmitInfo,
            CommandBufferCount = 1,
            PCommandBuffers = &command,
            SignalSemaphoreCount = 1,
            PSignalSemaphores = &complete
        };

        lock (queueSync)
        {
            Check(api.QueueSubmit(queue, 1, &submit, slots[FrameSlot].Fence), "vkQueueSubmit(frame)");
        }
    }

    protected override void FinishFrame()
    {
    }

    protected override void WaitIdle()
    {
        if (device.Handle != 0)
        {
            Check(api.DeviceWaitIdle(device), "vkDeviceWaitIdle");
        }
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

            if (shadowPipeline.Handle != 0)
            {
                api.DestroyPipeline(device, shadowPipeline, null);
            }

            if (scenePipeline.Handle != 0)
            {
                api.DestroyPipeline(device, scenePipeline, null);
            }

            if (depthPipeline.Handle != 0)
            {
                api.DestroyPipeline(device, depthPipeline, null);
            }

            if (uiPipeline.Handle != 0)
            {
                api.DestroyPipeline(device, uiPipeline, null);
            }

            if (pipelineLayout.Handle != 0)
            {
                api.DestroyPipelineLayout(device, pipelineLayout, null);
            }

            if (descriptorPool.Handle != 0)
            {
                api.DestroyDescriptorPool(device, descriptorPool, null);
            }

            if (descriptorLayout.Handle != 0)
            {
                api.DestroyDescriptorSetLayout(device, descriptorLayout, null);
            }

            if (sampler.Handle != 0)
            {
                api.DestroySampler(device, sampler, null);
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
