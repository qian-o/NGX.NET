#nullable enable

namespace NGX.NET;

public struct NGXCoordinatesVK
{
    public uint X;

    public uint Y;

    internal unsafe NGXCoordinatesVK(in NGXCoordinatesVKNative native)
    {
        X = native.X;
        Y = native.Y;
    }
}
