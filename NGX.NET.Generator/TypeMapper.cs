namespace NGX.NET.Generator;

internal class TypeMapper
{
    private static readonly HashSet<string> Acronyms = ["NGX", "DLSS", "DLSSD", "DLSSG", "DLAA", "DLISP", "CUDA", "D3D11", "D3D12", "VK", "UI", "ULL", "F", "D", "I", "API", "HDR", "SR", "RR", "RW", "VRAM"];
    private static readonly string[] FunctionGroups = ["D3D11", "D3D12", "CUDA", "VULKAN", "VK", "Parameter", "DLSSD", "DLSS"];

    private readonly Models models;
    private readonly Dictionary<string, string> unionNames = [];
    private readonly HashSet<string> inputRecords = [];
    private readonly HashSet<string> outputRecords = [];
    private readonly HashSet<string> allocatingRecords = [];
    private readonly HashSet<string> stableRecords = [];

    internal TypeMapper(Models models)
    {
        this.models = models;

        foreach (AstRecord parent in models.Records.Values)
        {
            foreach (AstField field in parent.Fields)
            {
                AstType type = field.Type;
                if (type.Kind is NativeTypeKind.Record && models.Records[type.Name].IsUnion)
                {
                    unionNames[type.Name] = TypeName(parent.Name) + "Union";
                }
            }
        }

        AnalyzeConversions();
    }

    internal string ManagedRecord(string name)
    {
        return unionNames.GetValueOrDefault(name, TypeName(name));
    }

    internal bool IsRecord(AstType type)
    {
        return type.Kind is NativeTypeKind.Record && !models.Records[type.Name].IsOpaque;
    }

    internal string? CallbackName(AstType type)
    {
        string cpp = type.Cpp.Replace("const ", "", StringComparison.Ordinal);

        return models.Aliases.ContainsKey(cpp) ? TypeName(cpp) : null;
    }

    internal string PublicType(AstType type)
    {
        NativeTypeKind kind = type.Kind;
        if (kind is NativeTypeKind.Bool)
        {
            return "bool";
        }

        if (kind is NativeTypeKind.Record)
        {
            return ManagedRecord(type.Name);
        }

        if (kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            if (CallbackName(type) is string callback)
            {
                return callback + "?";
            }

            AstType element = type.Element!;
            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                return "string?";
            }

            if (IsRecord(element))
            {
                return PublicType(element) + "?";
            }

            if (element.Kind is NativeTypeKind.Record && element.Name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter")
            {
                return TypeName(element.Name);
            }

            if (element.Kind is NativeTypeKind.ULongLong or NativeTypeKind.ULong)
            {
                return "ulong?";
            }

            return "nint";
        }

        return Type(type);
    }

    internal string PublicFieldType(string record, AstField field)
    {
        if (record is "NVSDK_NGX_PathListInfo" && field.Name is "Path")
        {
            return "string[]?";
        }

        if (record is "NVSDK_NGX_FeatureCommonInfo" && field.Name is "InternalData")
        {
            return "nint";
        }

        AstType type = field.Type;
        if (models.Records[record].IsUnion && IsRecord(type))
        {
            return PublicType(type) + "?";
        }

        if (MathFieldType(record, field) is string math)
        {
            return math.Replace("*", "?", StringComparison.Ordinal);
        }

        if (type.Kind is NativeTypeKind.ConstantArray)
        {
            AstType element = type.Element!;
            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU)
            {
                return "string?";
            }

            return PublicType(element) + "[]?";
        }

        return PublicType(type);
    }

    internal string Type(AstType type)
    {
        NativeTypeKind kind = type.Kind;
        string cpp = type.Cpp.Replace("const ", "", StringComparison.Ordinal).Trim();
        if (cpp is "size_t")
        {
            return "nuint";
        }

        if (cpp is "size_t *")
        {
            return "nuint*";
        }

        if (kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            AstType element = type.Element!;
            NativeTypeKind elementKind = element.Kind;
            if (elementKind is NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto)
            {
                return "nint";
            }

            if (elementKind is NativeTypeKind.Record && models.Records[element.Name].IsOpaque)
            {
                return "nint";
            }

            return (elementKind is NativeTypeKind.WChar ? "void" : Type(element)) + "*";
        }

        if (kind is NativeTypeKind.Enum or NativeTypeKind.Record)
        {
            return ManagedRecord(type.Name) + (kind is NativeTypeKind.Record ? "Native" : "");
        }

        return kind switch
        {
            NativeTypeKind.Void => "void",
            NativeTypeKind.Bool => "Bool8",
            NativeTypeKind.CharS or NativeTypeKind.CharU => "byte",
            NativeTypeKind.SChar => "sbyte",
            NativeTypeKind.UChar => "byte",
            NativeTypeKind.Short => "short",
            NativeTypeKind.UShort => "ushort",
            NativeTypeKind.Int => "int",
            NativeTypeKind.UInt => "uint",
            NativeTypeKind.Long => type.Size is 8 ? "long" : "int",
            NativeTypeKind.ULong => type.Size is 8 ? "ulong" : "uint",
            NativeTypeKind.LongLong => "long",
            NativeTypeKind.ULongLong => "ulong",
            NativeTypeKind.Float => "float",
            NativeTypeKind.Double => "double",
            NativeTypeKind.WChar => throw new InvalidOperationException("Unsupported native wchar_t value."),
            _ => throw new InvalidOperationException($"Unsupported native type: {type}")
        };
    }

    internal string NativeParameterType(AstParameter parameter)
    {
        AstType type = parameter.Type;

        if (parameter.Role is ParameterRole.RequiredName)
        {
            return "string";
        }

        if (parameter.Direction is ParameterDirection.Out)
        {
            AstType element = type.Element!;
            string output = Type(type)[..^1];

            if (element.Kind is NativeTypeKind.Pointer)
            {
                if (element.Element is { Kind: NativeTypeKind.Record, Name: "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle" } handle)
                {
                    output = ManagedRecord(handle.Name);
                }
                else if (element.Element!.Kind is NativeTypeKind.Void)
                {
                    output = "nint";
                }
            }

            return "out " + output;
        }

        if (type.Element is { Kind: NativeTypeKind.Record, Name: "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle" } input)
        {
            return ManagedRecord(input.Name);
        }

        return Type(type);
    }

    internal bool HasNativeRecord(string name)
    {
        return inputRecords.Contains(name) || outputRecords.Contains(name);
    }

    internal bool HasInputConversion(string name)
    {
        return inputRecords.Contains(name);
    }

    internal bool HasOutputConversion(string name)
    {
        return outputRecords.Contains(name) && !models.Records[name].IsUnion;
    }

    internal bool Allocates(string name)
    {
        return allocatingRecords.Contains(name);
    }

    internal bool RequiresScope(string name)
    {
        return allocatingRecords.Contains(name) || stableRecords.Contains(name);
    }

    private void AnalyzeConversions()
    {
        foreach (AstFunction function in models.Functions.Values)
        {
            AddRecord(outputRecords, function.Result);

            foreach (AstParameter parameter in function.Parameters)
            {
                AddRecord(parameter.Direction is ParameterDirection.Out ? outputRecords : inputRecords, parameter.Type);
            }
        }

        ExpandRecords(inputRecords);
        ExpandRecords(outputRecords);

        foreach (AstRecord record in models.Records.Values.Where(static record => !record.IsOpaque))
        {
            if (record.Name is "NVSDK_NGX_PathListInfo" || record.Fields.Any(field => FieldAllocates(record.Name, field)))
            {
                allocatingRecords.Add(record.Name);
            }

            if (record.Fields.Any(field => field.Type.Kind is NativeTypeKind.ConstantArray && MathFieldType(record.Name, field) is not null))
            {
                stableRecords.Add(record.Name);
            }
        }

        PropagateRequirements(allocatingRecords);
        PropagateRequirements(stableRecords);
    }

    private void AddRecord(HashSet<string> names, AstType type)
    {
        while (type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference or NativeTypeKind.ConstantArray)
        {
            type = type.Element!;
        }

        if (IsRecord(type))
        {
            names.Add(type.Name);
        }
    }

    private void ExpandRecords(HashSet<string> names)
    {
        int previous;
        do
        {
            previous = names.Count;

            foreach (string name in names.ToArray())
            {
                foreach (AstField field in models.Records[name].Fields)
                {
                    AddRecord(names, field.Type);
                }
            }
        }
        while (names.Count != previous);
    }

    private bool FieldAllocates(string record, AstField field)
    {
        AstType type = field.Type;

        while (type.Kind is NativeTypeKind.ConstantArray)
        {
            type = type.Element!;
        }

        if (type.Kind is not NativeTypeKind.Pointer)
        {
            return false;
        }

        AstType element = type.Element!;

        return CallbackName(type) is not null || IsRecord(element) || element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar or NativeTypeKind.ULongLong or NativeTypeKind.ULong || MathFieldType(record, field)?.EndsWith('*') is true;
    }

    private void PropagateRequirements(HashSet<string> names)
    {
        int previous;
        do
        {
            previous = names.Count;

            foreach (AstRecord record in models.Records.Values.Where(static record => !record.IsOpaque))
            {
                HashSet<string> dependencies = [];

                foreach (AstField field in record.Fields)
                {
                    AddRecord(dependencies, field.Type);
                }

                if (dependencies.Overlaps(names))
                {
                    names.Add(record.Name);
                }
            }
        }
        while (names.Count != previous);
    }

    internal static string[] CallbackArguments(string name)
    {
        if (name is "NVSDK_NGX_AppLogCallback")
        {
            return ["message", "loggingLevel", "sourceComponent"];
        }

        if (name.Contains("ProgressCallback", StringComparison.Ordinal))
        {
            return ["progress", "shouldCancel"];
        }

        if (name.Contains("_Parameter_", StringComparison.Ordinal))
        {
            return ["parameters", "name", "value"];
        }

        if (name.Contains("D3D12_ResourceAlloc", StringComparison.Ordinal))
        {
            return ["description", "state", "heap", "resource"];
        }

        if (name.Contains("D3D11_BufferAlloc", StringComparison.Ordinal))
        {
            return ["description", "buffer"];
        }

        if (name.Contains("D3D11_Tex2DAlloc", StringComparison.Ordinal))
        {
            return ["description", "texture"];
        }

        if (name.Contains("ResourceRelease", StringComparison.Ordinal))
        {
            return ["resource"];
        }

        if (name.Contains("GetCurrentSettings", StringComparison.Ordinal))
        {
            return ["handle", "parameters"];
        }

        if (name.Contains("EstimateVRAM", StringComparison.Ordinal))
        {
            return ["motionDepthWidth", "motionDepthHeight", "colorWidth", "colorHeight", "colorFormat", "motionFormat", "depthFormat", "hudlessFormat", "uiFormat", "estimatedBytes"];
        }

        if (name.Contains("GetStats", StringComparison.Ordinal) || name.Contains("GetOptimalSettings", StringComparison.Ordinal))
        {
            return ["parameters"];
        }

        throw new InvalidOperationException("Unclassified callback: " + name);
    }

    // PascalCase names cannot collide with C#'s lowercase keywords.

    internal static string Name(string name)
    {
        name = name.Replace("NVSDK_NGX_", "", StringComparison.Ordinal);

        return string.Concat(name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static p => Acronyms.Contains(p) || p.Any(char.IsLower) ? char.ToUpperInvariant(p[0]) + p[1..] : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(p.ToLowerInvariant())));
    }

    internal static string TypeName(string native)
    {
        string name = Name(native);

        return name.StartsWith("NGX", StringComparison.Ordinal) ? name : "NGX" + name;
    }

    internal static string NativeParameterName(string native)
    {
        string name = Name(native);
        // Preserve existing camelCase prefixes, such as pVRAMAllocatedBytes.
        name = char.IsLower(native[0]) ? char.ToLowerInvariant(name[0]) + name[1..] : JsonNamingPolicy.CamelCase.ConvertName(name);

        return name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => "@" + name,
            _ => name
        };
    }

    internal static string ParameterName(string native)
    {
        return NativeParameterName(ManagedName(native));
    }

    internal static string NativeFieldName(string native)
    {
        return Name(native);
    }

    internal static string EnumMember(string enumName, string nativeMember)
    {
        string prefix = enumName + "_";
        bool hasPrefix = nativeMember.StartsWith(prefix, StringComparison.Ordinal);
        string member = hasPrefix ? nativeMember[prefix.Length..] : nativeMember.Replace("NVSDK_NGX_", string.Empty, StringComparison.Ordinal);

        if (!hasPrefix)
        {
            string[] parts = nativeMember.Split('_');
            string normalized = enumName.Replace("_", string.Empty, StringComparison.Ordinal);
            for (int count = 1; count < parts.Length; count++)
            {
                if (string.Concat(parts.Take(count)).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                {
                    member = string.Join("_", parts.Skip(count));

                    break;
                }
            }
        }

        return EnumMember(member);
    }

    internal static (string Group, string Method) FunctionName(string native)
    {
        if (native is "GetNGXResultAsString")
        {
            return ("", "GetResultAsString");
        }

        string name = native.StartsWith("NVSDK_NGX_", StringComparison.Ordinal) ? native[10..] : native[4..];
        foreach (string prefix in FunctionGroups)
        {
            if (name.StartsWith(prefix + "_", StringComparison.Ordinal))
            {
                return (prefix is "VULKAN" or "VK" ? "Vulkan" : prefix, Name(name[(prefix.Length + 1)..]));
            }
        }

        return ("", Name(name));
    }

    internal static string EnumMember(string value)
    {
        if (value.Replace("_", "", StringComparison.Ordinal) is "VKIMAGEVIEW")
        {
            return "VkImageView";
        }

        if (value.Replace("_", "", StringComparison.Ordinal) is "VKBUFFER")
        {
            return "VkBuffer";
        }

        return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static part =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^[RGBADESX0-9]+$") && part.Any(char.IsDigit))
            {
                return part;
            }

            return string.Concat(System.Text.RegularExpressions.Regex.Matches(part, @"[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+").Select(static match => char.ToUpperInvariant(match.Value[0]) + match.Value[1..].ToLowerInvariant()));
        }));
    }

    internal static string PublicFieldName(string record, string field)
    {
        return (record, field) switch
        {
            ("NVSDK_NGX_PathListInfo", "Path") => "Paths",
            ("NVSDK_NGX_D3D11_DLSSD_Eval_Params" or "NVSDK_NGX_D3D12_DLSSD_Eval_Params", "InColorAfterDepthOfFieldSubtectBase") => "ColorAfterDepthOfFieldSubrectBase",
            _ => Name(ManagedName(field))
        };
    }

    internal static string? MathFieldType(string record, AstField field)
    {
        return record switch
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
    }
    private static string ManagedName(string native)
    {
        string name = Regex.Replace(native, @"^[pP]+(?=[A-Z_])_?", string.Empty);
        name = Regex.Replace(name, @"^(?:In|in|Out|out)(?=[A-Z_]|$)_?", string.Empty);

        return Regex.Replace(name, @"(?:Params|Cmd|Dev|Attrib|Buf|Exts|Ext|Res)(?=[A-Z0-9_]|$)", static match => match.Value switch
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
