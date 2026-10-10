using System.Text;

namespace NGX.NET;

internal static unsafe class NativeTextHelper
{
    internal static string ReadUtf8(ReadOnlySpan<byte> buffer)
    {
        int end = buffer.IndexOf((byte)0);

        return Encoding.UTF8.GetString(end < 0 ? buffer : buffer[..end]);
    }

    internal static string? ReadWide(void* pointer)
    {
        if (pointer is null)
        {
            return null;
        }

        if (OperatingSystem.IsWindows())
        {
            return Marshal.PtrToStringUni((nint)pointer);
        }

        uint* characters = (uint*)pointer;
        int length = 0;
        while (characters[length] is not 0)
        {
            length++;
        }

        return Encoding.UTF32.GetString(new ReadOnlySpan<byte>(pointer, length * sizeof(uint)));
    }
}
