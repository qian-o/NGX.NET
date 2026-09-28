using Streamline.NET;

namespace Showcase.Models;

internal abstract class GpuImage : IDisposable
{
    public required int Width;
    public required int Height;
    public required ImageFormat Format;
    public int Layers = 1;

    public abstract Resource Describe();

    public abstract void Dispose();
}
