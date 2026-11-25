namespace NGX.NET.Generator;

internal class AstModel(AstSource source, Dictionary<string, AstRecord> records, Dictionary<string, AstEnum> enums, Dictionary<string, AstFunction> functions, Dictionary<string, AstMacro> macros, Dictionary<string, AstType> aliases)
{
    public readonly AstSource Source = source;

    public readonly Dictionary<string, AstRecord> Records = records;

    public readonly Dictionary<string, AstEnum> Enums = enums;

    public readonly Dictionary<string, AstFunction> Functions = functions;

    public readonly Dictionary<string, AstMacro> Macros = macros;

    public readonly Dictionary<string, AstType> Aliases = aliases;
}
