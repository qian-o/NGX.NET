using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal class RenderResources(RHI context, Scene scene) : IDisposable
{
    public Scene Scene { get; } = scene;

    public GpuImage[][] Frames { get; } = new GpuImage[RenderLayout.FramesInFlight][];

    public GpuImage[] GeneratedFrames { get; } = new GpuImage[RenderLayout.FramesInFlight];

    // All lighting work runs on one graphics queue, so scratch storage is shared.
    public GpuImage LightingSamples { get; private set; } = null!;

    public int InputWidth { get; private set; }

    public int InputHeight { get; private set; }

    public int OutputWidth { get; private set; }

    public int OutputHeight { get; private set; }

    public GpuImage Image(int frame, ImageSlot slot)
    {
        return Frames[frame][(int)slot];
    }

    public void Resize(int inputWidth, int inputHeight, int outputWidth, int outputHeight)
    {
        InputWidth = inputWidth;
        InputHeight = inputHeight;
        OutputWidth = outputWidth;
        OutputHeight = outputHeight;
        bool changed = false;
        for (int frame = 0; frame < Frames.Length; frame++)
        {
            Frames[frame] ??= new GpuImage[(int)ImageSlot.Count];

            for (ImageSlot slot = 0; slot is < ImageSlot.Count; slot++)
            {
                (int width, int height) = RenderLayout.Size(slot, inputWidth, inputHeight, outputWidth, outputHeight);
                GpuImage? current = Frames[frame][(int)slot];
                GpuImage image = ResizeImage(current, width, height, RenderLayout.Format(slot));
                changed |= !ReferenceEquals(current, image);
                Frames[frame][(int)slot] = image;
            }

            GeneratedFrames[frame] = ResizeImage(GeneratedFrames[frame], outputWidth, outputHeight, ImageFormat.Rgba8);
        }

        GpuImage lighting = ResizeImage(LightingSamples, context.RayQuerySupported ? inputWidth : 1, context.RayQuerySupported ? inputHeight : 1, ImageFormat.Rgba32, RenderLayout.LightingPaths);
        changed |= !ReferenceEquals(LightingSamples, lighting);
        LightingSamples = lighting;

        if (changed)
        {
            context.UpdateDescriptors();
        }
    }

    public void Dispose()
    {
        LightingSamples?.Dispose();
        LightingSamples = null!;

        foreach (GpuImage[]? frame in Frames)
        {
            if (frame is not null)
            {
                foreach (GpuImage? image in frame)
                {
                    image?.Dispose();
                }
            }
        }

        Array.Clear(Frames);

        foreach (GpuImage? image in GeneratedFrames)
        {
            image?.Dispose();
        }

        Array.Clear(GeneratedFrames);
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
