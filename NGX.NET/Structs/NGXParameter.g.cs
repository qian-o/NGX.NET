namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public readonly struct NGXParameter(nint value) : IEquatable<NGXParameter>
{
    public readonly nint Value = value;

    public bool IsNull => Value is 0;

    public bool Equals(NGXParameter other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NGXParameter other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return $"NGXParameter {{ Value = {Value}, IsNull = {IsNull} }}";
    }

    public static bool operator ==(NGXParameter left, NGXParameter right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NGXParameter left, NGXParameter right)
    {
        return !left.Equals(right);
    }
}
