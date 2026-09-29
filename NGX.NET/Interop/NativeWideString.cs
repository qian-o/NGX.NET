using System.Runtime.InteropServices;
using System.Text;

namespace NGX.NET;

/// <summary>
/// Owns a null-terminated wchar_t string: UTF-16 on Windows, UTF-32 on Linux.
/// Keep it alive for as long as the native API retains its pointer.
/// </summary>
public sealed unsafe class NativeWideString : IDisposable
{
    private void* pointer;

    /// <summary>
    /// Pointer to the native string. The caller must not free it.
    /// </summary>
    public void* Pointer => pointer;

    /// <summary>
    /// Encodes a managed string using the platform's wchar_t representation.
    /// </summary>
    public NativeWideString(string value)
    {
        Encoding encoding = OperatingSystem.IsWindows() ? Encoding.Unicode : OperatingSystem.IsLinux() ? Encoding.UTF32 : throw new PlatformNotSupportedException();
        byte[] bytes = encoding.GetBytes(value + '\0');
        pointer = NativeMemory.Alloc((nuint)bytes.Length);
        bytes.CopyTo(new Span<byte>(pointer, bytes.Length));
    }

    /// <summary>
    /// Releases the owned string.
    /// </summary>
    public void Dispose()
    {
        NativeMemory.Free(pointer);
        pointer = null;
        GC.SuppressFinalize(this);
    }

    ~NativeWideString()
    {
        NativeMemory.Free(pointer);
    }
}
