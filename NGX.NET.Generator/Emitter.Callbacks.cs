using System.Text;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private (string Type, string Modifier, string Attribute) CallbackParameter(JsonElement type)
    {
        if (type.Text("kind") is "POINTER" or "LVALUEREFERENCE")
        {
            JsonElement element = type.GetProperty("element");
            string kind = element.Text("kind");
            if (kind is "CHAR_S" or "CHAR_U") return ("string?", "", "[MarshalAs(UnmanagedType.LPUTF8Str)] ");
            if (kind == "BOOL") return ("bool", "ref ", "[MarshalAs(UnmanagedType.I1)] ");
            if (kind == "POINTER") return ("nint", "out ", "");
            if (kind == "RECORD") return (element.Text("name") is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter" ? ManagedRecord(element.Text("name")) : "nint", "", "");
            if (kind == "VOID") return ("nint", "", "");
            return (Type(type)[..^1], "out ", "");
        }
        return (PublicType(type), "", "");
    }

    private void WriteCallbacks()
    {
        StringBuilder guards = new(Header + "using System.Runtime.InteropServices;\n\n" + Namespace + "internal static partial class NgxCallbacks\n{\n");
        foreach ((string name, JsonElement alias) in aliases)
        {
            JsonElement signature = alias.GetProperty("element");
            if (signature.Text("kind") != "FUNCTIONPROTO") continue;
            string managed = TypeName(name);
            string result = PublicType(signature.GetProperty("result"));
            var parameters = signature.Items("arguments").Select(CallbackParameter).ToArray();
            string[] names = CallbackArguments(name);
            if (names.Length != parameters.Length) throw new InvalidOperationException("Callback signature changed: " + name);
            string args = string.Join(", ", parameters.Select((p, i) => p.Attribute + p.Modifier + p.Type + " " + names[i]));
            bool used = functions.Values.SelectMany(f => f.Items("parameters")).Concat(records.Values.SelectMany(r => r.Items("fields")))
                .Any(p => CallbackName(p.GetProperty("type")) == managed);
            string contract = used
                ? ". Ngx contains callback exceptions and manages delegate roots for this signature."
                : ". Borrowed SDK callbacks require a live source; raw managed registrations require caller-owned roots and exception containment.";
            files[$"Callbacks/{managed}.g.cs"] = Header + "using System.Runtime.InteropServices;\n\n" + Namespace + Summary(name + contract) + $"[UnmanagedFunctionPointer(CallingConvention.Cdecl)]\npublic delegate {result} {managed}({args});\n";
            if (!used) continue;
            guards.AppendLine($"    internal static nint Acquire({managed}? callback)\n    {{\n        if (callback is null) return 0;\n\n        {managed} guarded = ({string.Join(", ", parameters.Select((p, i) => p.Modifier + p.Type + " " + names[i]))}) =>\n        {{");
            for (int i = 0; i < parameters.Length; i++) if (parameters[i].Modifier == "out ") guards.AppendLine($"            {names[i]} = default;");
            guards.AppendLine($"            try\n            {{\n                {(result == "void" ? "" : "return ")}callback({string.Join(", ", parameters.Select((p, i) => p.Modifier + names[i]))});\n            }}\n            catch (Exception exception)\n            {{\n                Report(exception);");
            for (int i = 0; i < parameters.Length; i++) if (parameters[i].Modifier == "ref " && parameters[i].Type == "bool") guards.AppendLine($"                {names[i]} = true;");
            if (result != "void") guards.AppendLine($"                return {(result == "NGXResult" ? "NGXResult.Fail" : "default")};");
            guards.AppendLine("            }\n        };\n\n        return Register(guarded);\n    }\n");
        }
        guards.AppendLine("}");
        files["Callbacks/NgxCallbacks.g.cs"] = guards.ToString();
    }

    private static string[] CallbackArguments(string name)
    {
        if (name == "NVSDK_NGX_AppLogCallback") return ["message", "loggingLevel", "sourceComponent"];
        if (name.Contains("ProgressCallback", StringComparison.Ordinal)) return ["progress", "shouldCancel"];
        if (name.Contains("_Parameter_", StringComparison.Ordinal)) return ["parameters", "name", "value"];
        if (name.Contains("D3D12_ResourceAlloc", StringComparison.Ordinal)) return ["description", "state", "heap", "resource"];
        if (name.Contains("D3D11_BufferAlloc", StringComparison.Ordinal)) return ["description", "buffer"];
        if (name.Contains("D3D11_Tex2DAlloc", StringComparison.Ordinal)) return ["description", "texture"];
        if (name.Contains("ResourceRelease", StringComparison.Ordinal)) return ["resource"];
        if (name.Contains("GetCurrentSettings", StringComparison.Ordinal)) return ["handle", "parameters"];
        if (name.Contains("EstimateVRAM", StringComparison.Ordinal)) return ["motionDepthWidth", "motionDepthHeight", "colorWidth", "colorHeight", "colorFormat", "motionFormat", "depthFormat", "hudlessFormat", "uiFormat", "estimatedBytes"];
        if (name.Contains("GetStats", StringComparison.Ordinal) || name.Contains("GetOptimalSettings", StringComparison.Ordinal)) return ["parameters"];
        throw new InvalidOperationException("Unclassified callback: " + name);
    }
}
