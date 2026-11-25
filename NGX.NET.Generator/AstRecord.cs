namespace NGX.NET.Generator;

internal class AstRecord(string name, bool isUnion, bool isOpaque, int sizeInBytes, AstField[] fields)
{
    public readonly string Name = name;

    public readonly bool IsUnion = isUnion;

    public readonly bool IsOpaque = isOpaque;

    public readonly int SizeInBytes = sizeInBytes;

    public readonly AstField[] Fields = fields;
}
