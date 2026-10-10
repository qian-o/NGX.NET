namespace NGX.NET.Generator;

internal class EnumEmitter(Dictionary<string, string> files)
{
    internal void WriteEnum(string name, AstEnum value)
    {
        string managed = TypeName(name);
        AstEnumValue[] values = [.. value.Values];
        bool unsigned = name is "NVSDK_NGX_Result" || values.Any(static item => item.Value > int.MaxValue);
        bool flags = name is "NVSDK_NGX_DLSS_Feature_Flags" or "NVSDK_NGX_DLSSG_ResourceFlags" or "NVSDK_NGX_DLSSG_EvalFlags" or "NVSDK_NGX_Feature_Support_Result";
        bool hasNone = false;
        string prefix = name + "_";
        CodeWriter text = CreateFile();
        WriteSummary(text, name);

        if (flags)
        {
            text.Line("[Flags]");
        }

        text.BeginBlock($"public enum {managed} : {(unsigned ? "uint" : "int")}");

        foreach (AstEnumValue item in values)
        {
            string native = item.Name;
            bool hasPrefix = native.StartsWith(prefix, StringComparison.Ordinal);
            string member = hasPrefix ? native[prefix.Length..] : native.Replace("NVSDK_NGX_", string.Empty, StringComparison.Ordinal);
            if (!hasPrefix)
            {
                string[] parts = native.Split('_');
                string normalized = name.Replace("_", string.Empty, StringComparison.Ordinal);
                for (int count = 1; count < parts.Length; count++)
                {
                    if (string.Concat(parts.Take(count)).Equals(normalized, StringComparison.OrdinalIgnoreCase))
                    {
                        member = string.Join("_", parts.Skip(count));

                        break;
                    }
                }
            }

            member = EnumMember(member);
            hasNone |= member is "None";
            long numeric = item.Value;
            uint bits = unchecked((uint)numeric);
            string literal = unsigned ? $"{bits.ToString(CultureInfo.InvariantCulture)}u" : numeric.ToString(CultureInfo.InvariantCulture);
            if (flags && bits is not 0)
            {
                literal = string.Join(" | ", Enumerable.Range(0, 32).Where(bit => (bits & (1u << bit)) is not 0).Select(bit => $"{(unsigned ? "1u" : "1")} << {bit}"));
            }

            WriteSummary(text, native);
            text.Line($"{member} = {literal},");
            text.BlankLine();
        }

        if (flags && !hasNone)
        {
            WriteSummary(text, "No flags are set.");
            text.Line("None = 0");
        }

        text.EndBlock();
        files[$"Types/{managed}.g.cs"] = text.ToString();
    }
}
