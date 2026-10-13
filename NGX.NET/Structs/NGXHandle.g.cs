namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public readonly struct NGXHandle(nint value) : IEquatable<NGXHandle>
{
    public readonly nint Value = value;

    public bool IsNull => Value is 0;

    public bool Equals(NGXHandle other)
    {
        return Value == other.Value;
    }

    public override bool Equals(object? obj)
    {
        return obj is NGXHandle other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }

    public override string ToString()
    {
        return $"NGXHandle {{ Value = {Value}, IsNull = {IsNull} }}";
    }

    public static bool operator ==(NGXHandle left, NGXHandle right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(NGXHandle left, NGXHandle right)
    {
        return !left.Equals(right);
    }
}
