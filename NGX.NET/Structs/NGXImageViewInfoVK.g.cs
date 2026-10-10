#nullable enable

namespace NGX.NET;

public struct NGXImageViewInfoVK
{
    public nint ImageView;

    public nint Image;

    public NGXVkImageSubresourceRange SubresourceRange;

    public NGXVkFormat Format;

    public uint Width;

    public uint Height;

    internal unsafe NGXImageViewInfoVK(in NGXImageViewInfoVKNative native)
    {
        ImageView = native.ImageView;
        Image = native.Image;
        SubresourceRange = new(in native.SubresourceRange);
        Format = native.Format;
        Width = native.Width;
        Height = native.Height;
    }
}
