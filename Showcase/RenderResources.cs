using NGX.NET;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal class RenderResources(RHI context, Scene scene) : DisposableObject
{
    public Scene Scene { get; } = scene;

    public GpuImage[][] Frames { get; } = new GpuImage[RenderLayout.FramesInFlight][];

    public GpuImage[] GeneratedFrames { get; } = new GpuImage[RenderLayout.FramesInFlight];

    // Fixed-size tables shared by all frame slots, ordered on the render queue.
    public GpuImage[] Atmosphere { get; } = new GpuImage[(int)AtmosphereImage.Count];

    // Path tracing uses one queue; a same-state UAV barrier orders Film between frames.
    public GpuImage Film { get; private set; } = null!;

    // One display exposure history, ordered on the rendering queue.
    public GpuImage Exposure { get; private set; } = null!;

    public int InputWidth { get; private set; }

    public int InputHeight { get; private set; }

    public int OutputWidth { get; private set; }

    public int OutputHeight { get; private set; }

    public GpuImage Image(int frame, ImageSlot slot)
    {
        return Frames[frame][(int)slot];
    }

    public GpuImage AtmosphereTexture(AtmosphereImage slot)
    {
        return Atmosphere[(int)slot];
    }

    public void Resize(int inputWidth, int inputHeight, int outputWidth, int outputHeight)
    {
        InputWidth = inputWidth;
        InputHeight = inputHeight;
        OutputWidth = outputWidth;
        OutputHeight = outputHeight;
        bool changed = false;
        for (int i = 0; i < Frames.Length; i++)
        {
            Frames[i] ??= new GpuImage[(int)ImageSlot.Count];

            for (ImageSlot slot = 0; slot < ImageSlot.Count; slot++)
            {
                (int width, int height) = RenderLayout.Size(slot, inputWidth, inputHeight, outputWidth, outputHeight);
                GpuImage? current = Frames[i][(int)slot];
                GpuImage image = ResizeImage(current, width, height, RenderLayout.Format(slot));
                changed |= !ReferenceEquals(current, image);
                Frames[i][(int)slot] = image;
            }

            GeneratedFrames[i] = ResizeImage(GeneratedFrames[i], outputWidth, outputHeight, ImageFormat.Rgba8);
        }

        GpuImage lighting = ResizeImage(Film, inputWidth, inputHeight, ImageFormat.Rgba32);
        changed |= !ReferenceEquals(Film, lighting);
        Film = lighting;

        GpuImage exposure = ResizeImage(Exposure, 1, 1, ImageFormat.Float);
        changed |= !ReferenceEquals(Exposure, exposure);
        Exposure = exposure;

        for (AtmosphereImage slot = 0; slot < AtmosphereImage.Count; slot++)
        {
            (int width, int height) = RenderLayout.AtmosphereSize(slot);
            GpuImage? current = Atmosphere[(int)slot];
            GpuImage image = ResizeImage(current, width, height, ImageFormat.Rgba32);
            changed |= !ReferenceEquals(current, image);
            Atmosphere[(int)slot] = image;
        }

        if (changed)
        {
            context.UpdateDescriptors();
        }
    }

    protected override void Destroy()
    {
        if (Atmosphere is not null)
        {
            for (int i = Atmosphere.Length - 1; i >= 0; i--)
            {
                Atmosphere[i]?.Dispose();
            }
            Array.Clear(Atmosphere);
        }

        Exposure?.Dispose();
        Exposure = null!;
        Film?.Dispose();
        Film = null!;

        if (Frames is null)
        {
            return;
        }

        for (int i = Frames.Length - 1; i >= 0; i--)
        {
            GeneratedFrames?[i]?.Dispose();
            GpuImage[]? frame = Frames[i];
            if (frame is not null)
            {
                for (int j = frame.Length - 1; j >= 0; j--)
                {
                    frame[j]?.Dispose();
                }
            }
        }

        if (GeneratedFrames is not null)
        {
            Array.Clear(GeneratedFrames);
        }

        Array.Clear(Frames);
    }

    private GpuImage ResizeImage(GpuImage? image, int width, int height, ImageFormat format, int layers = 1)
    {
        if (image is not null && image.Width == width && image.Height == height && image.Format == format && image.Layers == layers)
        {
            return image;
        }

        GpuImage replacement = context.CreateImage(width, height, format, layers);
        image?.Dispose();

        return replacement;
    }
}
