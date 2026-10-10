namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXPathListInfoNative
{
    [FieldOffset(0)]
    public void** Path;

    [FieldOffset(8)]
    public uint Length;

    public NGXPathListInfoNative(in NGXPathListInfo value, NativeScope scope)
    {
        this = default;

        if (value.Paths is string[] paths && paths.Length > 0)
        {
            Path = (void**)scope.Alloc<nint>(paths.Length);
            Length = (uint)paths.Length;

            for (int i = 0; i < paths.Length; i++)
            {
                ArgumentNullException.ThrowIfNull(paths[i]);
                Path[i] = scope.AllocWide(paths[i]);
            }
        }
    }
}
