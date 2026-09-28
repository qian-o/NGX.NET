namespace Streamline.NET;

#pragma warning disable RCS1242 // These overloads preserve the native const-reference contract.

public unsafe partial struct Extent : IEquatable<Extent>
{
    /// <summary>
    /// True when both native dimensions are nonzero.
    /// </summary>
    public static implicit operator bool(Extent value)
    {
        return value.Width != 0 && value.Height != 0;
    }

    /// <summary>
    /// Converts origin and dimensions to the native Windows rectangle coordinates.
    /// </summary>
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

    /// <summary>
    /// Compares resolution without comparing the origin.
    /// </summary>
    public readonly bool IsSameRes(in Extent other)
    {
        return Width == other.Width && Height == other.Height;
    }

    /// <inheritdoc />
    public readonly bool Equals(Extent other)
    {
        return Top == other.Top && Left == other.Left && Width == other.Width && Height == other.Height;
    }

    /// <inheritdoc />
    public override readonly bool Equals(object? obj)
    {
        return obj is Extent other && Equals(other);
    }

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Top, Left, Width, Height);
    }

    /// <summary>
    /// Compares all four coordinates.
    /// </summary>
    public static bool operator ==(Extent left, Extent right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares all four coordinates.
    /// </summary>
    public static bool operator !=(Extent left, Extent right)
    {
        return !left.Equals(right);
    }
}

#pragma warning restore RCS1242
