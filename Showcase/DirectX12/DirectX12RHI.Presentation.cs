using Showcase.Helpers;
using Showcase.Models;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

namespace Showcase.DirectX12;

internal sealed unsafe partial class DirectX12RHI
{
    public override void CreateSwapChain()
    {
        SwapChainDesc1 description = new()
        {
            Width = (uint)Resources.OutputWidth,
            Height = (uint)Resources.OutputHeight,
            Format = Format.FormatR8G8B8A8Unorm,
            BufferCount = RenderLayout.FramesInFlight,
            BufferUsage = DXGI.UsageRenderTargetOutput,
            SampleDesc = new(1, 0),
            SwapEffect = SwapEffect.FlipDiscard,
            Scaling = Scaling.Stretch,
            AlphaMode = AlphaMode.Ignore
        };

        using ComPtr<IDXGISwapChain1> created = default;
        Check(factory.Handle->CreateSwapChainForHwnd((IUnknown*)presentQueue.Handle, Window.Native!.Win32!.Value.Hwnd, &description, null, null, created.GetAddressOf()));
        Check(created.Handle->QueryInterface(SilkMarshal.GuidPtrOf<IDXGISwapChain3>(), (void**)swapChain.GetAddressOf()));
        Check(factory.Handle->MakeWindowAssociation(Window.Native!.Win32!.Value.Hwnd, NoAltEnter));

        for (uint i = 0; i < RenderLayout.FramesInFlight; i++)
        {
            ComPtr<ID3D12Resource> buffer = default;
            Check(swapChain.Handle->GetBuffer(i, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**)buffer.GetAddressOf()));
            backBuffers.Add(buffer);
        }
    }

    public override void DestroySwapChain()
    {
        foreach (ComPtr<ID3D12Resource> buffer in backBuffers)
        {
            buffer.Dispose();
        }

        backBuffers.Clear();
        swapChain.Dispose();
    }

    public override void WaitRenderedFrame(int slot)
    {
        ulong value = slots[slot].Fence;

        if (fence.Handle->GetCompletedValue() < value)
        {
            Check(fence.Handle->SetEventOnCompletion(value, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }

        Check(presentQueue.Handle->Wait(fence.Handle, value));
    }

    public override bool PresentImage(GpuImage image)
    {
        WaitPresentation();
        Check(presentAllocator.Handle->Reset());
        Check(presentCommands.Handle->Reset(presentAllocator.Handle, null));
        ComPtr<ID3D12Resource> back = backBuffers[(int)swapChain.Handle->GetCurrentBackBufferIndex()];
        TransitionBarrier(presentCommands, back, ResourceStates.Present, ResourceStates.CopyDest);
        presentCommands.Handle->CopyResource(back.Handle, ((DxImage)image).Texture.Handle);
        TransitionBarrier(presentCommands, back, ResourceStates.CopyDest, ResourceStates.Present);
        Check(presentCommands.Handle->Close());
        Execute(presentQueue, presentCommands);
        // The fence protects the copy's command allocator and source texture.
        // Signal before Present so it does not wait for DXGI presentation work.
        Check(presentQueue.Handle->Signal(presentFence.Handle, ++presentFenceValue));
        Check(swapChain.Handle->Present(0, 0));

        return true;
    }

    public override void WaitPresentation()
    {
        if (presentFence.Handle != null && presentFence.Handle->GetCompletedValue() < presentFenceValue)
        {
            Check(presentFence.Handle->SetEventOnCompletion(presentFenceValue, (void*)presentEvent.SafeWaitHandle.DangerousGetHandle()));
            presentEvent.WaitOne();
        }
    }

    private void WaitFence(ulong value)
    {
        if (value == 0 || fence.Handle->GetCompletedValue() >= value)
        {
            return;
        }

        Check(fence.Handle->SetEventOnCompletion(value, (void*)fenceEvent.SafeWaitHandle.DangerousGetHandle()));
        fenceEvent.WaitOne();
    }

    public override void WaitIdle()
    {
        if (queue.Handle != null && fence.Handle != null)
        {
            Check(queue.Handle->Signal(fence.Handle, ++fenceValue));
            WaitFence(fenceValue);
        }

        if (presentQueue.Handle != null && presentFence.Handle != null)
        {
            Check(presentQueue.Handle->Signal(presentFence.Handle, ++presentFenceValue));
            WaitPresentation();
        }
    }
}
