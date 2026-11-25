namespace NGX.NET.Generator;

internal class AstParameter(string name, AstType type, ParameterDirection direction)
{
    public readonly string Name = name;

    public readonly AstType Type = type;

    public readonly ParameterDirection Direction = direction;
}
