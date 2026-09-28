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
    Albedo, Normal, Emissive, Motion, Depth, Scene, Specular, HitDistance, Reconstructed, DisplayInput, Hudless, UI, Final, Diffuse, Shadow, Exposure, Luminance, FilteredLuminance, GeometricNormal, Count
}
internal enum ImageFormat
{
    Rgba16, Rgba32, Rg16, Float, Depth, Rgba8
}
internal enum ImageUse
{
    ShaderRead, Storage, ColorAttachment, DepthAttachment, CopySource, CopyDestination
}
internal enum GraphicsPass
{
    Scene, Depth, Shadow, UI
}
internal enum ComputePass
{
    TraceLighting, Lighting, PrepareLuminance, FilterLuminance, MeterExposure, ToneMap, NativeResolve, CopyDisplay, Composite
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
    public Vector4 EnvironmentMinimum;
    public Vector4 EnvironmentMaximum;
}

internal abstract class GpuImage : IDisposable
{
    public required int Width;
    public required int Height;
    public required ImageFormat Format;
    public int Layers = 1;
    public abstract Resource Describe();
    public abstract void Dispose();
}

internal static class RenderLayout
{
    public static (string Vertex, string Fragment) Shaders(GraphicsPass pass) => pass switch
    {
        GraphicsPass.Scene => ("SceneVS", "ScenePS"),
        GraphicsPass.Depth => ("SceneVS", "DepthPS"),
        GraphicsPass.Shadow => ("ShadowVS", "ShadowPS"),
        GraphicsPass.UI => ("UiVS", "UiPS"),
        _ => throw new ArgumentOutOfRangeException(nameof(pass))
    };

    public static ReadOnlySpan<ImageSlot> ColorTargets(GraphicsPass pass) => pass switch
    {
        GraphicsPass.Scene => [ImageSlot.Albedo, ImageSlot.Normal, ImageSlot.Emissive, ImageSlot.Motion, ImageSlot.GeometricNormal],
        GraphicsPass.UI => [ImageSlot.UI],
        GraphicsPass.Depth or GraphicsPass.Shadow => [],
        _ => throw new ArgumentOutOfRangeException(nameof(pass))
    };

    // NVIDIA's DLSS integration guide, section 3.5: native bias 0, epsilon 0.
    // Native rendering keeps its ordinary footprint; temporal reconstruction needs
    // texture detail at the output resolution rather than the lower input resolution.
    public static float TextureMipBias(int inputWidth, int outputWidth, bool temporal) =>
        temporal ? MathF.Log2((float)inputWidth / outputWidth) - 1 : 0;

    public const int FramesInFlight = 3;
    public const int PreviousExposureSrv = 6 + (int)ImageSlot.Count;
    public const int FontSrv = PreviousExposureSrv + 1;
    public const int LightingSamplesSrv = FontSrv + 1;
    public const int SrvCount = LightingSamplesSrv + 1;
    public const int PrimarySamples = 1; // PathTracing.slang; strata advance across frames
    public const int LightingPaths = PrimarySamples * 2; // diffuse and specular
    public const int ShadowMapSize = 2048;
    public const int LuminanceTileSize = 16;
    public const int LightingSamplesUav = 12;
    public const int UavCount = LightingSamplesUav + 1;
    public const int UniformStride = 512;
    public const int UniformSlots = 16;
    public static readonly ImageSlot[] GeometryOutputs = [ImageSlot.Albedo, ImageSlot.Normal, ImageSlot.Emissive, ImageSlot.Depth, ImageSlot.GeometricNormal];
    public static readonly ImageSlot[] LightingOutputs = [ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Motion, ImageSlot.Diffuse];
    public static readonly ImageSlot[] StorageImages = [ImageSlot.Scene, ImageSlot.Specular, ImageSlot.HitDistance, ImageSlot.Reconstructed, ImageSlot.DisplayInput, ImageSlot.Hudless, ImageSlot.Final, ImageSlot.Motion, ImageSlot.Diffuse, ImageSlot.Exposure, ImageSlot.Luminance, ImageSlot.FilteredLuminance];
    public static (int Width, int Height) Size(ImageSlot slot, int inputWidth, int inputHeight, int outputWidth, int outputHeight) => slot switch
    {
        ImageSlot.Exposure => (1, 1),
        ImageSlot.Shadow => (ShadowMapSize, ShadowMapSize),
        ImageSlot.Luminance or ImageSlot.FilteredLuminance =>
            ((outputWidth + LuminanceTileSize - 1) / LuminanceTileSize, (outputHeight + LuminanceTileSize - 1) / LuminanceTileSize),
        ImageSlot.Reconstructed or ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => (outputWidth, outputHeight),
        _ => (inputWidth, inputHeight)
    };

    public static ImageFormat Format(ImageSlot slot) => slot switch
    {
        ImageSlot.Motion or ImageSlot.GeometricNormal => ImageFormat.Rg16,
        ImageSlot.Exposure => ImageFormat.Rgba32,
        ImageSlot.Depth or ImageSlot.Shadow => ImageFormat.Depth,
        ImageSlot.HitDistance or ImageSlot.FilteredLuminance => ImageFormat.Float,
        ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => ImageFormat.Rgba8,
        _ => ImageFormat.Rgba16
    };
}
