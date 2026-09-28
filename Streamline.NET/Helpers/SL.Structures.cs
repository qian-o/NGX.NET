namespace Streamline.NET;

public static unsafe partial class SL
{
    /// <summary>
    /// Finds the first matching type in a native structure chain. The returned address borrows the original storage.
    /// </summary>
    public static T* FindStruct<T>(void* pointer) where T : unmanaged, ISLStructure
    {
        BaseStructure* current = (BaseStructure*)pointer;

        while (current != null && current->StructType != T.TypeId)
        {
            current = current->Next;
        }

        return (T*)current;
    }

    /// <summary>
    /// Finds T, stopping when the next visited node has type TStop.
    /// </summary>
    /// <remarks>
    /// The upstream helper dereferences a null next node at chain end. This wrapper returns null for that otherwise undefined case.
    /// </remarks>
    public static T* FindStruct<T, TStop>(void* pointer)
        where T : unmanaged, ISLStructure
        where TStop : unmanaged, ISLStructure
    {
        BaseStructure* current = (BaseStructure*)pointer;

        while (current != null && current->StructType != T.TypeId)
        {
            current = current->Next;

            if (current != null && current->StructType == TStop.TypeId)
            {
                return null;
            }
        }

        return (T*)current;
    }

    /// <summary>
    /// Searches native chain roots in order. The array contains pointers, not contiguous structures.
    /// </summary>
    public static T* FindStruct<T>(void** pointers, uint count) where T : unmanaged, ISLStructure
    {
        for (uint index = 0; index < count; index++)
        {
            T* result = FindStruct<T>(pointers[index]);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Appends every matching address to the existing managed list, preserving native traversal order.
    /// </summary>
    /// <remarks>
    /// Addresses borrow their source storage. The managed list is never passed to native code.
    /// </remarks>
    public static bool FindStructs<T>(void** pointers, uint count, List<nint> structures) where T : unmanaged, ISLStructure
    {
        for (uint index = 0; index < count; index++)
        {
            BaseStructure* current = (BaseStructure*)pointers[index];

            while (current != null)
            {
                if (current->StructType == T.TypeId)
                {
                    structures.Add((nint)current);
                }

                current = current->Next;
            }
        }

        return structures.Count > 0;
    }
}
