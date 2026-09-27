namespace Streamline.NET.Generator;

internal sealed class TypeMapper(InterfaceSnapshot snapshot)
{
    private readonly HashSet<string> knownTypes = [.. AllDeclarations(snapshot.Declarations)
        .Where(declaration => declaration.IsRecord || declaration.Kind == "ENUM_DECL")
        .Select(declaration => declaration.QualifiedName)];

    private readonly HashSet<string> opaqueTypes = [.. snapshot.Declarations
        .Where(declaration => declaration.IsRecord && !declaration.Definition)
        .Select(declaration => declaration.QualifiedName)];

    private static IEnumerable<NativeDeclaration> AllDeclarations(IEnumerable<NativeDeclaration> declarations)
    {
        foreach (NativeDeclaration declaration in declarations)
        {
            yield return declaration;
            foreach (NativeDeclaration child in AllDeclarations(declaration.Children))
            {
                yield return child;
            }
        }
    }

    public string Map(NativeType type)
    {
        if (type.Spelling is "size_t" or "std::size_t")
        {
            return "nuint";
        }

        if (type.Spelling is "intptr_t" or "uintptr_t")
        {
            return type.Spelling == "intptr_t" ? "nint" : "nuint";
        }

        return type.Kind switch
        {
            "VOID" => "void",
            "BOOL" => "Bool8",
            "CHAR_S" or "SCHAR" => "sbyte",
            "CHAR_U" or "UCHAR" => "byte",
            "SHORT" => "short",
            "USHORT" => "ushort",
            "WCHAR" or "CHAR16" when type.Size == 2 => "char",
            "INT" or "LONG" when type.Size == 4 => "int",
            "UINT" or "ULONG" when type.Size == 4 => "uint",
            "LONGLONG" or "LONG" when type.Size == 8 => "long",
            "ULONGLONG" or "ULONG" when type.Size == 8 => "ulong",
            "FLOAT" => "float",
            "DOUBLE" => "double",
            "RECORD" or "ENUM" => MapNamed(type),
            "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE" => MapPointer(type),
            "FUNCTIONPROTO" or "FUNCTIONNOPROTO" => MapFunction(type),
            _ => throw new InvalidDataException($"Unsupported native type: {type.Spelling} ({type.Kind}, size {type.Size}).")
        };
    }

    private string MapNamed(NativeType type)
    {
        string name = type.Declaration ?? type.Canonical;

        if (name is "VkStructureType" or "VkResult")
        {
            return "int";
        }

        if (name.StartsWith("sl::Array<", StringComparison.Ordinal))
        {
            return "SLArray<" + TypeName(name[10..^1].Trim()) + ">";
        }

        if (!knownTypes.Contains(name))
        {
            throw new InvalidDataException($"Missing native definition: {name}");
        }

        return TypeName(name);
    }

    private string MapPointer(NativeType type)
    {
        NativeType element = type.Element ?? throw new InvalidDataException($"Missing pointee: {type.Spelling}");

        if (element.Kind is "FUNCTIONPROTO" or "FUNCTIONNOPROTO")
        {
            return MapFunction(element);
        }

        string name = element.Declaration ?? element.Canonical;

        if (name is "sl::FrameToken" or "sl::IAllocator" || opaqueTypes.Contains(name))
        {
            return "nint";
        }

        return Map(element) + "*";
    }

    private string MapFunction(NativeType type)
    {
        if (type.CallingConvention is not (1 or 10))
        {
            throw new InvalidDataException($"Unresolved calling convention {type.CallingConvention}: {type.Spelling}");
        }

        IEnumerable<string> arguments = type.Parameters.Select(Map).Append(Map(type.Result
            ?? throw new InvalidDataException($"Missing return type: {type.Spelling}")));
        return "delegate* unmanaged[Cdecl]<" + string.Join(", ", arguments) + ">";
    }

    public static string TypeName(string name)
    {
        name = name.Replace("sl::", "", StringComparison.Ordinal).Replace("::", ".", StringComparison.Ordinal);

        return name switch
        {
            "Result" => "SLResult",
            "Boolean" => "SLBoolean",
            "Version" => "SLVersion",
            "Array" => "SLArray",
            "uint2" => "UInt2",
            "uint3" => "UInt3",
            "float2" => "Float2",
            "float3" => "Float3",
            "float4" => "Float4",
            "float4x4" => "Float4x4",
            "tagRECT" => "Rect",
            "_LUID" => "Luid",
            "DXGI_FORMAT" => "DXGIFormat",
            _ => PascalCase(name)
        };
    }

    public static string PascalCase(string name)
    {
        return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name[1..];
    }

    public static string MemberName(string name)
    {
        return name switch
        {
            "INVALID_FLOAT" => "InvalidFloat",
            "INVALID_UINT" => "InvalidUInt",
            "MAX_FRAMES_IN_FLIGHT" => "MaxFramesInFlight",
            "sType" => "SType",
            "pNext" => "PNext",
            _ => PascalCase(name.Replace("_", "", StringComparison.Ordinal))
        };
    }

    public static string EnumMember(string name)
    {
        if (name.StartsWith("DXGI_FORMAT_", StringComparison.Ordinal))
        {
            string member = string.Concat(name[12..].Split('_').Select(part => part switch
            {
                "TYPELESS" => "Typeless", "FLOAT" => "Float", "UINT" => "UInt", "SINT" => "SInt",
                "UNORM" => "Unorm", "SNORM" => "Snorm", "SRGB" => "Srgb", "UNKNOWN" => "Unknown",
                "FORCE" => "Force", "BIAS" => "Bias", "SHARED" => "Shared", "EXP" => "Exp",
                "OPAQUE" => "Opaque", "SAMPLER" => "Sampler", "FEEDBACK" => "Feedback",
                "MIN" => "Min", "MIP" => "Mip", "REGION" => "Region", "USED" => "Used",
                _ => part
            }));
            return char.IsDigit(member[0]) ? "Format" + member : member;
        }
        name = name.Length > 1 && name[0] == 'e' && char.IsUpper(name[1]) ? name[1..] : PascalCase(name);
        return name.Replace("_", "", StringComparison.Ordinal);
    }

    public static string ConstantName(string name)
    {
        name = name.StartsWith('k') ? name[1..] : MemberName(name);
        return name.Replace("_INVALID", "Invalid", StringComparison.Ordinal).Replace("_", "", StringComparison.Ordinal);
    }

    public static string Identifier(string name)
    {
        return name is "base" or "event" or "params" or "ref" or "in" or "out" or "object"
            or "string" or "internal" or "fixed" or "value" or "lock" ? "@" + name : name;
    }

    public static string Group(NativeDeclaration declaration)
    {
        string file = Path.GetFileNameWithoutExtension(declaration.File);
        return file switch
        {
            "sl_dlss" => "DLSS",
            "sl_dlss_d" => "DLSSD",
            "sl_dlss_g" => "DLSSG",
            "sl_deepdvc" => "DeepDVC",
            "sl_directsr" => "DirectSR",
            "sl_nis" => "NIS",
            "sl_reflex" => "Reflex",
            "sl_pcl" => "PCL",
            "sl_nvperf" => "NvPerf",
            "sl_helpers_vk" => "Vulkan",
            "sl_security" => "Security",
            "sl_hooks" => "Hooks",
            _ when declaration.QualifiedName.StartsWith("Vk", StringComparison.Ordinal) => "Vulkan",
            _ => "Core"
        };
    }
}
