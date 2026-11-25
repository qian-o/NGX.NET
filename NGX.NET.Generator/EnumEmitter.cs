using System.Numerics;

namespace NGX.NET.Generator;

internal class EnumEmitter(Dictionary<string, string> files)
{
    internal void WriteEnum(string name, AstEnum value)
    {
        string managed = GetTypeName(name);
        bool isUnsigned = name is "NVSDK_NGX_Result" || value.Values.Any(static item => item.Value > int.MaxValue);
        bool isFlagsEnum = name is "NVSDK_NGX_DLSS_Feature_Flags" or "NVSDK_NGX_DLSSG_ResourceFlags" or "NVSDK_NGX_DLSSG_EvalFlags" or "NVSDK_NGX_Feature_Support_Result";
        bool hasZero = value.Values.Any(static item => item.Value is 0);
        bool hasNone = value.Values.Any(item => GetEnumMemberName(name, item.Name) is "None");
        ValidateNames(value.Values.Select(item => (item.Name, GetEnumMemberName(name, item.Name))), $"enum members in {name}");
        CodeWriter text = CreateFile();

        if (isFlagsEnum)
        {
            text.WriteLine("[Flags]");
        }

        text.BeginBlock($"public enum {managed} : {(isUnsigned ? "uint" : "int")}");

        if (isFlagsEnum && !hasZero && !hasNone)
        {
            text.WriteLine($"None = {(isUnsigned ? "0u" : "0")},");
            text.WriteBlankLine();
        }

        foreach (AstEnumValue item in value.Values)
        {
            string member = GetEnumMemberName(name, item.Name);
            long numeric = item.Value;
            uint bits = unchecked((uint)numeric);
            string literal = isUnsigned ? $"{bits.ToString(CultureInfo.InvariantCulture)}u" : numeric.ToString(CultureInfo.InvariantCulture);
            if (name is "NVSDK_NGX_Result" && bits > byte.MaxValue)
            {
                literal = $"0x{bits:X8}u";
            }

            if (isFlagsEnum && bits is not 0)
            {
                int count = BitOperations.PopCount(bits);
                literal = count <= 8
                    ? string.Join(" | ", Enumerable.Range(0, 32).Where(bit => (bits & (1u << bit)) is not 0).Select(bit => count > 1 ? $"({(isUnsigned ? "1u" : "1")} << {bit})" : $"{(isUnsigned ? "1u" : "1")} << {bit}"))
                    : $"0x{bits:X8}{(isUnsigned ? "u" : string.Empty)}";
            }

            text.WriteLine($"{member} = {literal},");
            text.WriteBlankLine();
        }

        text.EndBlock();
        files.Add($"Enums/{managed}.g.cs", text.ToString());
    }
}
