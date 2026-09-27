namespace Streamline.NET;

public unsafe partial struct StructType : IEquatable<StructType>
{
    /// <summary>Constructs the native identifier from its integer fields and eight trailing bytes.</summary>
    public StructType(uint data1, ushort data2, ushort data3, ReadOnlySpan<byte> data4)
    {
        this = default;
        Data1 = data1;
        Data2 = data2;
        Data3 = data3;
        data4.CopyTo(Data4);
    }

    /// <inheritdoc />
    public readonly bool Equals(StructType other)
    {
        return Data1 == other.Data1 && Data2 == other.Data2 && Data3 == other.Data3
            && ((ReadOnlySpan<byte>)Data4).SequenceEqual(other.Data4);
    }

    /// <inheritdoc />
    public override readonly bool Equals(object? obj)
    {
        return obj is StructType other && Equals(other);
    }

    /// <inheritdoc />
    public override readonly int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Data1);
        hash.Add(Data2);
        hash.Add(Data3);
        hash.AddBytes(Data4);
        return hash.ToHashCode();
    }

    /// <summary>Compares all native identifier bytes.</summary>
    public static bool operator ==(StructType left, StructType right) => left.Equals(right);

    /// <summary>Compares all native identifier bytes.</summary>
    public static bool operator !=(StructType left, StructType right) => !left.Equals(right);
}
