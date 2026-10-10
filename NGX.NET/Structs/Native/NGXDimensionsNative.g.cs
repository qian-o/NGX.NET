#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXDimensionsNative
{
    [FieldOffset(0)]
    public uint Width;

    [FieldOffset(4)]
    public uint Height;

    public NGXDimensionsNative(in NGXDimensions value)
    {
        Width = value.Width;
        Height = value.Height;
    }
}
