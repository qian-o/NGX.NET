using System.Numerics;
using System.Runtime.InteropServices;
using Streamline.NET;

namespace Showcase;

internal enum Reconstruction
{
    Native, DLSS, DLAA, RayReconstruction, NIS, DirectSR
}
internal enum ImageSlot
{
    Albedo, Normal, Emissive, Motion, Depth, Scene, Specular, HitDistance, Reconstructed, DisplayInput, Hudless, UI, Final, Diffuse, DepthCopy, Shadow, Count
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
    Lighting, ToneMap, NativeResolve, CopyDisplay, Composite
}

internal sealed record RenderSettings
{
    public Reconstruction Reconstruction = Reconstruction.DLSS;
    public DLSSMode Quality = DLSSMode.MaxQuality;
    public bool RayTracing;
    public uint GeneratedFrames;
    public ReflexMode Reflex = ReflexMode.LowLatency;
    public bool DeepDVC;
    public float Intensity = 0.5f;
    public float Saturation = 0.5f;
    public float Exposure;
    public float SunElevation = 50;
    public float SunAzimuth = 65;
    public float SunIntensity = 8;
    public float SkyIntensity = 0.65f;
    public float LocalLightIntensity;
    public bool ContactShadows = true;
    public bool PauseAnimation;
    public bool FixedCamera;
    public float Scale = 0.67f;
    public uint DirectSRVariant;
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
    public const int FramesInFlight = 3;
    public const int SrvCount = 23;
    public const int ShadowMapSize = 2048;
    public const int UavCount = 9;
    public const int UniformStride = 512;
    public const int UniformSlots = 16;
    public static readonly ImageSlot[] StorageImages = [ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Reconstructed, ImageSlot.DisplayInput, ImageSlot.Hudless, ImageSlot.Final, ImageSlot.Motion, ImageSlot.Diffuse];
    public static ImageFormat Format(ImageSlot slot) => slot switch
    {
        ImageSlot.Motion => ImageFormat.Rg16,
        ImageSlot.Depth or ImageSlot.Shadow => ImageFormat.Depth,
        ImageSlot.HitDistance or ImageSlot.DepthCopy => ImageFormat.Float,
        ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => ImageFormat.Rgba8,
        _ => ImageFormat.Rgba16
    };
}
