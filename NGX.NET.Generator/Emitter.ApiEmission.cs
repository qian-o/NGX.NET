using System.Text;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private void WriteConstants()
    {
        StringBuilder text = new(Header + Namespace + "public static unsafe partial class Ngx\n{\n");
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

            string? declaration = tokens.All(t => t.StartsWith('"')) ? $"public const string {Name(native)} = {string.Join(" + ", tokens)};" : native switch
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
