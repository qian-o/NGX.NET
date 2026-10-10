namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXImageViewInfoVKNative(in NGXImageViewInfoVK value)
{
    [FieldOffset(0)]
    public nint ImageView = value.ImageView;

    [FieldOffset(8)]
    public nint Image = value.Image;

    [FieldOffset(16)]
    public NGXVkImageSubresourceRangeNative SubresourceRange = new(in value.SubresourceRange);

    [FieldOffset(36)]
    public NGXVkFormat Format = value.Format;

    [FieldOffset(40)]
    public uint Width = value.Width;

    [FieldOffset(44)]
    public uint Height = value.Height;
}
