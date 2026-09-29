using System.Runtime.InteropServices;

namespace Showcase.Helpers;

internal sealed unsafe class NativeNames : IDisposable
{
    public byte** Pointer { get; private set; }

    public uint Length { get; }

    public NativeNames(string[] names)
    {
        Length = (uint)names.Length;
        Pointer = (byte**)NativeMemory.AllocZeroed(Length, (nuint)sizeof(nint));

        try
        {
            for (int i = 0; i < names.Length; i++)
            {
                Pointer[i] = (byte*)Marshal.StringToCoTaskMemUTF8(names[i]);
            }
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    public void Dispose()
    {
        if (Pointer == null)
        {
            return;
        }

        for (int i = 0; i < Length; i++)
        {
            Marshal.FreeCoTaskMem((nint)Pointer[i]);
        }

        NativeMemory.Free(Pointer);
        Pointer = null;
    }
}
