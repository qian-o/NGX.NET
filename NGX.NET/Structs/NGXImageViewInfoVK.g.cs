namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXImageViewInfoVK
{
    public nint ImageView;

    public nint Image;

    public NGXVkImageSubresourceRange SubresourceRange;

    public NGXVkFormat Format;

    public uint Width;

    public uint Height;
}
