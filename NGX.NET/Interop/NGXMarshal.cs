using System.Runtime.InteropServices;
using System.Text;

namespace NGX.NET;

/// <summary>
/// Explicit conversion between managed text and null-terminated native strings.
/// </summary>
public static unsafe class NGXMarshal
{
    /// <summary>
    /// Allocates and encodes the complete string, preserving embedded null
    /// characters and appending a terminator. Null maps to a null pointer.
    /// Release the allocation with Free after native code no longer uses it.
    /// </summary>
    public static void* StringToPtr(string? value, NGXEncoding encoding)
    {
        Encoding codec = GetEncoding(encoding);

        if (value is null)
        {
            return null;
        }

        int length = codec.GetByteCount(value);
        int terminatorSize = codec.GetByteCount("\0");
        byte* pointer = (byte*)NativeMemory.Alloc((nuint)checked(length + terminatorSize));

        try
        {
            codec.GetBytes(value.AsSpan(), new Span<byte>(pointer, length));
            new Span<byte>(pointer + length, terminatorSize).Clear();

            return pointer;
        }
        catch
        {
            NativeMemory.Free(pointer);
            throw;
        }
    }

    /// <summary>
    /// Copies native text up to the first null character into a managed string
    /// without freeing the source. The pointer must remain readable through its
    /// terminator. A null pointer returns null. This does not round-trip strings
    /// containing embedded null characters.
    /// </summary>
    public static string? PtrToString(void* pointer, NGXEncoding encoding)
    {
        Encoding codec = GetEncoding(encoding);

        if (pointer == null)
        {
            return null;
        }

        int length = codec.GetByteCount("\0") switch
        {
            sizeof(byte) => MemoryMarshal.CreateReadOnlySpanFromNullTerminated((byte*)pointer).Length,
            sizeof(char) => checked(MemoryMarshal.CreateReadOnlySpanFromNullTerminated((char*)pointer).Length * sizeof(char)),
            _ => Utf32ByteCount((uint*)pointer)
        };

        return codec.GetString(new ReadOnlySpan<byte>(pointer, length));
    }

    /// <summary>
    /// Releases memory allocated by StringToPtr. Null is allowed. Do not free
    /// borrowed SDK pointers or pinned managed buffers with this method.
    /// </summary>
    public static void Free(void* pointer)
    {
        NativeMemory.Free(pointer);
    }

    private static Encoding GetEncoding(NGXEncoding encoding)
    {
        return encoding switch
        {
            NGXEncoding.Utf8 => Encoding.UTF8,
            NGXEncoding.NativeWide => OperatingSystem.IsWindows() ? Encoding.Unicode
                : OperatingSystem.IsLinux() ? Encoding.UTF32
                : throw new PlatformNotSupportedException("NativeWide supports Windows and Linux wchar_t encodings."),
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unsupported native string encoding.")
        };
    }

    private static int Utf32ByteCount(uint* pointer)
    {
        int length = 0;

        while (pointer[length] != 0)
        {
            length = checked(length + 1);
        }

        return checked(length * sizeof(uint));
    }
}
