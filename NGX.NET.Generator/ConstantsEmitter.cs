namespace NGX.NET.Generator;

internal class ConstantsEmitter(Models models, Dictionary<string, string> files)
{
    internal void WriteConstants()
    {
        CodeWriter text = CreateFile();
        text.BeginBlock("public static unsafe partial class Ngx");

        Dictionary<string, AstMacro> macros = models.Macros;
        foreach (AstMacro macro in macros.Values.OrderBy(static value => value.Name, StringComparer.Ordinal))
        {
            string native = macro.Name;
            string[] tokens = [.. macro.Tokens];
            if (macro.IsFunctionLike || tokens.Length is 0)
            {
                continue;
            }

            string? declaration = native switch
            {
                _ when tokens.All(static token => token.StartsWith('"')) => $"public const string {Name(native)} = {string.Join(" + ", tokens)};",
                "NVSDK_NGX_VERSION_API_MACRO" => $"public const uint VersionAPI = {tokens[0]};",
                "NVSDK_NGX_DLSS_DEBUG_OVERLAY_VALUE_UNSET" => $"public const int DLSSDebugOverlayValueUnset = {string.Concat(tokens)};",
                _ => null
            };
            if (declaration is null)
            {
                continue;
            }

            WriteSummary(text, native);
            text.Line(declaration);
            text.BlankLine();
        }

        text.EndBlock();
        files["API/Constants.g.cs"] = text.ToString();
    }
}
