namespace NGX.NET.Generator;

internal static class AstReader
{
    private static readonly string[] OutputPrefixes = ["Out", "ppOut", "pOut"];
    private static readonly HashSet<string> OutputNames = ["pVRAMAllocatedBytes", "pOptLevel", "IsDevSnippetBranch", "estimatedVRAMInBytes"];
    private static readonly Dictionary<string, (ParameterRole Role, string CountParameter)> ParameterRoles = new(StringComparer.Ordinal)
    {
        ["InName"] = (ParameterRole.RequiredName, string.Empty),
        ["InDevice"] = (ParameterRole.Device, string.Empty),
        ["InFeatureInfo"] = (ParameterRole.OptionalRecord, string.Empty),
        ["pInDlssgOptEvalParams"] = (ParameterRole.FrameGenerationOptions, string.Empty),
        ["OutExtensionCount"] = (ParameterRole.ExtensionCount, string.Empty),
        ["OutInstanceExtCount"] = (ParameterRole.ExtensionCount, string.Empty),
        ["OutDeviceExtCount"] = (ParameterRole.ExtensionCount, string.Empty),
        ["OutExtensionProperties"] = (ParameterRole.ExtensionProperties, "OutExtensionCount"),
        ["OutInstanceExts"] = (ParameterRole.ExtensionNames, "OutInstanceExtCount"),
        ["OutDeviceExts"] = (ParameterRole.ExtensionNames, "OutDeviceExtCount")
    };

    internal static Models Read(JsonElement ast)
    {
        JsonElement[] platforms = [.. ast.GetProperty("platforms").EnumerateObject().Select(static platform => platform.Value)];
        Dictionary<string, AstRecord> records = Merge(platforms, "records", ReadRecord);
        Dictionary<string, AstEnum> enums = Merge(platforms, "enums", ReadEnum);
        Dictionary<string, AstFunction> functions = Merge(platforms, "functions", ReadFunction);
        Dictionary<string, AstType> aliases = platforms.SelectMany(static platform => platform.GetProperty("aliases").EnumerateObject()).GroupBy(static alias => alias.Name).ToDictionary(static group => group.Key, static group => ReadType(group.First().Value));
        Dictionary<string, AstMacro> macros = Merge(platforms, "macros", ReadMacro);
        AstCallback[] callbacks = [.. aliases.Where(static alias => alias.Value.Element is { Kind: NativeTypeKind.FunctionProto }).Select(static alias => ReadCallback(alias.Key, alias.Value.Element!))];

        return new(records, enums, functions, aliases, macros, callbacks);
    }

    private static Dictionary<string, T> Merge<T>(JsonElement[] platforms, string collection, Func<JsonElement, T> read)
    {
        return platforms.SelectMany(platform => platform.GetProperty(collection).EnumerateArray()).GroupBy(static value => value.GetProperty("name").GetString()!).ToDictionary(static group => group.Key, group => read(group.First()));
    }

    private static AstType ReadType(JsonElement value)
    {
        string cpp = value.GetProperty("cpp").GetString()!;
        string nativeKind = value.GetProperty("kind").GetString()!;
        NativeTypeKind kind = nativeKind switch
        {
            "VOID" => NativeTypeKind.Void,
            "BOOL" => NativeTypeKind.Bool,
            "CHAR_S" => NativeTypeKind.CharS,
            "CHAR_U" => NativeTypeKind.CharU,
            "SCHAR" => NativeTypeKind.SChar,
            "UCHAR" => NativeTypeKind.UChar,
            "SHORT" => NativeTypeKind.Short,
            "USHORT" => NativeTypeKind.UShort,
            "INT" => NativeTypeKind.Int,
            "UINT" => NativeTypeKind.UInt,
            "LONG" => NativeTypeKind.Long,
            "ULONG" => NativeTypeKind.ULong,
            "LONGLONG" => NativeTypeKind.LongLong,
            "ULONGLONG" => NativeTypeKind.ULongLong,
            "FLOAT" => NativeTypeKind.Float,
            "DOUBLE" => NativeTypeKind.Double,
            "WCHAR" => NativeTypeKind.WChar,
            "POINTER" => NativeTypeKind.Pointer,
            "LVALUEREFERENCE" => NativeTypeKind.LValueReference,
            "RVALUEREFERENCE" => NativeTypeKind.RValueReference,
            "RECORD" => NativeTypeKind.Record,
            "ENUM" => NativeTypeKind.Enum,
            "CONSTANTARRAY" => NativeTypeKind.ConstantArray,
            "FUNCTIONPROTO" => NativeTypeKind.FunctionProto,
            "FUNCTIONNOPROTO" => NativeTypeKind.FunctionNoProto,
            _ => throw new InvalidOperationException($"Unsupported native type kind: {nativeKind}.")
        };
        int size = value.GetProperty("size").GetInt32();
        string name = value.TryGetProperty("name", out JsonElement named) ? named.GetString()! : string.Empty;
        AstType? element = value.TryGetProperty("element", out JsonElement nested) ? ReadType(nested) : null;
        AstType? result = value.TryGetProperty("result", out JsonElement returned) ? ReadType(returned) : null;
        AstType[] arguments = value.TryGetProperty("arguments", out JsonElement parameters) ? [.. parameters.EnumerateArray().Select(ReadType)] : [];
        int count = value.TryGetProperty("count", out JsonElement length) ? length.GetInt32() : 0;

        return new(cpp, kind, size, name, element, result, arguments, count);
    }

    private static AstFunction ReadFunction(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        string export = value.GetProperty("export").GetString()!;
        AstType result = ReadType(value.GetProperty("result"));
        AstParameter[] parameters = [.. value.GetProperty("parameters").EnumerateArray().Select(parameter => ReadParameter(name, parameter))];

        return new(name, export, result, parameters);
    }

    private static AstParameter ReadParameter(string function, JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        AstType type = ReadType(value.GetProperty("type"));
        ParameterDirection direction = Direction(function, name, type);
        (ParameterRole role, string countParameter) = ParameterRoles.GetValueOrDefault(name);

        if (type.Element is { Kind: NativeTypeKind.Record, Name: "NVSDK_NGX_CUDADevice" })
        {
            role = ParameterRole.CudaDevice;
        }

        return new(name, type, direction, role, countParameter ?? string.Empty);
    }

    private static ParameterDirection Direction(string function, string name, AstType type)
    {
        if (type.Kind is not (NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference))
        {
            return ParameterDirection.In;
        }

        AstType element = type.Element!;
        if (element.Kind is NativeTypeKind.Record)
        {
            return name is "OutSupported" ? ParameterDirection.Out : ParameterDirection.In;
        }

        if (element.Kind is NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto or NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar or NativeTypeKind.Void)
        {
            return ParameterDirection.In;
        }

        if (OutputNames.Contains(name) || OutputPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.Ordinal)))
        {
            return ParameterDirection.Out;
        }

        throw new InvalidOperationException($"Unclassified pointer direction: {function}::{name}.");
    }

    private static AstCallback ReadCallback(string name, AstType signature)
    {
        string[] names = CallbackArguments(name);
        if (names.Length != signature.Arguments.Length)
        {
            throw new InvalidOperationException($"Callback signature changed: {name}.");
        }

        AstCallbackParameter[] parameters = [.. signature.Arguments.Select((type, index) => new AstCallbackParameter(names[index], type, CallbackDirection(type)))];

        return new(name, signature.Result!, parameters);
    }

    private static ParameterDirection CallbackDirection(AstType type)
    {
        if (type.Kind is not (NativeTypeKind.Pointer or NativeTypeKind.LValueReference))
        {
            return ParameterDirection.In;
        }

        return type.Element!.Kind switch
        {
            NativeTypeKind.Bool => ParameterDirection.Ref,
            NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.Record or NativeTypeKind.Void => ParameterDirection.In,
            _ => ParameterDirection.Out
        };
    }

    private static AstRecord ReadRecord(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        bool isUnion = value.GetProperty("kind").GetString() is "UNION_DECL";
        bool isOpaque = value.GetProperty("opaque").GetBoolean();
        int size = value.GetProperty("size").GetInt32();
        AstField[] fields = [.. value.GetProperty("fields").EnumerateArray().Select(ReadField)];

        return new(name, isUnion, isOpaque, size, fields);
    }

    private static AstField ReadField(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        int offset = value.GetProperty("offset").GetInt32();
        AstType type = ReadType(value.GetProperty("type"));
        string? declaration = value.TryGetProperty("declaration", out JsonElement declared) ? declared.GetString() : null;

        return new(name, offset, type, declaration);
    }

    private static AstEnum ReadEnum(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        AstEnumValue[] values = [.. value.GetProperty("values").EnumerateArray().Select(static member => new AstEnumValue(member.GetProperty("name").GetString()!, member.GetProperty("value").GetInt64()))];

        return new(name, values);
    }

    private static AstMacro ReadMacro(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        string[] tokens = [.. value.GetProperty("tokens").EnumerateArray().Select(static token => token.GetString()!)];
        bool isFunctionLike = value.GetProperty("functionLike").GetBoolean();

        return new(name, tokens, isFunctionLike);
    }
}
