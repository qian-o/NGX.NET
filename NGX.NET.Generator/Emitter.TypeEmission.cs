using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private void WriteEnum(string name, JsonElement value)
    {
        string managed = TypeName(name);
        JsonElement[] values = [.. value.Items("values")];
        bool unsigned = name == "NVSDK_NGX_Result" || values.Any(v => v.GetProperty("value").GetInt64() > int.MaxValue);
        string prefix = name + "_";
        StringBuilder text = new(Header + Namespace + Summary(name));
        text.AppendLine($"[NGXNativeName(\"{name}\")]");
        text.AppendLine($"public enum {managed} : {(unsigned ? "uint" : "int")}\n{{");

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                text.AppendLine();
            }

            JsonElement item = values[i];
            string native = item.Text("name");
            string member = native.StartsWith(prefix, StringComparison.Ordinal) ? native[prefix.Length..] : native.Replace("NVSDK_NGX_", "", StringComparison.Ordinal);

            if (!native.StartsWith(prefix, StringComparison.Ordinal))
            {
                string[] parts = native.Split('_');
                string normalized = name.Replace("_", "", StringComparison.Ordinal);

                for (int count = 1; count < parts.Length; count++)
                {
                    if (string.Concat(parts.Take(count)).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        member = string.Join("_", parts.Skip(count));
                        break;
                    }
                }
            }

            text.Append(Summary(native, 4));
            text.AppendLine($"    [NGXNativeName(\"{native}\")]");
            text.AppendLine($"    {member.Replace("_", "", StringComparison.Ordinal)} = {(unsigned ? unchecked((uint)item.GetProperty("value").GetInt64()).ToString(CultureInfo.InvariantCulture) : item.GetProperty("value").GetInt64().ToString(CultureInfo.InvariantCulture))},");
        }

        text.AppendLine("}");
        files[$"Types/{managed}.g.cs"] = text.ToString();
    }

    private void WriteRecord(string name, JsonElement value)
    {
        bool opaque = value.GetProperty("opaque").GetBoolean();

        if (opaque && !name.StartsWith("NVSDK_NGX_", StringComparison.Ordinal))
        {
            return;
        }

        string managed = unionNames.GetValueOrDefault(name, TypeName(name));
        JsonElement[] fields = [.. value.Items("fields")];
        bool hasNumerics = fields.Any(field => MathFieldType(name, field) is not null);
        bool hasInlineArrays = false;
        StringBuilder text = new(Summary(opaque ? name + ". Opaque native object; pass only pointers returned by NGX." : name));
        text.AppendLine($"[NGXNativeName(\"{name}\")]");
        text.AppendLine(opaque ? "[StructLayout(LayoutKind.Sequential)]" : $"[StructLayout(LayoutKind.Explicit, Size = {value.Number("size")})]");
        text.AppendLine($"public unsafe partial struct {managed}\n{{");

        for (int i = 0; i < fields.Length; i++)
        {
            if (i > 0)
            {
                text.AppendLine();
            }

            JsonElement field = fields[i];
            JsonElement type = field.GetProperty("type");
            string fieldName = field.Text("name");
            text.Append(Summary($"{name}::{fieldName}", 4));
            text.AppendLine($"    [NGXNativeName(\"{fieldName}\")]");
            text.AppendLine($"    [FieldOffset({field.Number("offset") / 8})]");

            if (MathFieldType(name, field) is string mathType)
            {
                text.AppendLine($"    public {mathType} {FieldName(fieldName)};");
            }
            else if (type.Text("kind") == "CONSTANTARRAY")
            {
                int count = 1;

                while (type.Text("kind") == "CONSTANTARRAY")
                {
                    count *= type.Number("count");
                    type = type.GetProperty("element");
                }

                string element = Type(type);

                if (element is "float" or "double" or "int" or "uint" or "sbyte" or "byte" or "long" or "ulong" or "short" or "ushort")
                {
                    text.AppendLine($"    public fixed {element} {FieldName(fieldName)}[{count}];");
                }
                else
                {
                    hasInlineArrays = true;

                    if (element.EndsWith('*'))
                    {
                        element = "NGXPointer<" + element[..^1] + ">";
                    }

                    string arrayName = TypeName(fieldName) + "Buffer";
                    text.AppendLine($"    public {arrayName} {FieldName(fieldName)};");
                    text.AppendLine();
                    text.Append(Summary($"Inline storage for {count} native elements.", 4));
                    text.AppendLine($"    [InlineArray({count})]\n    public struct {arrayName}\n    {{\n        private {element} element;\n    }}");
                }
            }
            else
            {
                text.AppendLine($"    public {Type(type)} {FieldName(fieldName)};");
            }
        }

        List<(string Field, string Value)> initializers = [];

        foreach (JsonElement field in fields)
        {
            if (!field.TryGetProperty("declaration", out JsonElement declaration) || !declaration.GetString()!.Contains('='))
            {
                continue;
            }

            string expression = declaration.GetString()!.Split('=', 2)[1].Trim();

            if (Regex.IsMatch(expression, @"^-?\d+(?:\.\d+f)?$"))
            {
                initializers.Add((FieldName(field.Text("name")), expression));
            }
            else if (!Regex.IsMatch(expression, @"^\{[0 ,.f]+\}$"))
            {
                throw new InvalidOperationException("Unreviewed native initializer: " + expression);
            }
        }

        if (initializers.Count > 0)
        {
            text.AppendLine();
            text.Append(Summary("Initializes the defaults declared by the native SDK.", 4));
            text.AppendLine($"    public {managed}()\n    {{\n        this = default;\n");

            foreach ((string field, string expression) in initializers)
            {
                text.AppendLine($"        {field} = {expression};");
            }

            text.AppendLine("    }");
        }

        text.AppendLine("}");
        string numerics = hasNumerics ? "using System.Numerics;\n" : "";
        string compilerServices = hasInlineArrays ? "using System.Runtime.CompilerServices;\n" : "";
        files[$"Types/{managed}.g.cs"] = Header + numerics + compilerServices + "using System.Runtime.InteropServices;\n\n" + Namespace + text;
    }

    private void WriteCallbacks()
    {
        Dictionary<string, JsonElement> aliases = platforms.SelectMany(p => p.GetProperty("aliases").EnumerateObject()).GroupBy(p => p.Name).ToDictionary(g => g.Key, g => g.First().Value);

        foreach ((string native, JsonElement type) in aliases)
        {
            if (type.Text("kind") != "POINTER" || type.GetProperty("element").Text("kind") != "FUNCTIONPROTO")
            {
                continue;
            }

            string name = TypeName(native);
            string pointer = Type(type);
            files[$"Callbacks/{name}.g.cs"] = Header + "using System.Runtime.InteropServices;\n\n" + Namespace + Summary(native + ". Keep callback code alive while NGX retains it; never let managed exceptions cross this ABI.") +
                $"[NGXNativeName(\"{native}\")]\n[StructLayout(LayoutKind.Sequential)]\npublic readonly unsafe struct {name}({pointer} pointer)\n{{\n" +
                Summary("Native C callback pointer. C++ reference parameters use their pointer ABI.", 4) +
                $"    public readonly {pointer} Pointer = pointer;\n}}\n";
        }
    }
}
