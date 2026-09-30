using System.Text;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private void WriteFunctions(string group, IEnumerable<JsonElement> functionsInGroup)
    {
        StringBuilder text = new(Header + "using System.Runtime.CompilerServices;\nusing System.Runtime.InteropServices;\n\n" + Namespace + "public static unsafe partial class NGX\n{\n");
        int indent = group.Length == 0 ? 4 : 8;
        string spaces = new(' ', indent);

        if (group.Length != 0)
        {
            text.Append(Summary($"{group} application API and native inline helpers.", 4));
            text.AppendLine($"    public static partial class {group}\n    {{");
        }

        JsonElement[] ordered = [.. functionsInGroup.OrderBy(f => f.Text("name"), StringComparer.Ordinal)];

        for (int i = 0; i < ordered.Length; i++)
        {
            if (i > 0)
            {
                text.AppendLine();
            }

            JsonElement function = ordered[i];
            string native = function.Text("name");
            string method = FunctionName(native).Method;
            string result = Type(function.GetProperty("result"));
            JsonElement[] parameters = [.. function.Items("parameters")];
            string args = string.Join(", ", parameters.Select(p => Type(p.GetProperty("type")) + " " + ParameterName(p.Text("name"))));
            text.Append(Summary($"{native}. Source: {function.Text("header")}:{function.Number("line")}.", indent));
            text.AppendLine($"{spaces}[LibraryImport(LibraryName, EntryPoint = \"{function.Text("export")}\")]");
            text.AppendLine($"{spaces}[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            text.AppendLine($"{spaces}public static partial {result} {method}({args});");
        }

        if (group.Length != 0)
        {
            text.AppendLine("    }");
        }

        text.AppendLine("}");
        files[$"API/{(group.Length == 0 ? "Core" : group)}.g.cs"] = text.ToString();
    }

    private void WriteConstants()
    {
        StringBuilder text = new(Header + Namespace + "public static unsafe partial class NGX\n{\n");
        Dictionary<string, JsonElement> macros = Merge("macros");
        bool hasMembers = false;

        foreach (JsonElement macro in macros.Values.OrderBy(m => m.Text("name"), StringComparer.Ordinal))
        {
            string native = macro.Text("name");
            string[] tokens = [.. macro.Items("tokens").Select(t => t.GetString()!)];

            if (macro.GetProperty("functionLike").GetBoolean() || tokens.Length == 0)
            {
                continue;
            }

            string? declaration = tokens.All(t => t.StartsWith('"'))
                ? $"public const string {Name(native)} = {string.Join(" + ", tokens)};"
                : native switch
                {
                    "NVSDK_NGX_VERSION_API_MACRO" => $"public const uint VersionAPI = {tokens[0]};",
                    "NVSDK_NGX_DLSS_DEBUG_OVERLAY_VALUE_UNSET" => $"public const int DLSSDebugOverlayValueUnset = {string.Concat(tokens)};",
                    _ => null
                };

            if (declaration is null)
            {
                continue;
            }

            if (hasMembers)
            {
                text.AppendLine();
            }

            hasMembers = true;
            text.Append(Summary(native, 4));
            text.AppendLine($"    {declaration}");
        }

        text.AppendLine("}");
        files["API/Constants.g.cs"] = text.ToString();
    }
}
