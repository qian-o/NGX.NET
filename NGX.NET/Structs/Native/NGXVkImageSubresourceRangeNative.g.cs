namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 20)]
internal unsafe struct NGXVkImageSubresourceRangeNative(in NGXVkImageSubresourceRange value)
{
    [FieldOffset(0)]
    public uint AspectMask = value.AspectMask;

    [FieldOffset(4)]
    public uint BaseMipLevel = value.BaseMipLevel;

    [FieldOffset(8)]
    public uint LevelCount = value.LevelCount;

    [FieldOffset(12)]
    public uint BaseArrayLayer = value.BaseArrayLayer;

    [FieldOffset(16)]
    public uint LayerCount = value.LayerCount;
}
