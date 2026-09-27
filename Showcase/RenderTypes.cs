using System.Numerics;
using System.Runtime.InteropServices;
using Streamline.NET;

namespace Showcase;

internal enum Reconstruction
{
    Native, DLSS, RayReconstruction
}
internal enum ImageSlot
{
    Albedo, Normal, Emissive, Motion, Depth, Scene, Specular, HitDistance, Reconstructed, DisplayInput, Hudless, UI, Final, Diffuse, Shadow, Exposure, Count
}
internal enum ImageFormat
{
    Rgba16, Rg16, Float, Depth, Rgba8
}
internal enum ImageUse
{
    ShaderRead, Storage, ColorAttachment, DepthAttachment, CopySource, CopyDestination, Present
}
internal enum ComputePass
{
    Lighting, MeterExposure, ToneMap, NativeResolve, CopyDisplay, Composite
}

internal readonly record struct RenderCapabilities(bool Dlss, bool RayReconstruction, bool FrameGeneration);

internal sealed record RenderSettings
{
    public DLSSMode Quality = DLSSMode.MaxQuality;
    public bool RayReconstruction;
    public bool FrameGeneration;

    // RR selects reconstruction only; hardware ray tracing is a renderer capability
    // and continues when this setting is off. RR without upscaling runs natively.
    public Reconstruction Reconstruction => RayReconstruction ? Reconstruction.RayReconstruction :
        Quality == DLSSMode.Off ? Reconstruction.Native : Reconstruction.DLSS;
    public DLSSMode ReconstructionQuality => Quality == DLSSMode.Off && RayReconstruction ? DLSSMode.DLAA : Quality;

    public void Reset(RenderCapabilities capabilities)
    {
        Quality = capabilities.Dlss ? DLSSMode.MaxQuality : DLSSMode.Off;
        RayReconstruction = capabilities.RayReconstruction;
        FrameGeneration = capabilities.FrameGeneration;
    }
}

[StructLayout(LayoutKind.Sequential)]
internal struct FrameConstants
{
    public Matrix4x4 ViewProjection;
    public Matrix4x4 CurrentViewProjection;
    public Matrix4x4 PreviousViewProjection;
    public Matrix4x4 InverseViewProjection;
    public Vector4 Camera;
    public Vector4 Size;
    public Vector4 Sun;
    public Vector4 Scene;
    public Vector4 Parameters;
    public Vector4 Jitter;
    public Vector4 Center;
    public Matrix4x4 SunViewProjection;
    public Vector4 Lighting;
    public Vector4 Exposure; // automatic metering enabled, delta seconds, reset history, reserved
}

internal abstract class GpuImage : IDisposable
{
    public required int Width;
    public required int Height;
    public required ImageFormat Format;
    public ImageUse Use;
    public abstract Resource Describe();
    public abstract void Dispose();
}

internal static class RenderLayout
{
    // NVIDIA's DLSS integration guide, section 3.5: native bias 0, epsilon 0.
    // Native rendering keeps its ordinary footprint; temporal reconstruction needs
    // texture detail at the output resolution rather than the lower input resolution.
    public static float TextureMipBias(int inputWidth, int outputWidth, bool temporal) =>
        temporal ? MathF.Log2((float)inputWidth / outputWidth) - 1 : 0;

    public const int FramesInFlight = 3;
    public const int PreviousExposureSrv = 6 + (int)ImageSlot.Count;
    public const int SrvCount = PreviousExposureSrv + 2; // previous exposure and font
    public const int ShadowMapSize = 2048;
    public const int UavCount = 10;
    public const int UniformStride = 512;
    public const int UniformSlots = 16;
    public static readonly ImageSlot[] StorageImages = [ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Reconstructed, ImageSlot.DisplayInput, ImageSlot.Hudless, ImageSlot.Final, ImageSlot.Motion, ImageSlot.Diffuse, ImageSlot.Exposure];
    public static ImageFormat Format(ImageSlot slot) => slot switch
    {
        ImageSlot.Motion => ImageFormat.Rg16,
        ImageSlot.Depth or ImageSlot.Shadow => ImageFormat.Depth,
        ImageSlot.HitDistance or ImageSlot.Exposure => ImageFormat.Float,
        ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => ImageFormat.Rgba8,
        _ => ImageFormat.Rgba16
    };
}
