using Hexa.NET.ImGui;
using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Vulkan;

namespace Showcase.Vulkan;

internal sealed unsafe partial class VulkanRHI
{
    private PipelineLayout pipelineLayout;
    private Pipeline scenePipeline;
    private Pipeline depthPipeline;
    private Pipeline uiPipeline;
    private Pipeline shadowPipeline;
    private readonly Dictionary<ComputePass, Pipeline> pipelines = [];

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

        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
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
        bool ui = pass == GraphicsPass.UI;
        bool depthOnly = pass is GraphicsPass.Depth or GraphicsPass.Shadow;
        (string vertexName, string fragmentName) = RenderLayout.Shaders(pass);
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        using NativeNames names = new([vertexName, fragmentName]);
        ShaderModule vertex = Shader(vertexName, "vertex"), fragment = default;

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
}
