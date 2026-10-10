namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
internal unsafe struct NGXResourceVKUnionNative
{
    [FieldOffset(0)]
    public NGXImageViewInfoVKNative ImageViewInfo;

    [FieldOffset(0)]
    public NGXBufferInfoVKNative BufferInfo;

    public NGXResourceVKUnionNative(in NGXResourceVKUnion value)
    {
        this = default;

        if (value.ImageViewInfo.HasValue && value.BufferInfo.HasValue)
        {
            throw new ArgumentException("Only one union member may be specified.", nameof(value));
        }

        if (value.ImageViewInfo is NGXImageViewInfoVK imageViewInfo)
        {
            ImageViewInfo = new(in imageViewInfo);
        }
        else if (value.BufferInfo is NGXBufferInfoVK bufferInfo)
        {
            BufferInfo = new(in bufferInfo);
        }
    }
}
