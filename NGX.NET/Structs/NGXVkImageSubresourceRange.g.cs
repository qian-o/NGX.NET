#nullable enable

namespace NGX.NET;

public struct NGXVkImageSubresourceRange
{
    public uint AspectMask;

    public uint BaseMipLevel;

    public uint LevelCount;

    public uint BaseArrayLayer;

    public uint LayerCount;

    internal unsafe NGXVkImageSubresourceRange(in NGXVkImageSubresourceRangeNative native)
    {
        AspectMask = native.AspectMask;
        BaseMipLevel = native.BaseMipLevel;
        LevelCount = native.LevelCount;
        BaseArrayLayer = native.BaseArrayLayer;
        LayerCount = native.LayerCount;
    }
}
