namespace NGX.NET.Generator;

internal class AstType(string cpp, NativeTypeKind kind, int sizeInBytes, string name, AstType? element, AstType? result, AstType[] arguments, int count)
{
    public readonly string Cpp = cpp;

    public readonly NativeTypeKind Kind = kind;

    public readonly int SizeInBytes = sizeInBytes;

    public readonly string Name = name;

    public readonly AstType? Element = element;

    public readonly AstType? Result = result;

    public readonly AstType[] Arguments = arguments;

    public readonly int Count = count;
}
