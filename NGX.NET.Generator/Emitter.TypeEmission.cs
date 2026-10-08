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

        if (name is "NVSDK_NGX_DLSS_Feature_Flags" or "NVSDK_NGX_DLSSG_ResourceFlags" or "NVSDK_NGX_DLSSG_EvalFlags" or "NVSDK_NGX_Feature_Support_Result")
        {
            text.AppendLine("[Flags]");
        }

        text.AppendLine($"public enum {managed} : {(unsigned ? "uint" : "int")}\n{{");

        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                text.AppendLine();
            }

            JsonElement item = values[i];
            string native = item.Text("name");
            bool hasPrefix = native.StartsWith(prefix, StringComparison.Ordinal);
            string member = hasPrefix ? native[prefix.Length..] : native.Replace("NVSDK_NGX_", "", StringComparison.Ordinal);

            if (!hasPrefix)
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
            text.AppendLine($"    {EnumMember(member)} = {(unsigned ? unchecked((uint)item.GetProperty("value").GetInt64()).ToString(CultureInfo.InvariantCulture) : item.GetProperty("value").GetInt64().ToString(CultureInfo.InvariantCulture))},");
        }

        text.AppendLine("}");
        files[$"Types/{managed}.g.cs"] = text.ToString();
    }

    [GeneratedRegex(@"^-?\d+(?:\.\d+f)?$")]
    private static partial Regex NumericInitializerRegex();

    [GeneratedRegex(@"^\{[0 ,.f]+\}$")]
    private static partial Regex ZeroInitializerRegex();
}
