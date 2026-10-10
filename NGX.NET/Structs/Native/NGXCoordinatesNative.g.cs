#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXCoordinatesNative
{
    [FieldOffset(0)]
    public uint X;

    [FieldOffset(4)]
    public uint Y;

    public NGXCoordinatesNative(in NGXCoordinates value)
    {
        X = value.X;
        Y = value.Y;
    }
}
