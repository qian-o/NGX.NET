using System.Runtime.InteropServices;
using System.Text;

namespace NGX.NET;

/// <summary>
/// Explicit conversion between managed text and null-terminated native strings.
/// </summary>
public static unsafe class NGXMarshal
{
    private static readonly Encoding UTF8 = new UTF8Encoding(false, true);
    private static readonly Encoding UTF16 = new UnicodeEncoding(false, false, true);
    private static readonly Encoding UTF32 = new UTF32Encoding(false, false, true);

    /// <summary>
    /// Allocates and encodes the complete string, preserving embedded null
    /// characters and appending a terminator. Null maps to a null pointer.
    /// Invalid Unicode is rejected. Release the allocation with Free after
    /// native code no longer uses it.
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

    internal static T* AllocValue<T>(T value)
        where T : unmanaged
    {
        T* pointer = (T*)NativeMemory.Alloc((nuint)sizeof(T));
        *pointer = value;

        return pointer;
    }

    internal static void* TextToPtr(string? value, NGXEncoding encoding)
    {
        if (value?.Contains('\0') is true)
        {
            throw new ArgumentException("Text cannot contain an embedded null character.", nameof(value));
        }

        return StringToPtr(value, encoding);
    }

    // Transfers the newly constructed native value, including its allocations.
    internal static T* AllocNative<T>(T value)
        where T : unmanaged, IDisposable
    {
        try
        {
            return AllocValue(value);
        }
        catch
        {
            value.Dispose();

            throw;
        }
    }

    internal static void FreeNative<T>(T* pointer)
        where T : unmanaged, IDisposable
    {
        if (pointer == null)
        {
            return;
        }

        pointer->Dispose();
        NativeMemory.Free(pointer);
    }

    internal static void WriteUtf8(string? value, Span<byte> buffer)
    {
        buffer.Clear();

        if (value is null)
        {
            return;
        }

        if (value.Contains('\0'))
        {
            throw new ArgumentException("Text cannot contain an embedded null character.", nameof(value));
        }

        int length = UTF8.GetByteCount(value);
        if (length >= buffer.Length)
        {
            throw new ArgumentException("UTF-8 text exceeds the native buffer capacity.", nameof(value));
        }

        UTF8.GetBytes(value, buffer);
    }

    internal static string ReadUtf8(ReadOnlySpan<byte> buffer)
    {
        int end = buffer.IndexOf((byte)0);

        return UTF8.GetString(end is < 0 ? buffer : buffer[..end]);
    }

    private static Encoding GetEncoding(NGXEncoding encoding)
    {
        return encoding switch
        {
            NGXEncoding.Utf8 => UTF8,
            NGXEncoding.NativeWide => OperatingSystem.IsWindows() ? UTF16 : UTF32,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, "Unsupported native string encoding.")
        };
    }

    private static int Utf32ByteCount(uint* pointer)
    {
        int length = 0;
        while (pointer[length] is not 0)
        {
            length = checked(length + 1);
        }

        return checked(length * sizeof(uint));
    }
}
