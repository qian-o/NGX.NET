namespace NGX.NET;

public struct NGXBufferInfoVK
{
    public nint Buffer;

    public uint SizeInBytes;

    internal unsafe NGXBufferInfoVK(in NGXBufferInfoVKNative native)
    {
        Buffer = native.Buffer;
        SizeInBytes = native.SizeInBytes;
    }
}
