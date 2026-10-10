#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXResourceVKNative
{
    [FieldOffset(0)]
    public NGXResourceVKUnionNative Resource;

    [FieldOffset(48)]
    public NGXResourceVKType Type;

    [FieldOffset(52)]
    public Bool8 ReadWrite;

    public NGXResourceVKNative(in NGXResourceVK value)
    {
        if ((value.Type is NGXResourceVKType.VkImageView && value.Resource.BufferInfo.HasValue) || (value.Type is NGXResourceVKType.VkBuffer && value.Resource.ImageViewInfo.HasValue))
        {
            throw new ArgumentException("Resource type and union member disagree.", nameof(value));
        }

        Resource = new(in value.Resource);
        Type = value.Type;
        ReadWrite = value.ReadWrite;
    }
}
