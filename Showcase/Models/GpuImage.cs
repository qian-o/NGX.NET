using NGX.NET;

namespace Showcase.Models;

internal abstract class GpuImage : DisposableObject
{
    public required int Width;

    public required int Height;

    public required ImageFormat Format;

    public int Layers = 1;

    public abstract NativeImage Describe();
}
