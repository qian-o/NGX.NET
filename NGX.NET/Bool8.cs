using System.Runtime.InteropServices;

namespace NGX.NET;

[StructLayout(LayoutKind.Sequential, Size = 1)]
internal readonly struct Bool8(bool value)
{
    public readonly byte Value = value ? (byte)1 : (byte)0;

    public static implicit operator Bool8(bool value)
    {
        return new(value);
    }

    public static implicit operator bool(Bool8 value)
    {
        return value.Value is not 0;
    }
}
