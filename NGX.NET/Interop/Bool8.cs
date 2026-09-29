using System.Runtime.InteropServices;

namespace NGX.NET;

/// <summary>
/// One-byte native C/C++ bool with blittable interop and callback semantics.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 1)]
public readonly struct Bool8(byte value) : IEquatable<Bool8>
{
    private readonly byte value = value;

    /// <summary>
    /// Converts a managed Boolean to the native representation.
    /// </summary>
    public static implicit operator Bool8(bool value) => new(value ? (byte)1 : (byte)0);

    /// <summary>
    /// Treats any nonzero native value as true.
    /// </summary>
    public static implicit operator bool(Bool8 value) => value.value != 0;

    /// <inheritdoc/>
    public bool Equals(Bool8 other) => (bool)this == (bool)other;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is Bool8 other && Equals(other);

    /// <inheritdoc/>
    public override int GetHashCode() => ((bool)this).GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => ((bool)this).ToString();
}
