using System.Numerics;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;

namespace Showcase;

internal sealed partial class NGXSession
{
    public bool Generate(nint command, GpuImage[] images, GpuImage output, CameraHandler camera, bool reset)
    {
        if (generation.IsNull)
        {
            Ngx.Parameter.Reset(frameParameters);
            NativeImage color = images[(int)ImageSlot.Final].Describe();
            NGXDLSSGCreateParams create = new()
            {
                Width = (uint)outputWidth,
                Height = (uint)outputHeight,
                RenderWidth = (uint)inputWidth,
                RenderHeight = (uint)inputHeight,
                NativeBackbufferFormat = isVulkan ? (uint)color.Vulkan.Resource.ImageViewInfo!.Value.Format : (uint)Silk.NET.DXGI.Format.FormatR8G8B8A8Unorm
            };

            NGXHandle created = default;
            Ngx.ThrowIfFailed(isVulkan ? Ngx.Vulkan.CreateDLSSG(command, 1, 1, out created, frameParameters, in create) : Ngx.D3D12.CreateDLSSG(command, 1, 1, out created, frameParameters, in create));
            generation = created;
            reset = true;
        }

        Matrix4x4.Invert(camera.Projection, out Matrix4x4 inverseProjection);
        Matrix4x4.Invert(camera.ViewProjection, out Matrix4x4 inverseViewProjection);
        Matrix4x4 clipToPrevious = inverseViewProjection * camera.PreviousViewProjection;
        Matrix4x4.Invert(clipToPrevious, out Matrix4x4 previousToClip);
        Vector3 forward = camera.Forward;
        Vector3 right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));
        Vector3 up = Vector3.Cross(right, forward);
        NGXDLSSGOptEvalParams options = new()
        {
            CameraViewToClip = camera.Projection,
            ClipToCameraView = inverseProjection,
            ClipToLensClip = Matrix4x4.Identity,
            ClipToPrevClip = clipToPrevious,
            PrevClipToClip = previousToClip,
            JitterOffset = camera.Jitter,
            MvecScale = Vector2.One,
            CameraPos = camera.Position,
            CameraUp = up,
            CameraRight = right,
            CameraFwd = forward,
            CameraNear = camera.Near,
            CameraFar = camera.Far,
            CameraFOV = CameraHandler.FieldOfView,
            CameraAspectRatio = (float)outputWidth / outputHeight,
            CameraMotionIncluded = true,
            DepthInverted = true,
            Reset = reset
        };

        NativeImage back = images[(int)ImageSlot.Final].Describe();
        NativeImage depth = images[(int)ImageSlot.Depth].Describe();
        NativeImage motion = images[(int)ImageSlot.Motion].Describe();
        NativeImage hudless = images[(int)ImageSlot.Hudless].Describe();
        NativeImage ui = images[(int)ImageSlot.UI].Describe();
        NativeImage generated = output.Describe();
        generated.Vulkan.ReadWrite = true;

        if (isVulkan)
        {
            NGXVKDLSSGEvalParams evaluate = new()
            {
                PBackbuffer = back.Vulkan,
                PDepth = depth.Vulkan,
                PMVecs = motion.Vulkan,
                PHudless = hudless.Vulkan,
                PUI = ui.Vulkan,
                POutputInterpFrame = generated.Vulkan
            };

            Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSG(command, generation, frameParameters, in evaluate, in options));
        }
        else
        {
            NGXD3D12DLSSGEvalParams evaluate = new()
            {
                PBackbuffer = back.DirectX,
                PDepth = depth.DirectX,
                PMVecs = motion.DirectX,
                PHudless = hudless.DirectX,
                PUI = ui.DirectX,
                POutputInterpFrame = generated.DirectX
            };

            Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSG(command, generation, frameParameters, in evaluate, in options));
        }

        // A reset produces a copy of the real frame; present that real frame once.
        return !reset;
    }
}
