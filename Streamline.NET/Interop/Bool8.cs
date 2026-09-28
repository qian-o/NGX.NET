using System.Runtime.InteropServices;

namespace Streamline.NET;

/// <summary>
/// A native one-byte C++ Boolean. Nonzero values convert to true.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Bool8(bool value) : IEquatable<Bool8>
{
    /// <summary>
    /// The raw one-byte representation.
    /// </summary>
    public readonly byte Value = value ? (byte)1 : (byte)0;

    /// <summary>
    /// Converts a native Boolean to a managed Boolean.
    /// </summary>
    public static implicit operator bool(Bool8 value)
    {
        return value.Value != 0;
    }

    /// <summary>
    /// Converts a managed Boolean to a native Boolean.
    /// </summary>
    public static implicit operator Bool8(bool value)
    {
        return new(value);
    }

    /// <inheritdoc />
    public bool Equals(Bool8 other)
    {
        return (Value != 0) == (other.Value != 0);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Bool8 other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return (Value != 0).GetHashCode();
    }

    /// <summary>
    /// Compares the Boolean values.
    /// </summary>
    public static bool operator ==(Bool8 left, Bool8 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares the Boolean values.
    /// </summary>
    public static bool operator !=(Bool8 left, Bool8 right)
    {
        return !left.Equals(right);
    }
}
