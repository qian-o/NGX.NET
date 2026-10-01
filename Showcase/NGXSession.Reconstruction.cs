using System.Numerics;
using NGX.NET;
using Showcase.Handlers;
using Showcase.Models;
using Ngx = NGX.NET.NGX;

namespace Showcase;

internal sealed unsafe partial class NGXSession
{
    public (int Width, int Height) Configure(RenderSettings value, int width, int height)
    {
        settings = value with { };
        outputWidth = width;
        outputHeight = height;
        uint renderWidth = 0, renderHeight = 0;

        if (value.Reconstruction is Reconstruction.DLSS or Reconstruction.RayReconstruction)
        {
            uint maxWidth = 0, maxHeight = 0, minWidth = 0, minHeight = 0;
            float sharpness = 0;
            NGXResult result = value.Reconstruction == Reconstruction.DLSS ? Ngx.DLSS.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness) : Ngx.DLSSD.GetOptimalSettings(capabilities, (uint)width, (uint)height, value.ReconstructionQuality, &renderWidth, &renderHeight, &maxWidth, &maxHeight, &minWidth, &minHeight, &sharpness);
            Ngx.ThrowIfFailed(result);
        }
        else
        {
            renderWidth = (uint)width;
            renderHeight = (uint)height;
        }

        inputWidth = (int)renderWidth;
        inputHeight = (int)renderHeight;

        return (inputWidth, inputHeight);
    }

    private void CreateReconstruction(nint command)
    {
        Ngx.Parameter.Reset(parameters);
        // Camera.Projection maps the near plane to 1 and the far plane to 0.
        const int flags = (int)(NGXDLSSFeatureFlags.IsHDR | NGXDLSSFeatureFlags.MVLowRes | NGXDLSSFeatureFlags.AutoExposure | NGXDLSSFeatureFlags.DepthInverted);
        NGXHandle* created = null;
        NGXResult result;

        if (settings.Reconstruction == Reconstruction.RayReconstruction)
        {
            NGXDLSSDCreateParams create = new()
            {
                InDenoiseMode = NGXDLSSDenoiseMode.DLUnified,
                InRoughnessMode = NGXDLSSRoughnessMode.Packed,
                InUseHWDepth = NGXDLSSDepthType.HW,
                InWidth = (uint)inputWidth,
                InHeight = (uint)inputHeight,
                InTargetWidth = (uint)outputWidth,
                InTargetHeight = (uint)outputHeight,
                InPerfQualityValue = settings.ReconstructionQuality,
                InFeatureCreateFlags = flags
            };

            result = isVulkan ? Ngx.Vulkan.CreateDLSSDExt1(device, command, 1, 1, &created, parameters, &create) : Ngx.D3D12.CreateDLSSDExt(command, 1, 1, &created, parameters, &create);
        }
        else
        {
            NGXDLSSCreateParams create = new()
            {
                Feature = new()
                {
                    InWidth = (uint)inputWidth,
                    InHeight = (uint)inputHeight,
                    InTargetWidth = (uint)outputWidth,
                    InTargetHeight = (uint)outputHeight,
                    InPerfQualityValue = settings.ReconstructionQuality
                },
                InFeatureCreateFlags = flags
            };

            result = isVulkan ? Ngx.Vulkan.CreateDLSSExt1(device, command, 1, 1, &created, parameters, &create) : Ngx.D3D12.CreateDLSSExt(command, 1, 1, &created, parameters, &create);
        }

        Ngx.ThrowIfFailed(result);
        reconstruction = created;
    }

    public void Evaluate(nint command, GpuImage[] images, CameraHandler camera, bool reset, float delta)
    {
        if (reconstruction == null)
        {
            CreateReconstruction(command);
        }

        NativeImage* descriptions = stackalloc NativeImage[images.Length];

        for (int i = 0; i < images.Length; i++)
        {
            descriptions[i] = images[i].Describe();
        }

        descriptions[(int)ImageSlot.Reconstructed].Vulkan.ReadWrite = true;
        EvaluateImages(command, descriptions, camera, reset, delta);
    }

    private void EvaluateImages(nint command, NativeImage* images, CameraHandler camera, bool reset, float delta)
    {
        Matrix4x4 view = camera.View, projection = camera.Projection;
        NGXDimensions dimensions = new()
        {
            Width = (uint)inputWidth,
            Height = (uint)inputHeight
        };

        if (!isVulkan)
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                NGXD3D12DLSSDEvalParams evaluate = new()
                {
                    PInColor = images[(int)ImageSlot.Scene].DirectX,
                    PInOutput = images[(int)ImageSlot.Reconstructed].DirectX,
                    PInDiffuseAlbedo = images[(int)ImageSlot.Diffuse].DirectX,
                    PInSpecularAlbedo = images[(int)ImageSlot.Specular].DirectX,
                    PInNormals = images[(int)ImageSlot.Normal].DirectX,
                    PInMotionVectorsReflections = images[(int)ImageSlot.SpecularMotion].DirectX,
                    PInWorldToViewMatrix = &view,
                    PInViewToClipMatrix = &projection,
                    PInDepth = images[(int)ImageSlot.Depth].DirectX,
                    PInMotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate));
            }
            else
            {
                NGXD3D12DLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        PInColor = images[(int)ImageSlot.Scene].DirectX,
                        PInOutput = images[(int)ImageSlot.Reconstructed].DirectX
                    },
                    PInDepth = images[(int)ImageSlot.Depth].DirectX,
                    PInMotionVectors = images[(int)ImageSlot.Motion].DirectX,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.D3D12.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate));
            }
        }
        else
        {
            if (settings.Reconstruction == Reconstruction.RayReconstruction)
            {
                NGXVKDLSSDEvalParams evaluate = new()
                {
                    PInColor = &images[(int)ImageSlot.Scene].Vulkan,
                    PInOutput = &images[(int)ImageSlot.Reconstructed].Vulkan,
                    PInDiffuseAlbedo = &images[(int)ImageSlot.Diffuse].Vulkan,
                    PInSpecularAlbedo = &images[(int)ImageSlot.Specular].Vulkan,
                    PInNormals = &images[(int)ImageSlot.Normal].Vulkan,
                    PInMotionVectorsReflections = &images[(int)ImageSlot.SpecularMotion].Vulkan,
                    PInWorldToViewMatrix = &view,
                    PInViewToClipMatrix = &projection,
                    PInDepth = &images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSDExt(command, reconstruction, parameters, &evaluate));
            }
            else
            {
                NGXVKDLSSEvalParams evaluate = new()
                {
                    Feature = new()
                    {
                        PInColor = &images[(int)ImageSlot.Scene].Vulkan,
                        PInOutput = &images[(int)ImageSlot.Reconstructed].Vulkan
                    },
                    PInDepth = &images[(int)ImageSlot.Depth].Vulkan,
                    PInMotionVectors = &images[(int)ImageSlot.Motion].Vulkan,
                    InJitterOffsetX = camera.Jitter.X,
                    InJitterOffsetY = camera.Jitter.Y,
                    InRenderSubrectDimensions = dimensions,
                    InReset = reset ? 1 : 0,
                    InMVScaleX = 1,
                    InMVScaleY = 1,
                    InPreExposure = 1,
                    InExposureScale = 1,
                    InFrameTimeDeltaInMsec = delta * 1000
                };

                Ngx.ThrowIfFailed(Ngx.Vulkan.EvaluateDLSSExt(command, reconstruction, parameters, &evaluate));
            }
        }
    }
}
