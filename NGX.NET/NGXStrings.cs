using System.Text;

namespace NGX.NET;

public unsafe class NGXStrings : DisposableObject
{
    private readonly List<nint> allocations = [];

    public byte* AllocUtf8(string? value)
    {
        return Alloc(value, Encoding.UTF8, sizeof(byte));
    }

    public void* AllocWide(string? value)
    {
        return OperatingSystem.IsWindows() ? Alloc(value, Encoding.Unicode, sizeof(char)) : Alloc(value, Encoding.UTF32, sizeof(uint));
    }

    public byte** AllocUtf8(ReadOnlySpan<string?> values)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (values.IsEmpty)
        {
            return null;
        }

        byte** table = (byte**)NativeMemory.AllocZeroed(checked((nuint)values.Length * (nuint)sizeof(nint)));
        allocations.Add((nint)table);

        for (int i = 0; i < values.Length; i++)
        {
            table[i] = AllocUtf8(values[i]);
        }

        return table;
    }

    public void** AllocWide(ReadOnlySpan<string?> values)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (values.IsEmpty)
        {
            return null;
        }

        void** table = (void**)NativeMemory.AllocZeroed(checked((nuint)values.Length * (nuint)sizeof(nint)));
        allocations.Add((nint)table);

        for (int i = 0; i < values.Length; i++)
        {
            table[i] = AllocWide(values[i]);
        }

        return table;
    }

    internal byte* AllocUtf8(ReadOnlySpan<byte> value)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        int sizeInBytes = checked(value.Length + sizeof(byte));
        allocations.EnsureCapacity(checked(allocations.Count + 1));

        byte* pointer = (byte*)NativeMemory.AllocZeroed((nuint)sizeInBytes);
        allocations.Add((nint)pointer);
        value.CopyTo(new Span<byte>(pointer, value.Length));

        return pointer;
    }

    protected override void Destroy()
    {
        if (allocations is null)
        {
            return;
        }

        for (int i = allocations.Count - 1; i >= 0; i--)
        {
            NativeMemory.Free((void*)allocations[i]);
        }

        allocations.Clear();
    }

    private byte* Alloc(string? value, Encoding encoding, int terminatorSizeInBytes)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);

        if (value is null)
        {
            return null;
        }

        int lengthInBytes = encoding.GetByteCount(value);
        int sizeInBytes = checked(lengthInBytes + terminatorSizeInBytes);
        allocations.EnsureCapacity(checked(allocations.Count + 1));

        byte* pointer = (byte*)NativeMemory.AllocZeroed((nuint)sizeInBytes);
        allocations.Add((nint)pointer);
        encoding.GetBytes(value, new Span<byte>(pointer, lengthInBytes));

        return pointer;
    }
}
