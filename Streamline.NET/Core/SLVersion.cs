using System.Globalization;

namespace Streamline.NET;

public unsafe partial struct SLVersion : IEquatable<SLVersion>
{
    /// <summary>
    /// True when any version component is nonzero.
    /// </summary>
    public static implicit operator bool(SLVersion value)
    {
        return value.Major != 0 || value.Minor != 0 || value.Build != 0;
    }

    /// <summary>
    /// Formats the native dotted version using invariant decimal digits.
    /// </summary>
    public readonly string ToStr()
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Build}");
    }

    /// <summary>
    /// Formats the native wide-string version as a managed string.
    /// </summary>
    public readonly string ToWStr()
    {
        return ToStr();
    }

    /// <summary>
    /// Formats the native packed OTA identifier without changing overflow behavior.
    /// </summary>
    public readonly string ToWStrOTAId()
    {
        return ((Major << 16) | (Minor << 8) | Build).ToString(CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public readonly bool Equals(SLVersion other)
    {
        return Major == other.Major && Minor == other.Minor && Build == other.Build;
    }

    /// <inheritdoc />
    public override readonly bool Equals(object? obj)
    {
        return obj is SLVersion other && Equals(other);
    }

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Major, Minor, Build);
    }

    /// <summary>
    /// Compares all version components.
    /// </summary>
    public static bool operator ==(SLVersion left, SLVersion right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares all version components.
    /// </summary>
    public static bool operator !=(SLVersion left, SLVersion right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Compares major, minor, and build in order.
    /// </summary>
    public static bool operator >(SLVersion left, SLVersion right)
    {
        return left.Major > right.Major ||
               (left.Major == right.Major && (left.Minor > right.Minor ||
                                            (left.Minor == right.Minor && left.Build > right.Build)));
    }

    /// <summary>
    /// Compares major, minor, and build in order.
    /// </summary>
    public static bool operator <(SLVersion left, SLVersion right)
    {
        return right > left;
    }

    /// <summary>
    /// Compares major, minor, and build in order.
    /// </summary>
    public static bool operator >=(SLVersion left, SLVersion right)
    {
        return left > right || left == right;
    }

    /// <summary>
    /// Compares major, minor, and build in order.
    /// </summary>
    public static bool operator <=(SLVersion left, SLVersion right)
    {
        return left < right || left == right;
    }
}
