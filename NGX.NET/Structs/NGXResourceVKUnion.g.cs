namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 48)]
public struct NGXResourceVKUnion
{
    [FieldOffset(0)]
    public NGXImageViewInfoVK ImageViewInfo;

    [FieldOffset(0)]
    public NGXBufferInfoVK BufferInfo;
}
