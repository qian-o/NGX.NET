#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXCoordinatesVKNative : IDisposable
{
    [FieldOffset(0)]
    public uint X;

    [FieldOffset(4)]
    public uint Y;

    public NGXCoordinatesVKNative(in NGXCoordinatesVK value)
    {
        try
        {
            X = value.X;
            Y = value.Y;
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
