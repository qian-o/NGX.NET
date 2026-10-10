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
        CodeWriter text = CreateFile();

        if (flags)
        {
            text.Line("[Flags]");
        }

        text.BeginBlock($"public enum {managed} : {(unsigned ? "uint" : "int")}");

        foreach (AstEnumValue item in values)
        {
            string member = EnumMember(name, item.Name);
            hasNone |= member is "None";
            long numeric = item.Value;
            uint bits = unchecked((uint)numeric);
            string literal = unsigned ? $"{bits.ToString(CultureInfo.InvariantCulture)}u" : numeric.ToString(CultureInfo.InvariantCulture);
            if (name is "NVSDK_NGX_Result" && bits > byte.MaxValue)
            {
                literal = $"0x{bits:X8}u";
            }

            if (flags && bits is not 0)
            {
                literal = string.Join(" | ", Enumerable.Range(0, 32).Where(bit => (bits & (1u << bit)) is not 0).Select(bit => $"{(unsigned ? "1u" : "1")} << {bit}"));
            }

            text.Line($"{member} = {literal},");
            text.BlankLine();
        }

        if (flags && !hasNone)
        {
            text.Line("None = 0");
        }

        text.EndBlock();
        files[$"Enums/{managed}.g.cs"] = text.ToString();
    }
}
