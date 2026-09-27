using System.Runtime.InteropServices;
using System.Text;

namespace Streamline.NET;

internal static unsafe class NativeStrings
{
    internal static bool Equals(sbyte* value, ReadOnlySpan<byte> expected)
    {
        for (int index = 0; index < expected.Length; index++)
        {
            if ((byte)value[index] != expected[index])
            {
                return false;
            }
        }
        return value[expected.Length] == 0;
    }
}

internal unsafe ref struct Utf8StringArray
{
    private byte* allocation;

    internal Utf8StringArray(ReadOnlySpan<string> values)
    {
        nuint pointerBytes = checked((nuint)values.Length * (nuint)sizeof(nint));
        nuint bytes = pointerBytes;
        foreach (string value in values)
        {
            bytes = checked(bytes + (nuint)Encoding.UTF8.GetByteCount(value) + 1);
        }
        allocation = values.IsEmpty ? null : (byte*)NativeMemory.Alloc(bytes);
        byte* cursor = allocation + pointerBytes;
        for (int index = 0; index < values.Length; index++)
        {
            Pointer[index] = (sbyte*)cursor;
            int length = Encoding.UTF8.GetByteCount(values[index]);
            Encoding.UTF8.GetBytes(values[index], new Span<byte>(cursor, length));
            cursor[length] = 0;
            cursor += length + 1;
        }
    }

    internal readonly sbyte** Pointer => (sbyte**)allocation;

    public void Dispose()
    {
        NativeMemory.Free(allocation);
        allocation = null;
    }
}
