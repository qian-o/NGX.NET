#nullable enable

namespace NGX.NET;

public struct NGXDimensions
{
    public uint Width;

    public uint Height;

    internal unsafe NGXDimensions(in NGXDimensionsNative native)
    {
        Width = native.Width;
        Height = native.Height;
    }
}
