using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private void InitializePipelines()
    {
        DescriptorRange1* ranges = stackalloc DescriptorRange1[2];
        ranges[0] = new()
        {
            RangeType = DescriptorRangeType.Srv,
            NumDescriptors = RenderLayout.SrvCount,
            Flags = DescriptorRangeFlags.DataVolatile,
            OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend
        };
        ranges[1] = new()
        {
            RangeType = DescriptorRangeType.Uav,
            NumDescriptors = RenderLayout.UavCount,
            Flags = DescriptorRangeFlags.DataVolatile,
            OffsetInDescriptorsFromTableStart = D3D12.DescriptorRangeOffsetAppend
        };

        RootParameter1* parameters = stackalloc RootParameter1[3];
        parameters[0] = new()
        {
            ParameterType = RootParameterType.TypeCbv,
            Descriptor = new()
            {
                Flags = RootDescriptorFlags.DataVolatile
            },
            ShaderVisibility = ShaderVisibility.All
        };
        parameters[1] = new()
        {
            ParameterType = RootParameterType.TypeDescriptorTable,
            DescriptorTable = new()
            {
                NumDescriptorRanges = 1,
                PDescriptorRanges = &ranges[0]
            },
            ShaderVisibility = ShaderVisibility.All
        };
        parameters[2] = new()
        {
            ParameterType = RootParameterType.TypeDescriptorTable,
            DescriptorTable = new()
            {
                NumDescriptorRanges = 1,
                PDescriptorRanges = &ranges[1]
            },
            ShaderVisibility = ShaderVisibility.All
        };

        StaticSamplerDesc sampler = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            MaxAnisotropy = 1,
            ComparisonFunc = ComparisonFunc.Never,
            BorderColor = StaticBorderColor.TransparentBlack,
            MinLOD = float.MinValue,
            MaxLOD = float.MaxValue,
            ShaderVisibility = ShaderVisibility.All
        };

        VersionedRootSignatureDesc description = new()
        {
            Version = D3DRootSignatureVersion.Version11,
            Desc11 = new()
            {
                NumParameters = 3,
                PParameters = parameters,
                NumStaticSamplers = 1,
                PStaticSamplers = &sampler,
                Flags = RootSignatureFlags.AllowInputAssemblerInputLayout
            }
        };

        using ComPtr<ID3D10Blob> serialized = default;
        using ComPtr<ID3D10Blob> errors = default;

        Check(d3d12.SerializeVersionedRootSignature(&description, serialized.GetAddressOf(), errors.GetAddressOf()));
        Check(device.Handle->CreateRootSignature(0, serialized.Handle->GetBufferPointer(), serialized.Handle->GetBufferSize(), SilkMarshal.GuidPtrOf<ID3D12RootSignature>(), (void**)root.GetAddressOf()));

        depthPipeline = GraphicsPipeline(GraphicsPass.Depth);
        scenePipeline = GraphicsPipeline(GraphicsPass.Scene);
        shadowPipeline = GraphicsPipeline(GraphicsPass.Shadow);
        uiPipeline = GraphicsPipeline(GraphicsPass.UI);

        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
        {
            byte[] shader = Compile(pass.ToString(), "compute");

            fixed (byte* code = shader)
            {
                ComputePipelineStateDesc compute = new()
                {
                    PRootSignature = root.Handle,
                    CS = new(code, (nuint)shader.Length)
                };

                ComPtr<ID3D12PipelineState> pipeline = default;
                Check(device.Handle->CreateComputePipelineState(&compute, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf()));
                pipelines[pass] = pipeline;
            }
        }
    }

    private ComPtr<ID3D12PipelineState> GraphicsPipeline(GraphicsPass pass)
    {
        bool ui = pass == GraphicsPass.UI;
        (string vertex, string fragment) = RenderLayout.Shaders(pass);
        byte[] vertexCode = Compile(vertex, "vertex");
        byte[] fragmentCode = Compile(fragment, "fragment");
        ReadOnlySpan<ImageSlot> targets = RenderLayout.ColorTargets(pass);
        DepthStencilopDesc stencil = new()
        {
            StencilFailOp = StencilOp.Keep,
            StencilDepthFailOp = StencilOp.Keep,
            StencilPassOp = StencilOp.Keep,
            StencilFunc = ComparisonFunc.Always
        };

        GraphicsPipelineStateDesc description = new()
        {
            PRootSignature = root.Handle,
            SampleMask = uint.MaxValue,
            SampleDesc = new(1, 0),
            RasterizerState = new()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.None,
                FrontCounterClockwise = !ui,
                DepthClipEnable = true
            },
            DepthStencilState = new()
            {
                DepthEnable = !ui,
                DepthWriteMask = pass is GraphicsPass.Depth or GraphicsPass.Shadow ? DepthWriteMask.All : DepthWriteMask.Zero,
                DepthFunc = pass switch
                {
                    GraphicsPass.Scene => ComparisonFunc.Equal,
                    GraphicsPass.Depth => ComparisonFunc.Greater,
                    _ => ComparisonFunc.LessEqual
                },
                StencilReadMask = byte.MaxValue,
                StencilWriteMask = byte.MaxValue,
                FrontFace = stencil,
                BackFace = stencil
            },
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            NumRenderTargets = (uint)targets.Length,
            DSVFormat = ui ? Format.FormatUnknown : Format.FormatD32Float
        };

        for (int i = 0; i < 8; i++)
        {
            description.BlendState.RenderTarget[i] = new()
            {
                BlendEnable = true,
                SrcBlend = Blend.One,
                DestBlend = ui ? Blend.InvSrcAlpha : Blend.Zero,
                BlendOp = BlendOp.Add,
                SrcBlendAlpha = Blend.One,
                DestBlendAlpha = ui ? Blend.InvSrcAlpha : Blend.Zero,
                BlendOpAlpha = BlendOp.Add,
                LogicOp = LogicOp.Noop,
                RenderTargetWriteMask = (byte)ColorWriteEnable.All
            };
        }

        for (int i = 0; i < targets.Length; i++)
        {
            description.RTVFormats[i] = NativeFormat(RenderLayout.Format(targets[i]));
        }

        using NativeNames semantics = new(["POSITION", "TEXCOORD", "COLOR"]);

        fixed (byte* vs = vertexCode)
        fixed (byte* ps = fragmentCode)
        {
            InputElementDesc* elements = stackalloc InputElementDesc[3];
            elements[0] = new()
            {
                SemanticName = semantics.Pointer[0],
                Format = Format.FormatR32G32Float
            };
            elements[1] = new()
            {
                SemanticName = semantics.Pointer[1],
                Format = Format.FormatR32G32Float,
                AlignedByteOffset = 8
            };
            elements[2] = new()
            {
                SemanticName = semantics.Pointer[2],
                Format = Format.FormatR8G8B8A8Unorm,
                AlignedByteOffset = 16
            };

            description.VS = new(vs, (nuint)vertexCode.Length);
            description.PS = new(ps, (nuint)fragmentCode.Length);
            description.InputLayout = new()
            {
                PInputElementDescs = ui ? elements : null,
                NumElements = ui ? 3u : 0u
            };

            ComPtr<ID3D12PipelineState> pipeline = default;
            Check(device.Handle->CreateGraphicsPipelineState(&description, SilkMarshal.GuidPtrOf<ID3D12PipelineState>(), (void**)pipeline.GetAddressOf()));

            return pipeline;
        }
    }

    private byte[] Compile(string entry, string stage) => ShaderCompiler.Compile("Scene.slang", entry, stage, false, RayQuerySupported);
}
