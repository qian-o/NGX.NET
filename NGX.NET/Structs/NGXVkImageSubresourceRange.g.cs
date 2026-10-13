namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXVkImageSubresourceRange
{
    public uint AspectMask;

    public uint BaseMipLevel;

    public uint LevelCount;

    public uint BaseArrayLayer;

    public uint LayerCount;
}
