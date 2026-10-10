namespace NGX.NET;

public struct NGXResourceVK
{
    public NGXResourceVKUnion Resource;

    public NGXResourceVKType Type;

    public bool ReadWrite;

    internal unsafe NGXResourceVK(in NGXResourceVKNative native)
    {
        Resource = native.Type is NGXResourceVKType.VkImageView ? new() { ImageViewInfo = new NGXImageViewInfoVK(in native.Resource.ImageViewInfo) } : new() { BufferInfo = new NGXBufferInfoVK(in native.Resource.BufferInfo) };
        Type = native.Type;
        ReadWrite = native.ReadWrite;
    }
}
