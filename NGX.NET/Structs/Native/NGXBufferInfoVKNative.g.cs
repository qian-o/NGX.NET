namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXBufferInfoVKNative(in NGXBufferInfoVK value)
{
    [FieldOffset(0)]
    public nint Buffer = value.Buffer;

    [FieldOffset(8)]
    public uint SizeInBytes = value.SizeInBytes;
}
