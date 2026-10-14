namespace NGX.NET.Generator;

internal class TypeMapper
{
    private static readonly HashSet<string> Acronyms = ["NGX", "DLSS", "DLSSD", "DLSSG", "DLAA", "DLISP", "CUDA", "D3D11", "D3D12", "VK", "UI", "ULL", "F", "D", "I", "API", "HDR", "SR", "RR", "RW", "VRAM"];
    private static readonly string[] FunctionGroups = ["D3D11", "D3D12", "CUDA", "VULKAN", "VK", "Parameter", "DLSSD", "DLSS"];
    // Restore word boundaries lost in uppercase native tokens and correct the SDK's Attrib typo.
    private static readonly Dictionary<string, string> NativeWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SHADINGMODELID"] = "ShadingModelId",
        ["MATERIALID"] = "MaterialId",
        ["ONEOVERLUMA"] = "OneOverLuma",
        ["IMAGEVIEW"] = "ImageView",
        ["FPENCODING"] = "FpEncoding",
        ["Atrrib"] = "Attrib"
    };

    private readonly AstModel model;
    private readonly Dictionary<string, string> unionNames = [];

    internal TypeMapper(AstModel model)
    {
        this.model = model;

        foreach (AstRecord parent in model.Records.Values)
        {
            foreach (AstField field in parent.Fields)
            {
                if (field.Type.Kind is NativeTypeKind.Record && model.Records[field.Type.Name].IsUnion)
                {
                    unionNames[field.Type.Name] = $"{GetTypeName(parent.Name)}Union";
                }
            }
        }
    }

    internal string GetRecordName(string name)
    {
        return unionNames.GetValueOrDefault(name, GetTypeName(name));
    }

    internal bool IsRecord(AstType type)
    {
        return type.Kind is NativeTypeKind.Record && !model.Records[type.Name].IsOpaque;
    }

    internal string GetFieldType(string record, AstField field)
    {
        if (GetMathFieldType(record, field) is string math)
        {
            return math;
        }

        return field.Type.Kind switch
        {
            NativeTypeKind.Bool => "Bool8",
            NativeTypeKind.ConstantArray => $"{GetFieldName(record, field.Name)}Buffer",
            _ => GetManagedType(field.Type)
        };
    }

    internal string GetManagedType(AstType type)
    {
        string cpp = type.Cpp.Replace("const ", string.Empty, StringComparison.Ordinal).Trim();
        if (cpp is "size_t")
        {
            return "nuint";
        }

        NativeTypeKind kind = type.Kind;
        if (kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            AstType element = type.Element!;
            if (element.Kind is NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto)
            {
                string arguments = string.Join(", ", element.Arguments.Select(GetManagedType).Append(GetManagedType(element.Result!)));

                return $"delegate* unmanaged[Cdecl]<{arguments}>";
            }

            if (element.Kind is NativeTypeKind.Record && model.Records[element.Name].IsOpaque)
            {
                return element.Name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter" ? GetRecordName(element.Name) : "nint";
            }

            return $"{(element.Kind is NativeTypeKind.WChar ? "void" : GetManagedType(element))}*";
        }

        if (kind is NativeTypeKind.Enum or NativeTypeKind.Record)
        {
            return GetRecordName(type.Name);
        }

        return kind switch
        {
            NativeTypeKind.Void => "void",
            NativeTypeKind.Bool => "byte",
            NativeTypeKind.CharS or NativeTypeKind.CharU => "byte",
            NativeTypeKind.SChar => "sbyte",
            NativeTypeKind.UChar => "byte",
            NativeTypeKind.Short => "short",
            NativeTypeKind.UShort => "ushort",
            NativeTypeKind.Int => "int",
            NativeTypeKind.UInt => "uint",
            NativeTypeKind.Long => type.SizeInBytes is 8 ? "long" : "int",
            NativeTypeKind.ULong => type.SizeInBytes is 8 ? "ulong" : "uint",
            NativeTypeKind.LongLong => "long",
            NativeTypeKind.ULongLong => "ulong",
            NativeTypeKind.Float => "float",
            NativeTypeKind.Double => "double",
            NativeTypeKind.WChar => throw new InvalidOperationException("Unsupported native wchar_t value."),
            _ => throw new InvalidOperationException($"Unsupported native type: {type.Cpp}.")
        };
    }

    internal static string GetName(string name)
    {
        name = name.Replace("NVSDK_NGX_", string.Empty, StringComparison.Ordinal);

        return string.Concat(name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static part => NativeWords.GetValueOrDefault(part, Acronyms.Contains(part) || part.Any(char.IsLower) ? $"{char.ToUpperInvariant(part[0])}{part[1..]}" : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(part.ToLowerInvariant()))));
    }

    internal static string GetTypeName(string native)
    {
        string name = GetName(native);

        return name.StartsWith("NGX", StringComparison.Ordinal) ? name : $"NGX{name}";
    }

    internal static string GetParameterName(string native)
    {
        native = NormalizeName(native);
        string name = GetName(native);

        // Preserve existing camelCase prefixes, such as pVRAMAllocatedBytes.
        name = char.IsLower(native[0]) ? $"{char.ToLowerInvariant(name[0])}{name[1..]}" : JsonNamingPolicy.CamelCase.ConvertName(name);

        return name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => $"@{name}",
            _ => name
        };
    }

    internal static string GetEnumMemberName(string enumName, string nativeMember)
    {
        const string TypeSuffix = "Type";
        string normalized = enumName.Replace("_", string.Empty, StringComparison.Ordinal);
        string withoutType = normalized.EndsWith(TypeSuffix, StringComparison.Ordinal) ? normalized[..^TypeSuffix.Length] : normalized;
        string[] parts = nativeMember.Split('_');

        // Native member prefixes may omit the enum's Type suffix. Match at underscore boundaries and retain a member name.
        for (int i = parts.Length - 1; i > 0; i--)
        {
            string prefix = string.Concat(parts.Take(i));
            if (prefix.Equals(normalized, StringComparison.OrdinalIgnoreCase) || prefix.Equals(withoutType, StringComparison.OrdinalIgnoreCase))
            {
                return GetEnumMemberName(string.Join("_", parts.Skip(i)));
            }
        }

        return GetEnumMemberName(nativeMember.Replace("NVSDK_NGX_", string.Empty, StringComparison.Ordinal));
    }

    internal static (string Group, string Method) GetFunctionName(string native)
    {
        if (native is "GetNGXResultAsString")
        {
            return (string.Empty, "GetResultAsString");
        }

        string name = native.StartsWith("NVSDK_NGX_", StringComparison.Ordinal) ? native[10..] : native[4..];
        foreach (string prefix in FunctionGroups)
        {
            if (name.StartsWith($"{prefix}_", StringComparison.Ordinal))
            {
                return (prefix is "VULKAN" or "VK" ? "Vulkan" : prefix, GetName(name[(prefix.Length + 1)..]));
            }
        }

        return (string.Empty, GetName(name));
    }

    internal static string GetEnumMemberName(string value)
    {
        return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static part =>
        {
            if (NativeWords.TryGetValue(part, out string? word))
            {
                return word;
            }

            if (Regex.IsMatch(part, @"^[RGBADESX0-9]+$") && part.Any(char.IsDigit))
            {
                return part;
            }

            return string.Concat(Regex.Matches(part, @"[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+").Select(static match => $"{char.ToUpperInvariant(match.Value[0])}{match.Value[1..].ToLowerInvariant()}"));
        }));
    }

    internal static string GetFieldName(string record, string field)
    {
        return (record, field) switch
        {
            ("NVSDK_NGX_D3D11_DLSSD_Eval_Params" or "NVSDK_NGX_D3D12_DLSSD_Eval_Params", "InColorAfterDepthOfFieldSubtectBase") => "ColorAfterDepthOfFieldSubrectBase",
            _ => GetName(NormalizeName(field))
        };
    }

    internal static string? GetMathFieldType(string record, AstField field)
    {
        string? math = record switch
        {
            "NVSDK_NGX_DLSSG_Opt_Eval_Params" => field.Name switch
            {
                "cameraViewToClip" or "clipToCameraView" or "clipToLensClip" or "clipToPrevClip" or "prevClipToClip" => "Matrix4x4",
                "jitterOffset" or "mvecScale" or "cameraPinholeOffset" => "Vector2",
                "cameraPos" or "cameraUp" or "cameraRight" or "cameraFwd" => "Vector3",
                _ => null
            },
            "NVSDK_NGX_CUDA_DLSSD_Eval_Params" or "NVSDK_NGX_D3D11_DLSSD_Eval_Params" or "NVSDK_NGX_D3D12_DLSSD_Eval_Params" or "NVSDK_NGX_VK_DLSSD_Eval_Params" when field.Name is "pInWorldToViewMatrix" or "pInViewToClipMatrix" => "Matrix4x4*",
            _ => null
        };
        bool isValid = math switch
        {
            null => true,
            "Matrix4x4" => field.Type is { Kind: NativeTypeKind.ConstantArray, Count: 4, SizeInBytes: 64, Element: { Kind: NativeTypeKind.ConstantArray, Count: 4, SizeInBytes: 16, Element: { Kind: NativeTypeKind.Float, SizeInBytes: 4 } } },
            "Vector2" => field.Type is { Kind: NativeTypeKind.ConstantArray, Count: 2, SizeInBytes: 8, Element: { Kind: NativeTypeKind.Float, SizeInBytes: 4 } },
            "Vector3" => field.Type is { Kind: NativeTypeKind.ConstantArray, Count: 3, SizeInBytes: 12, Element: { Kind: NativeTypeKind.Float, SizeInBytes: 4 } },
            "Matrix4x4*" => field.Type is { Kind: NativeTypeKind.Pointer, SizeInBytes: 8, Element: { Kind: NativeTypeKind.Float, SizeInBytes: 4 } },
            _ => false
        };
        if (!isValid)
        {
            throw new InvalidOperationException($"Unsupported native shape for {record}.{field.Name}: {math} requires its exact native float layout.");
        }

        return math;
    }

    private static string NormalizeName(string native)
    {
        string name = Regex.Replace(native, @"^[pP]+(?=[A-Z_])_?", string.Empty);
        name = Regex.Replace(name, @"^(?:In|in|Out|out)(?=[A-Z_]|$)_?", string.Empty);

        return Regex.Replace(name, @"(?:Params|Cmd|Dev(?=Ctx)|Attrib|Buf|Exts|Ext|Res)(?=[A-Z0-9_]|$)", static match => match.Value switch
        {
            "Params" => "Parameters",
            "Cmd" => "Command",
            "Dev" => "Device",
            "Attrib" => "Attributes",
            "Buf" => "Buffer",
            "Ext" => "Extension",
            "Exts" => "Extensions",
            "Res" => "Resolution",
            _ => match.Value
        });
    }
}
