namespace NGX.NET.Generator;

internal class ConstantsEmitter(AstModel model, Dictionary<string, string> files)
{
    internal void WriteConstants()
    {
        CodeWriter text = CreateFile();
        text.BeginBlock("public static unsafe partial class Ngx");

        foreach (AstMacro macro in model.Macros.Values.OrderBy(static value => value.Name, StringComparer.Ordinal))
        {
            if (macro.Name is "NVSDK_NGX_VERSION_API_MACRO")
            {
                text.WriteLine($"public const uint VersionAPI = {macro.Tokens[0].TrimEnd('u', 'U')}u;");
            }
            else if (macro.Name is "NVSDK_NGX_DLSS_DEBUG_OVERLAY_VALUE_UNSET")
            {
                text.WriteLine($"public const int DLSSDebugOverlayValueUnset = {string.Concat(macro.Tokens)};");
            }
        }

        text.WriteBlankLine();

        foreach (AstMacro macro in model.Macros.Values.OrderBy(static value => value.Name, StringComparer.Ordinal))
        {
            if (macro.IsFunctionLike || macro.Tokens.Length is 0 || !macro.Tokens.All(static token => token.StartsWith('"')))
            {
                continue;
            }

            string literal = GetStringLiteral(macro);
            text.WriteLine($"public static ReadOnlySpan<byte> {GetName(macro.Name)} => {literal};");
            text.WriteBlankLine();
        }

        WriteResultMacro(text, "NVSDK_NGX_FAILED", "==");
        WriteResultMacro(text, "NVSDK_NGX_SUCCEED", "!=");
        text.EndBlock();
        files["API/Constants.g.cs"] = text.ToString();
    }

    private void WriteResultMacro(CodeWriter text, string name, string operation)
    {
        AstMacro macro = model.Macros[name];
        string expected = $"(value)(((value)&0xFFF00000){operation}NVSDK_NGX_Result_Fail)";
        if (!macro.IsFunctionLike || string.Concat(macro.Tokens) != expected)
        {
            throw new InvalidOperationException($"Unsupported result macro: {name}.");
        }

        text.BeginBlock($"public static bool {name}(NGXResult value)");
        text.WriteLine($"return ((uint)value & 0xFFF00000u) {operation} (uint)NGXResult.Fail;");
        text.EndBlock();
    }

    private static string GetStringLiteral(AstMacro macro)
    {
        if (macro.Tokens.All(static token => !token.Contains('\\') && token.All(static character => character <= 127)))
        {
            return $"\"{string.Concat(macro.Tokens.Select(static token => token[1..^1]))}\\0\"u8";
        }

        List<byte> bytes = [];
        foreach (string token in macro.Tokens)
        {
            string content = token[1..^1];
            for (int i = 0; i < content.Length; i++)
            {
                char character = content[i];
                if (character is '\\')
                {
                    Match escape = Regex.Match(content[(i + 1)..], @"^x([0-9a-fA-F]+)");
                    if (!escape.Success)
                    {
                        throw new InvalidOperationException($"Unsupported native string escape: {macro.Name}.");
                    }

                    bytes.Add(byte.Parse(escape.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += escape.Length;
                }
                else
                {
                    bytes.Add(checked((byte)character));
                }
            }
        }

        bytes.Add(0);

        return $"[{string.Join(", ", bytes.Select(static value => $"0x{value:X2}"))}]";
    }
}
