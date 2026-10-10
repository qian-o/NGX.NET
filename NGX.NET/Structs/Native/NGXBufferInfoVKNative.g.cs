#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXBufferInfoVKNative : IDisposable
{
    [FieldOffset(0)]
    public nint Buffer;

    [FieldOffset(8)]
    public uint SizeInBytes;

    public NGXBufferInfoVKNative(in NGXBufferInfoVK value)
    {
        try
        {
            Buffer = value.Buffer;
            SizeInBytes = value.SizeInBytes;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
