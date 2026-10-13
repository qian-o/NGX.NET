namespace NGX.NET.Generator;

internal static class AstReader
{
    private static readonly string[] OutputPrefixes = ["Out", "ppOut", "pOut"];
    private static readonly HashSet<string> OutputNames = ["pVRAMAllocatedBytes", "pOptLevel", "IsDevSnippetBranch", "estimatedVRAMInBytes"];

    internal static AstModel Read(JsonElement ast)
    {
        JsonElement[] platforms = [.. ast.GetProperty("platforms").EnumerateObject().Select(static platform => platform.Value)];
        Dictionary<string, AstRecord> records = Merge(platforms, "records", ReadRecord, GetRecordSignature);
        Dictionary<string, AstEnum> enums = Merge(platforms, "enums", ReadEnum, GetEnumSignature);
        Dictionary<string, AstFunction> functions = Merge(platforms, "functions", ReadFunction, GetFunctionSignature);
        Dictionary<string, AstMacro> macros = Merge(platforms, "macros", ReadMacro, GetMacroSignature);
        Dictionary<string, string> aliases = [];
        foreach (JsonElement platform in platforms)
        {
            foreach (JsonProperty alias in platform.GetProperty("aliases").EnumerateObject())
            {
                string signature = GetTypeSignature(ReadType(alias.Value));
                if (aliases.TryGetValue(alias.Name, out string? previous) && previous != signature)
                {
                    throw new InvalidOperationException($"Incompatible native alias: {alias.Name}.");
                }

                aliases[alias.Name] = signature;
            }
        }

        return new(records, enums, functions, macros);
    }

    private static Dictionary<string, T> Merge<T>(ReadOnlySpan<JsonElement> platforms, string collection, Func<JsonElement, T> read, Func<T, string> signature)
    {
        Dictionary<string, T> result = [];
        foreach (JsonElement platform in platforms)
        {
            foreach (JsonElement value in platform.GetProperty(collection).EnumerateArray())
            {
                string name = value.GetProperty("name").GetString()!;
                T declaration = read(value);
                if (result.TryGetValue(name, out T? previous))
                {
                    if (signature(previous) != signature(declaration))
                    {
                        throw new InvalidOperationException($"Incompatible native {collection} declaration: {name}.");
                    }
                }
                else
                {
                    result.Add(name, declaration);
                }
            }
        }

        return result;
    }

    private static string GetRecordSignature(AstRecord record)
    {
        string fields = string.Join(";", record.Fields.Select(static field => $"{field.Name}:{field.OffsetInBits}:{GetTypeSignature(field.Type)}:{GetInitializer(field.Declaration)}"));

        return $"{record.IsUnion}:{record.IsOpaque}:{record.SizeInBytes}:{fields}";
    }

    private static string GetFunctionSignature(AstFunction function)
    {
        string parameters = string.Join(";", function.Parameters.Select(static parameter => $"{parameter.Name}:{GetTypeSignature(parameter.Type)}"));

        return $"{function.Export}:{GetTypeSignature(function.Result)}:{parameters}";
    }

    private static string GetEnumSignature(AstEnum value)
    {
        return string.Join(";", value.Values.Select(static member => $"{member.Name}:{unchecked((uint)member.Value)}"));
    }

    private static string GetMacroSignature(AstMacro macro)
    {
        if (macro.Name is "NVSDK_CONV" && (macro.Tokens.Length is 0 || macro.Tokens is ["__cdecl"]))
        {
            return "Cdecl";
        }

        return $"{macro.IsFunctionLike}:{string.Join(' ', macro.Tokens)}";
    }

    private static string GetTypeSignature(AstType type)
    {
        if (type.Cpp.Replace("const ", string.Empty, StringComparison.Ordinal).Trim() is "size_t")
        {
            return $"size_t:{type.SizeInBytes}";
        }

        if (type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            string element = type.Element!.Kind is NativeTypeKind.WChar && type.Element.SizeInBytes is 2 or 4 ? "wchar_t" : GetTypeSignature(type.Element);

            return $"{type.Kind}:{type.SizeInBytes}:{element}";
        }

        return type.Kind switch
        {
            NativeTypeKind.CharS or NativeTypeKind.CharU => $"char:{type.SizeInBytes}",
            NativeTypeKind.Long or NativeTypeKind.LongLong => $"signed:{type.SizeInBytes}",
            NativeTypeKind.ULong or NativeTypeKind.ULongLong => $"unsigned:{type.SizeInBytes}",
            NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto => $"function:{GetTypeSignature(type.Result!)}({string.Join(';', type.Arguments.Select(GetTypeSignature))})",
            NativeTypeKind.ConstantArray => $"array:{type.Count}:{type.SizeInBytes}:{GetTypeSignature(type.Element!)}",
            _ => $"{type.Kind}:{type.SizeInBytes}:{type.Name}"
        };
    }

    private static string GetInitializer(string? declaration)
    {
        return declaration is not null && declaration.Contains('=') ? declaration.Split('=', 2)[1].Trim() : string.Empty;
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
        int sizeInBytes = value.GetProperty("size").GetInt32();
        string name = value.TryGetProperty("name", out JsonElement named) ? named.GetString()! : string.Empty;
        AstType? element = value.TryGetProperty("element", out JsonElement nested) ? ReadType(nested) : null;
        AstType? result = value.TryGetProperty("result", out JsonElement returned) ? ReadType(returned) : null;
        AstType[] arguments = value.TryGetProperty("arguments", out JsonElement parameters) ? [.. parameters.EnumerateArray().Select(ReadType)] : [];
        int count = value.TryGetProperty("count", out JsonElement length) ? length.GetInt32() : 0;

        return new(cpp, kind, sizeInBytes, name, element, result, arguments, count);
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
        ParameterDirection direction = GetDirection(function, name, type);

        return new(name, type, direction);
    }

    private static ParameterDirection GetDirection(string function, string name, AstType type)
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

    private static AstRecord ReadRecord(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        bool isUnion = value.GetProperty("kind").GetString() is "UNION_DECL";
        bool isOpaque = value.GetProperty("opaque").GetBoolean();
        int sizeInBytes = value.GetProperty("size").GetInt32();
        AstField[] fields = [.. value.GetProperty("fields").EnumerateArray().Select(ReadField)];

        return new(name, isUnion, isOpaque, sizeInBytes, fields);
    }

    private static AstField ReadField(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        int offsetInBits = value.GetProperty("offset").GetInt32();
        AstType type = ReadType(value.GetProperty("type"));
        string? declaration = value.TryGetProperty("declaration", out JsonElement declared) ? declared.GetString() : null;

        return new(name, offsetInBits, type, declaration);
    }

    private static AstEnum ReadEnum(JsonElement value)
    {
        AstEnumValue[] values = [.. value.GetProperty("values").EnumerateArray().Select(static member => new AstEnumValue(member.GetProperty("name").GetString()!, member.GetProperty("value").GetInt64()))];

        return new(values);
    }

    private static AstMacro ReadMacro(JsonElement value)
    {
        string name = value.GetProperty("name").GetString()!;
        string[] tokens = [.. value.GetProperty("tokens").EnumerateArray().Select(static token => token.GetString()!)];
        bool isFunctionLike = value.GetProperty("functionLike").GetBoolean();

        return new(name, tokens, isFunctionLike);
    }
}
