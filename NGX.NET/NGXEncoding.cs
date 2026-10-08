namespace NGX.NET;

/// <summary>
/// Encoding of a null-terminated native string.
/// </summary>
public enum NGXEncoding
{
    /// <summary>
    /// UTF-8 bytes followed by a zero byte.
    /// </summary>
    Utf8,

    /// <summary>
    /// Native wchar_t: UTF-16 on Windows and UTF-32 on Unix.
    /// </summary>
    NativeWide
}
