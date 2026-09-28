namespace Streamline.NET;

public unsafe partial struct PrecisionInfo : IEquatable<PrecisionInfo>
{
    /// <summary>
    /// Returns the exact native name, or Unknown for an unrecognized formula.
    /// </summary>
    public static string GetPrecisionFormulaAsStr(PrecisionFormula formula)
    {
        return formula switch
        {
            PrecisionFormula.NoTransform => "eNoTransform",
            PrecisionFormula.LinearTransform => "eLinearTransform",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// True when a precision transform is requested.
    /// </summary>
    public static implicit operator bool(PrecisionInfo value)
    {
        return value.ConversionFormula != PrecisionFormula.NoTransform;
    }

    /// <inheritdoc />
    public readonly bool Equals(PrecisionInfo other)
    {
        return ConversionFormula == other.ConversionFormula && Bias == other.Bias && Scale == other.Scale;
    }

    /// <inheritdoc />
    public override readonly bool Equals(object? obj)
    {
        return obj is PrecisionInfo other && Equals(other);
    }

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        return HashCode.Combine(ConversionFormula, Bias, Scale);
    }

    /// <summary>
    /// Compares the precision settings, excluding the structure header.
    /// </summary>
    public static bool operator ==(PrecisionInfo left, PrecisionInfo right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Compares the precision settings, excluding the structure header.
    /// </summary>
    public static bool operator !=(PrecisionInfo left, PrecisionInfo right)
    {
        return !left.Equals(right);
    }
}
