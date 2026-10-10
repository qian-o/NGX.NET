#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXPathListInfoNative : IDisposable
{
    [FieldOffset(0)]
    public void** Path;

    [FieldOffset(8)]
    public uint Length;

    public NGXPathListInfoNative(in NGXPathListInfo value)
    {
        this = default;

        try
        {
            if (value.Paths is string[] paths && paths.Length > 0)
            {
                Path = (void**)NativeMemory.AllocZeroed(checked((nuint)paths.Length * (nuint)sizeof(void*)));
                Length = checked((uint)paths.Length);

                for (int i = 0; i < paths.Length; i++)
                {
                    ArgumentNullException.ThrowIfNull(paths[i]);
                    Path[i] = NGXMarshal.TextToPtr(paths[i], NGXEncoding.NativeWide);
                }
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
        if (Path is not null)
        {
            for (int i = checked((int)Length) - 1; i >= 0; i--)
            {
                NGXMarshal.Free(Path[i]);
            }

            NativeMemory.Free(Path);
        }

        this = default;
    }
}
