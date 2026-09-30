using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    protected override void InitializeRendererCore()
    {
        descriptors = CreateDescriptorHeap(
            DescriptorHeapType.CbvSrvUav,
            RenderLayout.FramesInFlight * DescriptorsPerFrame,
            DescriptorHeapFlags.ShaderVisible);
        renderTargets = CreateDescriptorHeap(DescriptorHeapType.Rtv, RenderLayout.FramesInFlight * (int)ImageSlot.Count);
        depthViews = CreateDescriptorHeap(DescriptorHeapType.Dsv, RenderLayout.FramesInFlight * 2);
        descriptorIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.CbvSrvUav);
        rtvIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.Rtv);
        dsvIncrement = device.Handle->GetDescriptorHandleIncrementSize(DescriptorHeapType.Dsv);

        for (int i = 0; i < slots.Length; i++)
        {
            DxFrame frame = slots[i] = new();
            frame.Allocator = CreateAllocator();
            frame.Constants = UploadBuffer(RenderLayout.UniformStride * RenderLayout.UniformSlots);
            frame.Objects = UploadBuffer(Resources.Scene.Objects.Length * sizeof(SceneObject));
        }

        commandList = CreateCommands(slots[0].Allocator);
        recording = true;

        if (RayQuerySupported)
        {
            Check(commandList.Handle->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList4>(), (void**)rayCommands.GetAddressOf()));
        }

        sceneBuffers[0] = StaticBuffer<SceneVertex>(Resources.Scene.Vertices);
        sceneBuffers[1] = StaticBuffer<SceneMaterial>(Resources.Scene.Materials);
        sceneBuffers[2] = StaticBuffer<uint>(Resources.Scene.Texels);
        sceneBuffers[3] = StaticBuffer<TextureDescription>(Resources.Scene.TextureInfo);

        if (RayQuerySupported)
        {
            InitializeAccelerationStructures();
        }

        UploadFont();
        ExecuteUploads();
        InitializePipelines();
    }

    public override void UpdateFontTexture()
    {
        DxFrame frame = slots[Frame.Slot];
        Check(frame.Allocator.Handle->Reset());
        Check(commandList.Handle->Reset(frame.Allocator.Handle, null));
        recording = true;
        UploadFont();
        ExecuteUploads();
        UpdateDescriptors();
    }

    private void UploadFont()
    {
        DxImage replacement = (DxImage)CreateImage(UI.FontWidth, UI.FontHeight, ImageFormat.Rgba8);
        font?.Dispose();
        font = replacement;
        int rowPitch = (UI.FontWidth * 4 + 255) & ~255;
        ComPtr<ID3D12Resource> fontUpload = UploadBuffer(rowPitch * UI.FontHeight);
        uploads.Add(fontUpload);
        byte* mapped = Map<byte>(fontUpload);

        for (int row = 0; row < UI.FontHeight; row++)
        {
            UI.FontPixels.AsSpan(row * UI.FontWidth * 4, UI.FontWidth * 4).CopyTo(new Span<byte>(mapped + row * rowPitch, UI.FontWidth * 4));
        }

        fontUpload.Handle->Unmap(0, null);
        Transition(font, ImageUse.CopyDestination);
        PlacedSubresourceFootprint footprint = new()
        {
            Footprint = new()
            {
                Format = Format.FormatR8G8B8A8Unorm,
                Width = (uint)UI.FontWidth,
                Height = (uint)UI.FontHeight,
                Depth = 1,
                RowPitch = (uint)rowPitch
            }
        };

        TextureCopyLocation source = new() { PResource = fontUpload.Handle, Type = TextureCopyType.PlacedFootprint, PlacedFootprint = footprint };
        TextureCopyLocation destination = new() { PResource = font.Texture.Handle, Type = TextureCopyType.SubresourceIndex };
        commandList.Handle->CopyTextureRegion(&destination, 0, 0, 0, &source, null);
        Transition(font, ImageUse.ShaderRead);
    }

    private void ExecuteUploads()
    {
        Check(commandList.Handle->Close());
        recording = false;
        Execute(queue, commandList);
        WaitIdle();

        foreach (ComPtr<ID3D12Resource> upload in uploads)
        {
            upload.Dispose();
        }

        uploads.Clear();
    }
}
