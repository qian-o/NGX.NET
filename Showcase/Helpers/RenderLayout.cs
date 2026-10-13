using Showcase.Models;

namespace Showcase.Helpers;

internal static class RenderLayout
{
    public const int FramesInFlight = 3;

    public const int FontSrv = 7 + (int)ImageSlot.Count;

    public const int AtmosphereSrv = FontSrv + 1;

    public const int SrvCount = AtmosphereSrv + (int)AtmosphereImage.Count;

    public const int FilmUav = 11;

    public const int ExposureUav = FilmUav + 1;

    public const int AtmosphereUav = ExposureUav + 1;

    public const int UavCount = AtmosphereUav + (int)AtmosphereImage.Count;

    public const uint VulkanSamplerBinding = 64;

    public const int UniformStride = 512;

    public const int UniformSlots = 16;

    public static readonly ComputePass[] ComputePasses = Enum.GetValues<ComputePass>();

    public static readonly ImageSlot[] PathOutputs = [ImageSlot.Scene, ImageSlot.Normal, ImageSlot.Depth, ImageSlot.Motion, ImageSlot.Diffuse, ImageSlot.Specular, ImageSlot.SpecularHitDistance];

    public static readonly ImageSlot[] StorageImages = [ImageSlot.Scene, ImageSlot.Specular, ImageSlot.SpecularHitDistance, ImageSlot.Reconstructed, ImageSlot.DisplayInput, ImageSlot.Hudless, ImageSlot.Final, ImageSlot.Motion, ImageSlot.Diffuse, ImageSlot.Normal, ImageSlot.Depth];

    public static (int Width, int Height) AtmosphereSize(AtmosphereImage image)
    {
        return image switch
        {
            AtmosphereImage.Transmittance => (256, 64),
            AtmosphereImage.MultipleScattering => (32, 32),
            AtmosphereImage.SkyView => (256, 128),
            AtmosphereImage.SkyDistribution => (257, 129),
            AtmosphereImage.GroundIrradiance => (2, 1),
            // Sixteen depth slices stacked vertically in a 2D texture.
            AtmosphereImage.AerialScattering or AtmosphereImage.AerialTransmittance => (32, 32 * 16),
            _ => throw new ArgumentOutOfRangeException(nameof(image))
        };
    }

    public static (int Width, int Height) Size(ImageSlot slot, int inputWidth, int inputHeight, int outputWidth, int outputHeight)
    {
        return slot is ImageSlot.Reconstructed or ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final ? (outputWidth, outputHeight) : (inputWidth, inputHeight);
    }

    public static ImageFormat Format(ImageSlot slot)
    {
        return slot switch
        {
            ImageSlot.Motion => ImageFormat.Rg16,
            ImageSlot.Scene or ImageSlot.Reconstructed => ImageFormat.Rgba32,
            ImageSlot.Depth or ImageSlot.SpecularHitDistance => ImageFormat.Float,
            ImageSlot.DisplayInput or ImageSlot.Hudless or ImageSlot.UI or ImageSlot.Final => ImageFormat.Rgba8,
            _ => ImageFormat.Rgba16
        };
    }
}
