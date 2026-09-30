using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    private CpuDescriptorHandle Cpu(int frame, int index) =>
        new() { Ptr = descriptors.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * DescriptorsPerFrame + index) * descriptorIncrement) };

    private GpuDescriptorHandle Gpu(int frame, int index) =>
        new() { Ptr = descriptors.Handle->GetGPUDescriptorHandleForHeapStart().Ptr + (ulong)((frame * DescriptorsPerFrame + index) * descriptorIncrement) };

    public override void UpdateDescriptors()
    {
        uint[] strides =
        [
            (uint)sizeof(SceneVertex),
            (uint)sizeof(SceneMaterial),
            sizeof(uint),
            (uint)sizeof(TextureDescription),
            (uint)sizeof(SceneObject)
        ];

        uint[] counts =
        [
            (uint)Resources.Scene.Vertices.Length,
            (uint)Resources.Scene.Materials.Length,
            (uint)Resources.Scene.Texels.Length,
            (uint)Resources.Scene.TextureInfo.Length,
            (uint)Resources.Scene.Objects.Length
        ];

        for (int frame = 0; frame < Resources.Frames.Length; frame++)
        {
            for (int i = 0; i < 5; i++)
            {
                ShaderResourceViewDesc bufferView = new()
                {
                    ViewDimension = SrvDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    Buffer = new()
                    {
                        NumElements = counts[i],
                        StructureByteStride = strides[i]
                    }
                };
                device.Handle->CreateShaderResourceView((i == 4 ? slots[frame].Objects : sceneBuffers[i]).Handle, &bufferView, Cpu(frame, i));
            }

            if (RayQuerySupported)
            {
                ShaderResourceViewDesc rayView = new()
                {
                    ViewDimension = SrvDimension.RaytracingAccelerationStructure,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    RaytracingAccelerationStructure = new()
                    {
                        Location = slots[frame].Tlas.Handle->GetGPUVirtualAddress()
                    }
                };
                device.Handle->CreateShaderResourceView(null, &rayView, Cpu(frame, 5));
            }
            else
            {
                // The raster shader variant has no acceleration-structure binding.
                ShaderResourceViewDesc rayView = new()
                {
                    ViewDimension = SrvDimension.Buffer,
                    Shader4ComponentMapping = ShaderComponentMapping,
                    Buffer = new()
                    {
                        NumElements = 1,
                        StructureByteStride = sizeof(uint)
                    }
                };
                device.Handle->CreateShaderResourceView(null, &rayView, Cpu(frame, 5));
            }

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                DxImage image = (DxImage)Resources.Frames[frame][(int)slot];
                CreateSrv(image, Cpu(frame, 6 + (int)slot));

                if (image.Format == ImageFormat.Depth)
                {
                    image.Dsv = new() { Ptr = depthViews.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * 2 + (slot == ImageSlot.Depth ? 0 : 1)) * dsvIncrement) };
                    DepthStencilViewDesc depthView = new()
                    {
                        Format = Format.FormatD32Float,
                        ViewDimension = DsvDimension.Texture2D
                    };
                    device.Handle->CreateDepthStencilView(image.Texture.Handle, &depthView, image.Dsv);
                }
                else
                {
                    image.Rtv = new() { Ptr = renderTargets.Handle->GetCPUDescriptorHandleForHeapStart().Ptr + (nuint)((frame * (int)ImageSlot.Count + (int)slot) * rtvIncrement) };
                    device.Handle->CreateRenderTargetView(image.Texture.Handle, null, image.Rtv);
                }
            }

            int previousFrame = (frame + RenderLayout.FramesInFlight - 1) % RenderLayout.FramesInFlight;
            CreateSrv((DxImage)Resources.Frames[previousFrame][(int)ImageSlot.Exposure], Cpu(frame, RenderLayout.PreviousExposureSrv));
            CreateSrv(font, Cpu(frame, RenderLayout.FontSrv));
            CreateSrv((DxImage)Resources.LightingSamples, Cpu(frame, RenderLayout.LightingSamplesSrv));

            for (int i = 0; i < RenderLayout.StorageImages.Length; i++)
            {
                DxImage image = (DxImage)Resources.Frames[frame][(int)RenderLayout.StorageImages[i]];
                UnorderedAccessViewDesc storageView = new()
                {
                    Format = NativeFormat(image.Format),
                    ViewDimension = UavDimension.Texture2D
                };
                device.Handle->CreateUnorderedAccessView(image.Texture.Handle, null, &storageView, Cpu(frame, RenderLayout.SrvCount + i));
            }

            UnorderedAccessViewDesc lightingView = new()
            {
                Format = NativeFormat(Resources.LightingSamples.Format),
                ViewDimension = UavDimension.Texture2Darray,
                Texture2DArray = new()
                {
                    ArraySize = (uint)Resources.LightingSamples.Layers
                }
            };
            device.Handle->CreateUnorderedAccessView(((DxImage)Resources.LightingSamples).Texture.Handle, null, &lightingView, Cpu(frame, RenderLayout.SrvCount + RenderLayout.LightingSamplesUav));
        }
    }

    private void CreateSrv(DxImage image, CpuDescriptorHandle descriptor)
    {
        ShaderResourceViewDesc description = new()
        {
            Format = image.Format == ImageFormat.Depth ? Format.FormatR32Float : NativeFormat(image.Format),
            ViewDimension = image.Layers > 1 ? SrvDimension.Texture2Darray : SrvDimension.Texture2D,
            Shader4ComponentMapping = ShaderComponentMapping
        };

        if (image.Layers > 1)
        {
            description.Texture2DArray = new()
            {
                MipLevels = 1,
                ArraySize = (uint)image.Layers
            };
        }
        else
        {
            description.Texture2D = new()
            {
                MipLevels = 1
            };
        }

        device.Handle->CreateShaderResourceView(image.Texture.Handle, &description, descriptor);
    }
}
