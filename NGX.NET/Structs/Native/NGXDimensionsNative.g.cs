#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXDimensionsNative : IDisposable
{
    [FieldOffset(0)]
    public uint Width;

    [FieldOffset(4)]
    public uint Height;

    public NGXDimensionsNative(in NGXDimensions value)
    {
        try
        {
            Width = value.Width;
            Height = value.Height;
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
