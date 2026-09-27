using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Streamline.NET;

#pragma warning disable RCS1242 // These overloads preserve the native const-reference contract.

public unsafe partial struct Float4x4
{
    /// <summary>Initializes the four rows in native order.</summary>
    public Float4x4(Float4 row0, Float4 row1, Float4 row2, Float4 row3)
    {
        this = default;
        Row[0] = row0;
        Row[1] = row1;
        Row[2] = row2;
        Row[3] = row3;
    }

    /// <summary>Returns a reference into this matrix. The index must be less than four.</summary>
    [UnscopedRef]
    public ref Float4 this[uint index] => ref Row[(int)index];

    /// <summary>Replaces the specified row.</summary>
    public void SetRow(uint index, in Float4 value)
    {
        Row[(int)index] = value;
    }

    /// <summary>Returns a read-only reference into this matrix.</summary>
    [UnscopedRef]
    public readonly ref readonly Float4 GetRow(uint index)
    {
        return ref Row[(int)index];
    }
}

public unsafe partial struct Extent : IEquatable<Extent>
{
    /// <summary>True when both native dimensions are nonzero.</summary>
    public static implicit operator bool(Extent value) => value.Width != 0 && value.Height != 0;

    /// <summary>Converts origin and dimensions to the native Windows rectangle coordinates.</summary>
    public static implicit operator Rect(Extent value)
    {
        return new()
        {
            Left = unchecked((int)value.Left),
            Top = unchecked((int)value.Top),
            Right = unchecked((int)(value.Left + value.Width)),
            Bottom = unchecked((int)(value.Top + value.Height))
        };
    }

    /// <summary>Compares resolution without comparing the origin.</summary>
    public readonly bool IsSameRes(in Extent other) => Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public readonly bool Equals(Extent other) => Top == other.Top && Left == other.Left && Width == other.Width && Height == other.Height;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is Extent other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(Top, Left, Width, Height);

    /// <summary>Compares all four coordinates.</summary>
    public static bool operator ==(Extent left, Extent right) => left.Equals(right);

    /// <summary>Compares all four coordinates.</summary>
    public static bool operator !=(Extent left, Extent right) => !left.Equals(right);
}

public unsafe partial struct PrecisionInfo : IEquatable<PrecisionInfo>
{
    /// <summary>Returns the exact native name, or Unknown for an unrecognized formula.</summary>
    public static string GetPrecisionFormulaAsStr(PrecisionFormula formula)
    {
        return formula switch
        {
            PrecisionFormula.NoTransform => "eNoTransform",
            PrecisionFormula.LinearTransform => "eLinearTransform",
            _ => "Unknown"
        };
    }

    /// <summary>True when a precision transform is requested.</summary>
    public static implicit operator bool(PrecisionInfo value) => value.ConversionFormula != PrecisionFormula.NoTransform;

    /// <inheritdoc />
    public readonly bool Equals(PrecisionInfo other) => ConversionFormula == other.ConversionFormula && Bias == other.Bias && Scale == other.Scale;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is PrecisionInfo other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(ConversionFormula, Bias, Scale);

    /// <summary>Compares the precision settings, excluding the structure header.</summary>
    public static bool operator ==(PrecisionInfo left, PrecisionInfo right) => left.Equals(right);

    /// <summary>Compares the precision settings, excluding the structure header.</summary>
    public static bool operator !=(PrecisionInfo left, PrecisionInfo right) => !left.Equals(right);
}

public unsafe partial struct SLVersion : IEquatable<SLVersion>
{
    /// <summary>True when any version component is nonzero.</summary>
    public static implicit operator bool(SLVersion value) => value.Major != 0 || value.Minor != 0 || value.Build != 0;

    /// <summary>Formats the native dotted version using invariant decimal digits.</summary>
    public readonly string ToStr() => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Build}");

    /// <summary>Formats the native wide-string version as a managed string.</summary>
    public readonly string ToWStr() => ToStr();

    /// <summary>Formats the native packed OTA identifier without changing overflow behavior.</summary>
    public readonly string ToWStrOTAId() => ((Major << 16) | (Minor << 8) | Build).ToString(CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public readonly bool Equals(SLVersion other) => Major == other.Major && Minor == other.Minor && Build == other.Build;

    /// <inheritdoc />
    public override readonly bool Equals(object? obj) => obj is SLVersion other && Equals(other);

    /// <inheritdoc />
    public override readonly int GetHashCode() => HashCode.Combine(Major, Minor, Build);

    /// <summary>Compares all version components.</summary>
    public static bool operator ==(SLVersion left, SLVersion right) => left.Equals(right);

    /// <summary>Compares all version components.</summary>
    public static bool operator !=(SLVersion left, SLVersion right) => !left.Equals(right);

    /// <summary>Compares major, minor, and build in order.</summary>
    public static bool operator >(SLVersion left, SLVersion right) => left.Major > right.Major || (left.Major == right.Major && (left.Minor > right.Minor || (left.Minor == right.Minor && left.Build > right.Build)));

    /// <summary>Compares major, minor, and build in order.</summary>
    public static bool operator <(SLVersion left, SLVersion right) => right > left;

    /// <summary>Compares major, minor, and build in order.</summary>
    public static bool operator >=(SLVersion left, SLVersion right) => left > right || left == right;

    /// <summary>Compares major, minor, and build in order.</summary>
    public static bool operator <=(SLVersion left, SLVersion right) => left < right || left == right;
}

public unsafe partial struct ViewportHandle
{
    /// <summary>Reads the private native viewport value without changing its header.</summary>
    public static implicit operator uint(ViewportHandle handle) => handle.value;
}

public unsafe partial struct Resource
{
    /// <summary>Returns the borrowed ID3D12Resource address. No COM reference is acquired.</summary>
    public readonly nint AsD3D12Resource() => (nint)Native;

    /// <summary>Returns the borrowed ID3D11Resource address. No COM reference is acquired.</summary>
    public readonly nint AsD3D11Resource() => (nint)Native;

    /// <summary>Returns the borrowed ID3D11Buffer address. No COM reference is acquired.</summary>
    public readonly nint AsD3D11Buffer() => (nint)Native;

    /// <summary>Returns the borrowed ID3D11Texture2D address. No COM reference is acquired.</summary>
    public readonly nint AsD3D11Texture2D() => (nint)Native;
}

public unsafe partial struct PCLHelper
{
    /// <summary>Returns the stored native PCL marker.</summary>
    public readonly PCLMarker Get() => marker;
}

public unsafe partial struct ReflexHelper
{
    /// <summary>Returns the stored native marker value.</summary>
    public static implicit operator uint(ReflexHelper value) => value.marker;
}
