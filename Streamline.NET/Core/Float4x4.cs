using System.Diagnostics.CodeAnalysis;

namespace Streamline.NET;

#pragma warning disable RCS1242 // These overloads preserve the native const-reference contract.

public unsafe partial struct Float4x4
{
    /// <summary>
    /// Initializes the four rows in native order.
    /// </summary>
    public Float4x4(Float4 row0, Float4 row1, Float4 row2, Float4 row3)
    {
        this = default;

        Row[0] = row0;
        Row[1] = row1;
        Row[2] = row2;
        Row[3] = row3;
    }

    /// <summary>
    /// Returns a reference into this matrix. The index must be less than four.
    /// </summary>
    [UnscopedRef]
    public ref Float4 this[uint index] => ref Row[(int)index];

    /// <summary>
    /// Replaces the specified row.
    /// </summary>
    public void SetRow(uint index, in Float4 value)
    {
        Row[(int)index] = value;
    }

    /// <summary>
    /// Returns a read-only reference into this matrix.
    /// </summary>
    [UnscopedRef]
    public readonly ref readonly Float4 GetRow(uint index)
    {
        return ref Row[(int)index];
    }
}

#pragma warning restore RCS1242
