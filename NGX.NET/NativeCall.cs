namespace NGX.NET;

internal unsafe class NativeCall : IDisposable
{
    private NativeOwner? values;
    private List<nint>? strings;

    // Omitted DLSSG options leave earlier matrix pointers unchanged.
    internal bool HasFrameGenerationOptions { get; set; }

    public void Dispose()
    {
        while (values is not null)
        {
            NativeOwner value = values;
            values = value.Next;
            value.Dispose();
        }

        if (strings is not null)
        {
            foreach (nint value in strings)
            {
                NGXMarshal.Free((void*)value);
            }
            strings.Clear();
        }
    }

    internal void* String(string? value, NGXEncoding encoding)
    {
        void* pointer = NGXMarshal.TextToPtr(value, encoding);
        try
        {
            (strings ??= []).Add((nint)pointer);
        }
        catch
        {
            NGXMarshal.Free(pointer);

            throw;
        }

        return pointer;
    }

    internal T* Take<T>(ref T value)
        where T : unmanaged, IDisposable
    {
        NativeValue<T> owner = new(ref value);
        owner.Next = values;
        values = owner;

        return owner.Pointer;
    }
}
