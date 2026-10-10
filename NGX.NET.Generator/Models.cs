namespace NGX.NET.Generator;

internal class Models(Dictionary<string, AstRecord> records, Dictionary<string, AstEnum> enums, Dictionary<string, AstFunction> functions, Dictionary<string, AstType> aliases, Dictionary<string, AstMacro> macros, AstCallback[] callbacks)
{
    public readonly Dictionary<string, AstRecord> Records = records;

    public readonly Dictionary<string, AstEnum> Enums = enums;

    public readonly Dictionary<string, AstFunction> Functions = functions;

    public readonly Dictionary<string, AstType> Aliases = aliases;

    public readonly Dictionary<string, AstMacro> Macros = macros;

    public readonly AstCallback[] Callbacks = callbacks;
}

internal class AstType(string cpp, NativeTypeKind kind, int size, string name, AstType? element, AstType? result, AstType[] arguments, int count)
{
    public readonly string Cpp = cpp;

    public readonly NativeTypeKind Kind = kind;

    public readonly int Size = size;

    public readonly string Name = name;

    public readonly AstType? Element = element;

    public readonly AstType? Result = result;

    public readonly AstType[] Arguments = arguments;

    public readonly int Count = count;
}

internal class AstFunction(string name, string export, AstType result, AstParameter[] parameters)
{
    public readonly string Name = name;

    public readonly string Export = export;

    public readonly AstType Result = result;

    public readonly AstParameter[] Parameters = parameters;
}

internal class AstParameter(string name, AstType type, ParameterDirection direction, ParameterRole role, string countParameter)
{
    public readonly string Name = name;

    public readonly AstType Type = type;

    public readonly ParameterDirection Direction = direction;

    public readonly ParameterRole Role = role;

    public readonly string CountParameter = countParameter;
}

internal class AstCallback(string name, AstType result, AstCallbackParameter[] parameters)
{
    public readonly string Name = name;

    public readonly AstType Result = result;

    public readonly AstCallbackParameter[] Parameters = parameters;
}

internal class AstCallbackParameter(string name, AstType type, ParameterDirection direction)
{
    public readonly string Name = name;

    public readonly AstType Type = type;

    public readonly ParameterDirection Direction = direction;
}

internal class AstRecord(string name, bool isUnion, bool isOpaque, int size, AstField[] fields)
{
    public readonly string Name = name;

    public readonly bool IsUnion = isUnion;

    public readonly bool IsOpaque = isOpaque;

    public readonly int Size = size;

    public readonly AstField[] Fields = fields;
}

internal class AstField(string name, int offset, AstType type, string? declaration)
{
    public readonly string Name = name;

    public readonly int Offset = offset;

    public readonly AstType Type = type;

    public readonly string? Declaration = declaration;
}

internal class AstEnum(string name, AstEnumValue[] values)
{
    public readonly string Name = name;

    public readonly AstEnumValue[] Values = values;
}

internal class AstEnumValue(string name, long value)
{
    public readonly string Name = name;

    public readonly long Value = value;
}

internal class AstMacro(string name, string[] tokens, bool isFunctionLike)
{
    public readonly string Name = name;

    public readonly string[] Tokens = tokens;

    public readonly bool IsFunctionLike = isFunctionLike;
}

internal enum NativeTypeKind
{
    Void,

    Bool,

    CharS,

    CharU,

    SChar,

    UChar,

    Short,

    UShort,

    Int,

    UInt,

    Long,

    ULong,

    LongLong,

    ULongLong,

    Float,

    Double,

    WChar,

    Pointer,

    LValueReference,

    RValueReference,

    Record,

    Enum,

    ConstantArray,

    FunctionProto,

    FunctionNoProto
}

internal enum ParameterDirection
{
    In,

    Out,

    Ref
}

internal enum ParameterRole
{
    Default,

    RequiredName,

    Device,

    CudaDevice,

    OptionalRecord,

    FrameGenerationOptions,

    ExtensionCount,

    ExtensionProperties,

    ExtensionNames
}
