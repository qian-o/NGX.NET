namespace NGX.NET.Generator;

internal class EnumEmitter(Dictionary<string, string> files)
{
    internal void WriteEnum(string name, AstEnum value)
    {
        string managed = GetTypeName(name);
        bool isUnsigned = name is "NVSDK_NGX_Result" || value.Values.Any(static item => item.Value > int.MaxValue);
        bool isFlagsEnum = name is "NVSDK_NGX_DLSS_Feature_Flags" or "NVSDK_NGX_DLSSG_ResourceFlags" or "NVSDK_NGX_DLSSG_EvalFlags" or "NVSDK_NGX_Feature_Support_Result";
        bool hasNone = false;
        CodeWriter text = CreateFile();

        if (isFlagsEnum)
        {
            text.WriteLine("[Flags]");
        }

        text.BeginBlock($"public enum {managed} : {(isUnsigned ? "uint" : "int")}");

        foreach (AstEnumValue item in value.Values)
        {
            string member = GetEnumMemberName(name, item.Name);
            hasNone |= member is "None";
            long numeric = item.Value;
            uint bits = unchecked((uint)numeric);
            string literal = isUnsigned ? $"{bits.ToString(CultureInfo.InvariantCulture)}u" : numeric.ToString(CultureInfo.InvariantCulture);
            if (name is "NVSDK_NGX_Result" && bits > byte.MaxValue)
            {
                literal = $"0x{bits:X8}u";
            }

            if (isFlagsEnum && bits is not 0)
            {
                literal = string.Join(" | ", Enumerable.Range(0, 32).Where(bit => (bits & (1u << bit)) is not 0).Select(bit => $"{(isUnsigned ? "1u" : "1")} << {bit}"));
            }

            text.WriteLine($"{member} = {literal},");
            text.WriteBlankLine();
        }

        if (isFlagsEnum && !hasNone)
        {
            text.WriteLine("None = 0");
        }

        text.EndBlock();
        files[$"Enums/{managed}.g.cs"] = text.ToString();
    }
}
