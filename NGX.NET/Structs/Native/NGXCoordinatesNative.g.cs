#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 8)]
internal unsafe struct NGXCoordinatesNative : IDisposable
{
    [FieldOffset(0)]
    public uint X;

    [FieldOffset(4)]
    public uint Y;

    public NGXCoordinatesNative(in NGXCoordinates value)
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
