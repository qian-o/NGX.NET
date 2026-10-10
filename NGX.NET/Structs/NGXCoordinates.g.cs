#nullable enable

namespace NGX.NET;

public struct NGXCoordinates
{
    public uint X;

    public uint Y;

    internal unsafe NGXCoordinates(in NGXCoordinatesNative native)
    {
        X = native.X;
        Y = native.Y;
    }
}
