namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXBufferInfoVK
{
    public nint Buffer;

    public uint SizeInBytes;
}
