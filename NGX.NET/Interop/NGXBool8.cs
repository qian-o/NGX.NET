using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// One-byte native C/C++ bool with blittable interop and callback semantics.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct NGXBool8(byte value) : IEquatable<NGXBool8>
{
    private readonly byte value = value;

    /// <summary>
    /// Converts a managed Boolean to the native representation.
    /// </summary>
    public static implicit operator NGXBool8(bool value)
    {
        return new(value ? (byte)1 : (byte)0);
    }

    /// <summary>
    /// Treats any nonzero native value as true.
    /// </summary>
    public static implicit operator bool(NGXBool8 value)
    {
        return value.value != 0;
    }

    /// <inheritdoc/>
    public bool Equals(NGXBool8 other)
    {
        return (bool)this == (bool)other;
    }

    /// <inheritdoc/>
    public override bool Equals(object? obj)
    {
        return obj is NGXBool8 other && Equals(other);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        return ((bool)this).GetHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return ((bool)this).ToString();
    }
}
