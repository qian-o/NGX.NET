namespace NGX.NET.Generator;

internal class AstField(string name, int offsetInBits, AstType type, string? declaration)
{
    public readonly string Name = name;

    public readonly int OffsetInBits = offsetInBits;

    public readonly AstType Type = type;

    public readonly string? Declaration = declaration;
}
