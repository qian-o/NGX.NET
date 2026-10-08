using System.Globalization;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private static readonly HashSet<string> acronyms = ["NGX", "DLSS", "DLSSD", "DLSSG", "DLAA", "DLISP", "CUDA", "D3D11", "D3D12", "VK", "UI", "ULL", "F", "D", "I", "API", "HDR", "SR", "RR", "RW", "VRAM"];
    private static readonly string[] functionGroups = ["D3D11", "D3D12", "CUDA", "VULKAN", "VK", "Parameter", "DLSSD", "DLSS"];

    // PascalCase names cannot collide with C#'s lowercase keywords.
    private static string Name(string name)
    {
        name = name.Replace("NVSDK_NGX_", "", StringComparison.Ordinal);

        return string.Concat(name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(p => acronyms.Contains(p) || p.Any(char.IsLower) ? char.ToUpperInvariant(p[0]) + p[1..] : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(p.ToLowerInvariant())));
    }

    private static string TypeName(string native)
    {
        string name = Name(native);

        return name.StartsWith("NGX", StringComparison.Ordinal) ? name : "NGX" + name;
    }

    private static string ParameterName(string native)
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

    private static (string Group, string Method) FunctionName(string native)
    {
        if (native == "GetNGXResultAsString")
        {
            return ("", "GetResultAsString");
        }

        string name = native.StartsWith("NVSDK_NGX_", StringComparison.Ordinal) ? native[10..] : native[4..];

        foreach (string prefix in functionGroups)
        {
            if (name.StartsWith(prefix + "_", StringComparison.Ordinal))
            {
                return (prefix is "VULKAN" or "VK" ? "Vulkan" : prefix, Name(name[(prefix.Length + 1)..]));
            }
        }

        return ("", Name(name));
    }
    private string ManagedRecord(string name) => unionNames.GetValueOrDefault(name, TypeName(name));

    private static string EnumMember(string value)
    {
        if (value.Replace("_", "", StringComparison.Ordinal) == "VKIMAGEVIEW") return "VkImageView";
        if (value.Replace("_", "", StringComparison.Ordinal) == "VKBUFFER") return "VkBuffer";
        return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(part =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^[RGBADESX0-9]+$") && part.Any(char.IsDigit)) return part;
            return string.Concat(System.Text.RegularExpressions.Regex.Matches(part, @"[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+")
                .Select(match => char.ToUpperInvariant(match.Value[0]) + match.Value[1..].ToLowerInvariant()));
        }));
    }
}
