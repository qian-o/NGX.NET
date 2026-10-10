namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXCoordinatesNative(in NGXCoordinates value)
{
    [FieldOffset(0)]
    public uint X = value.X;

    [FieldOffset(4)]
    public uint Y = value.Y;
}
