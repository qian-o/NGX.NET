#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXImageViewInfoVKNative : IDisposable
{
    [FieldOffset(0)]
    public nint ImageView;

    [FieldOffset(8)]
    public nint Image;

    [FieldOffset(16)]
    public NGXVkImageSubresourceRangeNative SubresourceRange;

    [FieldOffset(36)]
    public NGXVkFormat Format;

    [FieldOffset(40)]
    public uint Width;

    [FieldOffset(44)]
    public uint Height;

    public NGXImageViewInfoVKNative(in NGXImageViewInfoVK value)
    {
        try
        {
            ImageView = value.ImageView;
            Image = value.Image;
            SubresourceRange = new(in value.SubresourceRange);
            Format = value.Format;
            Width = value.Width;
            Height = value.Height;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        SubresourceRange.Dispose();
        this = default;
    }
}
