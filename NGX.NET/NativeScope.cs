using System.Buffers;
using System.Text;

namespace NGX.NET;

internal unsafe class NativeScope : DisposableObject
{
    private nint[]? allocations;
    private List<Delegate>? callbacks;
    private int allocationCount;

    internal T* Alloc<T>(in T value)
        where T : unmanaged
    {
        T* pointer = Alloc<T>(1);
        *pointer = value;

        return pointer;
    }

    internal T* Alloc<T>(int count)
        where T : unmanaged
    {
        if (count is 0)
        {
            return null;
        }

        T* pointer = (T*)NativeMemory.AllocZeroed((nuint)count, (nuint)sizeof(T));

        if (allocations is null)
        {
            allocations = ArrayPool<nint>.Shared.Rent(1);
        }
        else if (allocationCount == allocations.Length)
        {
            nint[] expanded = ArrayPool<nint>.Shared.Rent(allocations.Length + 1);
            allocations.AsSpan().CopyTo(expanded);
            ArrayPool<nint>.Shared.Return(allocations, clearArray: true);
            allocations = expanded;
        }

        allocations[allocationCount++] = (nint)pointer;

        return pointer;
    }

    internal byte* AllocUtf8(string? value)
    {
        if (value is null)
        {
            return null;
        }

        int length = Encoding.UTF8.GetByteCount(value);
        byte* pointer = Alloc<byte>(length + 1);
        Encoding.UTF8.GetBytes(value, new Span<byte>(pointer, length));

        return pointer;
    }

    internal void* AllocWide(string? value)
    {
        if (value is null)
        {
            return null;
        }

        Encoding encoding = OperatingSystem.IsWindows() ? Encoding.Unicode : Encoding.UTF32;
        int length = encoding.GetByteCount(value);
        int terminatorSize = OperatingSystem.IsWindows() ? sizeof(char) : sizeof(uint);
        byte* pointer = Alloc<byte>(length + terminatorSize);
        encoding.GetBytes(value, new Span<byte>(pointer, length));

        return pointer;
    }

    internal nint Keep<T>(T callback)
        where T : Delegate
    {
        (callbacks ??= []).Add(callback);

        return Marshal.GetFunctionPointerForDelegate<T>(callback);
    }

    protected override void Destroy()
    {
        if (allocations is nint[] pointers)
        {
            for (int i = allocationCount - 1; i >= 0; i--)
            {
                NativeMemory.Free((void*)pointers[i]);
            }

            ArrayPool<nint>.Shared.Return(pointers, clearArray: true);
            allocations = null;
            allocationCount = 0;
        }

        callbacks?.Clear();
    }
}
