using System.Text;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private bool IsRecord(JsonElement type) => type.Text("kind") == "RECORD" && !records[type.Text("name")].GetProperty("opaque").GetBoolean();

    private string? CallbackName(JsonElement type)
    {
        string cpp = type.Text("cpp").Replace("const ", "", StringComparison.Ordinal);
        return aliases.ContainsKey(cpp) ? TypeName(cpp) : null;
    }

    private string PublicType(JsonElement type)
    {
        string kind = type.Text("kind");
        if (kind == "BOOL") return "bool";
        if (kind == "RECORD") return ManagedRecord(type.Text("name"));
        if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
        {
            if (CallbackName(type) is string callback) return callback + "?";
            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U" or "WCHAR") return "string?";
            if (IsRecord(element)) return PublicType(element) + "?";
            if (element.Text("kind") == "RECORD" && element.Text("name") is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter") return TypeName(element.Text("name"));
            if (element.Text("kind") is "ULONGLONG" or "ULONG") return "ulong?";
            return "nint";
        }
        return Type(type);
    }

    private string PublicFieldType(string record, JsonElement field)
    {
        if (record == "NVSDK_NGX_PathListInfo" && field.Text("name") == "Path") return "string[]?";
        if (record == "NVSDK_NGX_FeatureCommonInfo" && field.Text("name") == "InternalData") return "nint";
        JsonElement type = field.GetProperty("type");
        if (records[record].Text("kind") == "UNION_DECL" && IsRecord(type)) return PublicType(type) + "?";
        if (MathFieldType(record, field) is string math) return math.Replace("*", "?", StringComparison.Ordinal);
        if (type.Text("kind") == "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U") return "string?";
            return PublicType(element) + "[]?";
        }
        return PublicType(type);
    }

    private static string PublicFieldName(string record, string field) => record == "NVSDK_NGX_PathListInfo" && field == "Path" ? "Paths" : Name(field);

    private void WriteRecord(string name, JsonElement record)
    {
        if (record.GetProperty("opaque").GetBoolean())
        {
            if (name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter") WriteHandle(name);
            return;
        }
        string managed = ManagedRecord(name);
        JsonElement[] fields = [.. record.Items("fields")];
        bool union = record.Text("kind") == "UNION_DECL";
        string imports = "using System.Numerics;\nusing System.Runtime.CompilerServices;\nusing System.Runtime.InteropServices;\n\n";
        StringBuilder publicText = new(Header + imports + Namespace + Summary(name) + $"public partial struct {managed}\n{{\n");
        StringBuilder nativeText = new(Header + imports + Namespace + Summary(name + ". Owns only storage allocated by managed conversion.") + $"[StructLayout(LayoutKind.Explicit, Size = {record.Number("size")})]\ninternal unsafe partial struct {managed}Native : IDisposable\n{{\n");
        foreach (JsonElement field in fields)
        {
            string fieldName = field.Text("name");
            JsonElement type = field.GetProperty("type");
            if (!(name == "NVSDK_NGX_PathListInfo" && fieldName == "Length"))
            {
                publicText.Append(Summary(name + "::" + fieldName, 4));
                publicText.AppendLine($"    public {PublicFieldType(name, field)} {PublicFieldName(name, fieldName)};\n");
            }
            nativeText.Append(Summary(name + "::" + fieldName, 4));
            nativeText.AppendLine($"    [FieldOffset({field.Number("offset") / 8})]");
            if (MathFieldType(name, field) is string math)
            {
                nativeText.AppendLine($"    public {math} {Name(fieldName)};\n");
            }
            else if (type.Text("kind") == "CONSTANTARRAY")
            {
                int count = type.Number("count");
                string element = Type(type.GetProperty("element"));
                if (element is "sbyte" or "byte" or "float" or "uint" or "int")
                {
                    nativeText.AppendLine($"    public fixed {element} {Name(fieldName)}[{count}];\n");
                }
                else
                {
                    string array = Name(fieldName) + "Buffer";
                    if (element.EndsWith('*')) element = "NGXPointer<" + element[..^1] + ">";
                    nativeText.AppendLine($"    public {array} {Name(fieldName)};\n\n    [InlineArray({count})]\n    internal struct {array}\n    {{\n        private {element} element;\n    }}\n");
                }
            }
            else nativeText.AppendLine($"    public {Type(type)} {Name(fieldName)};\n");
        }
        WriteDefaults(publicText, managed, fields);
        nativeText.AppendLine($"    public {managed}Native(in {managed} value)\n    {{\n        this = default;\n\n        try\n        {{");
        if (union)
        {
            JsonElement[] branches = [.. fields.Where(f => IsRecord(f.GetProperty("type")))];
            if (branches.Length > 1) nativeText.AppendLine($"            if (value.{Name(branches[0].Text("name"))}.HasValue && value.{Name(branches[1].Text("name"))}.HasValue) throw new ArgumentException(\"Only one union member may be specified.\", nameof(value));");
            for (int i = 0; i < branches.Length; i++)
            {
                string field = Name(branches[i].Text("name"));
                string type = PublicType(branches[i].GetProperty("type"));
                nativeText.AppendLine($"            {(i == 0 ? "if" : "else if")} (value.{field} is {type} member{i})\n            {{\n                {field} = new(in member{i});\n            }}");
            }
            foreach (JsonElement field in fields.Where(f => !IsRecord(f.GetProperty("type"))))
                nativeText.AppendLine($"            else\n            {{\n                {Name(field.Text("name"))} = value.{Name(field.Text("name"))};\n            }}");
        }
        else
        {
            if (name == "NVSDK_NGX_LoggingInfo") nativeText.AppendLine("            if (value.DisableOtherLoggingSinks && value.LoggingCallback is null) throw new ArgumentException(\"A logging callback is required when disabling other logging sinks.\", nameof(value));");
            if (name == "NVSDK_NGX_Application_Identifier")
            {
                nativeText.AppendLine("            if (value.IdentifierType == NGXApplicationIdentifierType.ProjectId != value.V.ProjectDesc.HasValue) throw new ArgumentException(\"Application identifier and active union member disagree.\", nameof(value));");
                nativeText.AppendLine("            if (value.IdentifierType is not (NGXApplicationIdentifierType.ProjectId or NGXApplicationIdentifierType.ApplicationId)) throw new ArgumentOutOfRangeException(nameof(value));");
            }
            if (name == "NVSDK_NGX_Resource_VK") nativeText.AppendLine("            if (value.Type == NGXResourceVKType.VkImageView && value.Resource.BufferInfo.HasValue || value.Type == NGXResourceVKType.VkBuffer && value.Resource.ImageViewInfo.HasValue) throw new ArgumentException(\"Resource type and union member disagree.\", nameof(value));");
            foreach (JsonElement field in fields) EmitFieldConstruction(nativeText, name, field);
        }
        nativeText.AppendLine("        }\n        catch\n        {\n            Dispose();\n            throw;\n        }\n    }\n\n    public void Dispose()\n    {");
        if (!union)
        {
            foreach (JsonElement field in fields) EmitFieldDisposal(nativeText, name, field);
        }
        nativeText.AppendLine("        this = default;\n    }\n}");
        if (!union)
        {
            publicText.AppendLine($"    internal unsafe {managed}(in {managed}Native native)\n    {{\n        this = default;");
            foreach (JsonElement field in fields) EmitFieldRead(publicText, name, field);
            publicText.AppendLine("    }");
        }
        publicText.AppendLine("}");
        files[$"Types/{managed}.g.cs"] = publicText.ToString();
        files[$"Types/Native/{managed}Native.g.cs"] = nativeText.ToString();
    }

    private void WriteHandle(string native)
    {
        string name = TypeName(native);
        files[$"Types/{name}.g.cs"] = Header + "using System.Runtime.InteropServices;\n\n" + Namespace + Summary(native + ". Borrowed handle; release through the matching NGX API after GPU work completes.") + $"[StructLayout(LayoutKind.Sequential)]\npublic readonly record struct {name}(nint Value)\n{{\n    /// <summary>Whether this handle is null.</summary>\n    public bool IsNull => Value == 0;\n}}\n";
    }

    private static void WriteDefaults(StringBuilder text, string name, JsonElement[] fields)
    {
        List<(string Field, string Value)> defaults = [];
        foreach (JsonElement field in fields)
        {
            if (!field.TryGetProperty("declaration", out JsonElement declaration) || !declaration.GetString()!.Contains('=')) continue;
            string value = declaration.GetString()!.Split('=', 2)[1].Trim();
            if (NumericInitializerRegex().IsMatch(value)) defaults.Add((Name(field.Text("name")), value));
            else if (!ZeroInitializerRegex().IsMatch(value)) throw new InvalidOperationException("Unsupported initializer: " + value);
        }
        if (defaults.Count == 0) return;
        text.Append(Summary("Initializes the defaults declared by the SDK.", 4));
        text.AppendLine($"    public {name}()\n    {{\n        this = default;");
        foreach ((string field, string value) in defaults) text.AppendLine($"        {field} = {value};");
        text.AppendLine("    }\n");
    }

    private void EmitFieldConstruction(StringBuilder text, string record, JsonElement field)
    {
        string name = field.Text("name"), target = Name(name), source = "value." + PublicFieldName(record, name);
        JsonElement type = field.GetProperty("type");
        if (record == "NVSDK_NGX_PathListInfo")
        {
            if (name == "Length") return;
            text.AppendLine("            if (value.Paths is { Length: > 0 } paths)\n            {\n                Path = (void**)NativeMemory.AllocZeroed(checked((nuint)paths.Length * (nuint)sizeof(void*)));\n                Length = checked((uint)paths.Length);\n                for (int i = 0; i < paths.Length; i++)\n                {\n                    ArgumentNullException.ThrowIfNull(paths[i]);\n                    Path[i] = NGXMarshal.TextToPtr(paths[i], NGXEncoding.NativeWide);\n                }\n            }");
            return;
        }
        if (MathFieldType(record, field) is string math)
        {
            text.AppendLine(math.EndsWith('*') ? $"            {target} = {source}.HasValue ? NGXMarshal.AllocValue({source}.Value) : null;" : $"            {target} = {source};");
            return;
        }
        if (type.Text("kind") == "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            int count = type.Number("count");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U")
                text.AppendLine($"            fixed (sbyte* buffer = {target}) NGXMarshal.WriteUtf8({source}, new Span<byte>(buffer, {count}));");
            else
            {
                string input = "items" + target;
                text.AppendLine($"            if ({source} is {{ }} {input})\n            {{\n                if ({input}.Length > {count}) throw new ArgumentException(\"{target} accepts at most {count} elements.\", nameof(value));\n                for (int i = 0; i < {input}.Length; i++)\n                {{");
                EmitAssignment(text, element, $"{target}[i]", $"{input}[i]", 20);
                text.AppendLine("                }\n            }");
            }
            return;
        }
        EmitAssignment(text, type, target, source, 12);
    }

    private void EmitAssignment(StringBuilder text, JsonElement type, string target, string source, int indent)
    {
        string expression;
        string kind = type.Text("kind");
        if (kind == "RECORD") expression = $"new(in {source})";
        else if (kind == "POINTER")
        {
            JsonElement element = type.GetProperty("element");
            string ek = element.Text("kind");
            if (CallbackName(type) is string cb) expression = $"NgxCallbacks.Acquire({source})";
            else if (ek is "CHAR_S" or "CHAR_U" or "WCHAR") expression = $"({Type(type)})NGXMarshal.TextToPtr({source}, NGXEncoding.{(ek == "WCHAR" ? "NativeWide" : "Utf8")})";
            else if (IsRecord(element))
            {
                string native = Type(element);
                string local = "item" + System.Text.RegularExpressions.Regex.Replace(target, "[^a-zA-Z0-9]", "");
                text.AppendLine($"{new string(' ', indent)}if ({source} is {PublicType(element)} {local})\n{new string(' ', indent)}{{\n{new string(' ', indent + 4)}{target} = NGXMarshal.AllocNative<{native}>(new(in {local}));\n{new string(' ', indent)}}}");
                return;
            }
            else if (ek is "ULONGLONG" or "ULONG") expression = $"{source}.HasValue ? NGXMarshal.AllocValue({source}.GetValueOrDefault()) : null";
            else expression = Type(type) == "nint" ? source : $"({Type(type)}){source}";
        }
        else expression = source;
        text.AppendLine($"{new string(' ', indent)}{target} = {expression};");
    }

    private void EmitFieldDisposal(StringBuilder text, string record, JsonElement field)
    {
        string name = field.Text("name"), target = Name(name);
        JsonElement type = field.GetProperty("type");
        if (record == "NVSDK_NGX_PathListInfo")
        {
            if (name == "Path") text.AppendLine("        if (Path != null)\n        {\n            for (uint i = 0; i < Length; i++) NGXMarshal.Free(Path[i]);\n            NativeMemory.Free(Path);\n        }");
            return;
        }
        if (record == "NVSDK_NGX_Application_Identifier" && name == "v")
        {
            text.AppendLine("        if (IdentifierType == NGXApplicationIdentifierType.ProjectId) V.ProjectDesc.Dispose();");
            return;
        }
        if (MathFieldType(record, field) is string math)
        {
            if (math.EndsWith('*')) text.AppendLine($"        NGXMarshal.Free({target});");
            return;
        }
        if (type.Text("kind") == "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            string? release = ReleaseExpression(element, $"{target}[i]");
            if (release != null) text.AppendLine($"        for (int i = 0; i < {type.Number("count")}; i++) {release}");
        }
        else if (ReleaseExpression(type, target) is string release) text.AppendLine("        " + release);
    }

    private string? ReleaseExpression(JsonElement type, string value)
    {
        if (IsRecord(type)) return value + ".Dispose();";
        if (type.Text("kind") != "POINTER") return null;
        if (CallbackName(type) != null) return $"NgxCallbacks.Release({value});";
        JsonElement element = type.GetProperty("element");
        if (IsRecord(element)) return $"NGXMarshal.FreeNative(({Type(element)}*){value});";
        return element.Text("kind") is "CHAR_S" or "CHAR_U" or "WCHAR" or "ULONGLONG" or "ULONG" ? $"NGXMarshal.Free({value});" : null;
    }

    private void EmitFieldRead(StringBuilder text, string record, JsonElement field)
    {
        string name = field.Text("name"), target = PublicFieldName(record, name), source = "native." + Name(name);
        JsonElement type = field.GetProperty("type");
        if (record == "NVSDK_NGX_PathListInfo")
        {
            if (name == "Path") text.AppendLine("        Paths = new string[checked((int)native.Length)];\n        for (int i = 0; i < Paths.Length; i++) Paths[i] = NGXMarshal.PtrToString(native.Path[i], NGXEncoding.NativeWide)!;");
            return;
        }
        if (record == "NVSDK_NGX_Application_Identifier" && name == "v")
        {
            text.AppendLine("        V = native.IdentifierType == NGXApplicationIdentifierType.ProjectId ? new() { ProjectDesc = new NGXProjectIdDescription(in native.V.ProjectDesc) } : new() { ApplicationId = native.V.ApplicationId };");
            return;
        }
        if (record == "NVSDK_NGX_Resource_VK" && name == "Resource")
        {
            text.AppendLine("        Resource = native.Type == NGXResourceVKType.VkImageView ? new() { ImageViewInfo = new NGXImageViewInfoVK(in native.Resource.ImageViewInfo) } : new() { BufferInfo = new NGXBufferInfoVK(in native.Resource.BufferInfo) };");
            return;
        }
        if (MathFieldType(record, field) is string math)
        {
            text.AppendLine($"        {target} = {(math.EndsWith('*') ? source + " == null ? null : *" + source : source)};");
            return;
        }
        if (type.Text("kind") == "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U") text.AppendLine($"        fixed (sbyte* buffer = {source}) {target} = NGXMarshal.ReadUtf8(new ReadOnlySpan<byte>(buffer, {type.Number("count")}));");
            else text.AppendLine($"        {target} = new {PublicType(element)}[{type.Number("count")}];\n        for (int i = 0; i < {target}.Length; i++) {target}[i] = {ReadExpression(element, source + "[i]")};");
        }
        else text.AppendLine($"        {target} = {ReadExpression(type, source)};");
    }

    private string ReadExpression(JsonElement type, string source)
    {
        if (IsRecord(type)) return $"new(in {source})";
        if (type.Text("kind") == "POINTER")
        {
            if (CallbackName(type) is string cb) return $"{source} == 0 ? null : Marshal.GetDelegateForFunctionPointer<{cb}>({source})";
            JsonElement element = type.GetProperty("element");
            string kind = element.Text("kind");
            if (kind is "CHAR_S" or "CHAR_U" or "WCHAR") return $"NGXMarshal.PtrToString({source}, NGXEncoding.{(kind == "WCHAR" ? "NativeWide" : "Utf8")})";
            if (IsRecord(element)) return $"({Type(type)}){source} == null ? null : new {PublicType(element)}(in *({Type(type)}){source})";
            if (kind is "ULONGLONG" or "ULONG") return $"({Type(type)}){source} == null ? null : *({Type(type)}){source}";
            return "(nint)" + source;
        }
        return source;
    }
}
