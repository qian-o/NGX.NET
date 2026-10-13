namespace NGX.NET.Generator;

internal class AstMacro(string name, string[] tokens, bool isFunctionLike)
{
    public readonly string Name = name;

    public readonly string[] Tokens = tokens;

    public readonly bool IsFunctionLike = isFunctionLike;
}
