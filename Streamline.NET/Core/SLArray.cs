using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Streamline.NET;

/// <summary>
/// Native allocator-backed storage corresponding to sl::Array&lt;T&gt;.
/// </summary>
/// <remarks>
/// This is an owning, non-copyable native contract expressed as a CLR value type.
/// Do not copy an initialized value or release copies independently. Call Destroy
/// exactly once on the owning value; the allocator must outlive its allocations.
/// An outer native structure owning this member remains responsible for releasing it.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public unsafe struct SLArray<T> where T : unmanaged
{
    private T* data;

    private uint size;

    private IAllocator allocator;

    /// <summary>
    /// Returns the number of initialized native elements.
    /// </summary>
    public readonly uint Size()
    {
        return size;
    }

    /// <summary>
    /// Returns a reference into native storage. The index must be less than Size().
    /// </summary>
    public readonly ref T this[uint index]
    {
        get
        {
            Debug.Assert(index < size);

            return ref data[index];
        }
    }

    /// <summary>
    /// Releases existing storage and copies elements using the supplied native allocator.
    /// </summary>
    /// <remarks>
    /// The allocator is required for a nonempty source. The source must not alias the storage being released.
    /// </remarks>
    public void CopyFrom(IAllocator sourceAllocator, ReadOnlySpan<T> source)
    {
        Debug.Assert(sourceAllocator.Handle != 0 || source.IsEmpty);
        Destroy();

        if (source.IsEmpty)
        {
            return;
        }

        allocator = sourceAllocator;
        size = (uint)source.Length;
        data = (T*)allocator.Allocate(unchecked((uint)((nuint)size * (nuint)sizeof(T))));

        for (int index = 0; index < source.Length; index++)
        {
            data[index] = source[index];
        }
    }

    /// <summary>
    /// Replaces the managed destination contents with copies of the native elements.
    /// </summary>
    /// <remarks>
    /// The managed list is not passed across the native boundary.
    /// </remarks>
    public readonly void CopyTo(List<T> destination)
    {
        destination.Clear();

        for (uint index = 0; index < size; index++)
        {
            destination.Add(data[index]);
        }
    }

    /// <summary>
    /// Releases storage through its original allocator, then clears all native members.
    /// </summary>
    public void Destroy()
    {
        if (data != null)
        {
            allocator.Free(data);
        }

        allocator = default;
        data = null;
        size = 0;
    }
}
