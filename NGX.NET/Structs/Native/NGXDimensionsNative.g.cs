namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXDimensionsNative(in NGXDimensions value)
{
    [FieldOffset(0)]
    public uint Width = value.Width;

    [FieldOffset(4)]
    public uint Height = value.Height;
}
