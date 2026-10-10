#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 20)]
internal unsafe struct NGXVkImageSubresourceRangeNative : IDisposable
{
    [FieldOffset(0)]
    public uint AspectMask;

    [FieldOffset(4)]
    public uint BaseMipLevel;

    [FieldOffset(8)]
    public uint LevelCount;

    [FieldOffset(12)]
    public uint BaseArrayLayer;

    [FieldOffset(16)]
    public uint LayerCount;

    public NGXVkImageSubresourceRangeNative(in NGXVkImageSubresourceRange value)
    {
        try
        {
            AspectMask = value.AspectMask;
            BaseMipLevel = value.BaseMipLevel;
            LevelCount = value.LevelCount;
            BaseArrayLayer = value.BaseArrayLayer;
            LayerCount = value.LayerCount;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
