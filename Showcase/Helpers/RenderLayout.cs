using Showcase.Models;

namespace Showcase.Helpers;

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
        GraphicsPass.Scene =>
        [
            ImageSlot.Albedo,
            ImageSlot.Normal,
            ImageSlot.Emissive,
            ImageSlot.Motion,
            ImageSlot.SurfaceGeometry
        ],
        GraphicsPass.UI => [ImageSlot.UI],
        GraphicsPass.Depth or GraphicsPass.Shadow => [],
        _ => throw new ArgumentOutOfRangeException(nameof(pass))
    };

    // NVIDIA's DLSS integration guide, section 3.5: native bias 0, epsilon 0.
    // Native rendering keeps its ordinary footprint; temporal reconstruction needs
    // texture detail at the output resolution rather than the lower input resolution.
    public static float TextureMipBias(int inputWidth, int outputWidth, bool temporal) => temporal ? MathF.Log2((float)inputWidth / outputWidth) - 1 : 0;

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
    public static readonly ImageSlot[] GeometryOutputs =
    [
        ImageSlot.Albedo,
        ImageSlot.Normal,
        ImageSlot.Emissive,
        ImageSlot.Depth,
        ImageSlot.SurfaceGeometry
    ];
    public static readonly ImageSlot[] LightingOutputs =
    [
        ImageSlot.Scene,
        ImageSlot.Specular,
        ImageSlot.SpecularMotion,
        ImageSlot.Motion,
        ImageSlot.Diffuse
    ];
    public static readonly ImageSlot[] StorageImages =
    [
        ImageSlot.Scene,
        ImageSlot.Specular,
        ImageSlot.SpecularMotion,
        ImageSlot.Reconstructed,
        ImageSlot.DisplayInput,
        ImageSlot.Hudless,
        ImageSlot.Final,
        ImageSlot.Motion,
        ImageSlot.Diffuse,
        ImageSlot.Exposure,
        ImageSlot.Luminance,
        ImageSlot.FilteredLuminance
    ];

    public static (int Width, int Height) Size(ImageSlot slot, int inputWidth, int inputHeight, int outputWidth, int outputHeight) => slot switch
    {
        ImageSlot.Exposure => (1, 1),
        ImageSlot.Shadow => (ShadowMapSize, ShadowMapSize),
        ImageSlot.Luminance or ImageSlot.FilteredLuminance => ((outputWidth + LuminanceTileSize - 1) / LuminanceTileSize, (outputHeight + LuminanceTileSize - 1) / LuminanceTileSize),
        ImageSlot.Reconstructed or ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => (outputWidth, outputHeight),
        _ => (inputWidth, inputHeight)
    };

    public static ImageFormat Format(ImageSlot slot) => slot switch
    {
        ImageSlot.Motion or ImageSlot.SpecularMotion => ImageFormat.Rg16,
        ImageSlot.Exposure or ImageSlot.SurfaceGeometry => ImageFormat.Rgba32,
        ImageSlot.Depth or ImageSlot.Shadow => ImageFormat.Depth,
        ImageSlot.FilteredLuminance => ImageFormat.Float,
        ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => ImageFormat.Rgba8,
        _ => ImageFormat.Rgba16
    };
}
