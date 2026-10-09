using System.Runtime.InteropServices;
using System.Text;

namespace Showcase.Helpers;

internal unsafe class NativeNames : IDisposable
{
    public NativeNames(string[] names)
    {
        Length = (uint)names.Length;
        int size = checked(names.Length * sizeof(nint));
        foreach (string name in names)
        {
            size = checked(size + Encoding.UTF8.GetByteCount(name) + 1);
        }

        // The pointer table and UTF-8 strings share one allocation.
        Pointer = (byte**)NativeMemory.Alloc((nuint)size);
        byte* text = (byte*)(Pointer + names.Length);
        int remaining = size - (names.Length * sizeof(nint));
        for (int i = 0; i < names.Length; i++)
        {
            Pointer[i] = text;
            int count = Encoding.UTF8.GetBytes(names[i].AsSpan(), new Span<byte>(text, remaining));
            text[count++] = 0;
            text += count;
            remaining -= count;
        }
    }

    public byte** Pointer { get; private set; }

    public uint Length { get; }

    public void Dispose()
    {
        NativeMemory.Free(Pointer);
        Pointer = null;
    }
}
