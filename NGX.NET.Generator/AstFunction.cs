namespace NGX.NET.Generator;

internal class AstFunction(string name, string export, AstType result, AstParameter[] parameters)
{
    public readonly string Name = name;

    public readonly string Export = export;

    public readonly AstType Result = result;

    public readonly AstParameter[] Parameters = parameters;
}
