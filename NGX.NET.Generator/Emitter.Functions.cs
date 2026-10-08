using System.Text;
using System.Text.Json;

namespace NGX.NET.Generator;

internal sealed partial class Emitter
{
    private void WriteFunctions(string group, IEnumerable<JsonElement> source)
    {
        string imports = "using System.Runtime.CompilerServices;\nusing System.Runtime.InteropServices;\n\n";
        StringBuilder text = new(Header + imports + Namespace + "public static unsafe partial class Ngx\n{\n");
        int indent = group.Length == 0 ? 4 : 8;
        string pad = new(' ', indent);
        if (group.Length != 0)
        {
            text.Append(Summary($"{group} application API and native helpers.", 4));
            text.AppendLine($"    public static partial class {group}\n    {{\n        static {group}()\n        {{\n            RuntimeHelpers.RunClassConstructor(typeof(Ngx).TypeHandle);\n        }}\n");
        }
        foreach (JsonElement function in source.OrderBy(f => f.Text("name"), StringComparer.Ordinal))
        {
            WriteFunction(text, group, function, indent);
            string method = FunctionName(function.Text("name")).Method;
            string nativeArgs = string.Join(", ", function.Items("parameters").Select(p => Type(p.GetProperty("type")) + " " + ParameterName(p.Text("name"))));
            text.AppendLine($"{pad}[LibraryImport(LibraryName, EntryPoint = \"{function.Text("export")}\")]\n{pad}[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]\n{pad}private static partial {Type(function.GetProperty("result"))} {method}Native({nativeArgs});\n");
        }
        if (group.Length != 0) text.AppendLine("    }");
        text.AppendLine("}");
        files[$"API/{(group.Length == 0 ? "Core" : group)}.g.cs"] = text.ToString();
    }

    private void WriteFunction(StringBuilder text, string group, JsonElement function, int indent)
    {
        string method = FunctionName(function.Text("name")).Method;
        string pad = new(' ', indent), body = pad + "    ", inner = body + "    ";
        JsonElement resultType = function.GetProperty("result");
        string nativeResult = Type(resultType);
        string result = resultType.Text("kind") == "POINTER" ? "string?" : PublicType(resultType);
        bool init = method.StartsWith("Init", StringComparison.Ordinal);
        bool evaluate = function.Text("name").StartsWith("NGX_", StringComparison.Ordinal) && method.StartsWith("Evaluate", StringComparison.Ordinal);
        bool retained = init || evaluate;
        bool status = result == "NGXResult";
        bool cudaDevice = false;
        List<string> declarations = [], call = [], locals = [], setup = [], cleanup = [], outputs = [], forward = [];
        List<(int Index, string Type, string Name)> optionalRecords = [];
        string device = "0", parameterHandle = "0", allocated = "";
        JsonElement[] parameters = [.. function.Items("parameters")];
        foreach (JsonElement parameter in parameters)
        {
            string original = parameter.Text("name"), name = ParameterName(original), local = name + "Native";
            JsonElement type = parameter.GetProperty("type");
            string kind = type.Text("kind");
            string raw = Type(type);
            bool isCount = original is "OutExtensionCount" or "OutInstanceExtCount" or "OutDeviceExtCount";
            if (isCount)
            {
                locals.Add($"uint {local} = 0;");
                call.Add("&" + local);
                continue;
            }
            if (original == "OutExtensionProperties")
            {
                declarations.Add($"out NGXVkExtensionProperties[] {name}");
                forward.Add("out " + name);
                locals.Add($"{name} = [];\n{body}NGXVkExtensionPropertiesNative* {local} = null;");
                call.Add("&" + local);
                outputs.Add($"{name} = new NGXVkExtensionProperties[checked((int)outExtensionCountNative)];\n{inner}if ({name}.Length != 0 && {local} == null) throw new InvalidOperationException(\"NGX returned a null extension array.\");\n{inner}for (int i = 0; i < {name}.Length; i++) {name}[i] = new(in {local}[i]);");
                continue;
            }
            if (original is "OutInstanceExts" or "OutDeviceExts")
            {
                string count = original == "OutInstanceExts" ? "outInstanceExtCountNative" : "outDeviceExtCountNative";
                declarations.Add($"out string[] {name}");
                forward.Add("out " + name);
                locals.Add($"{name} = [];\n{body}sbyte** {local} = null;");
                call.Add("&" + local);
                outputs.Add($"{name} = new string[checked((int){count})];\n{inner}if ({name}.Length != 0 && {local} == null) throw new InvalidOperationException(\"NGX returned a null extension array.\");\n{inner}for (int i = 0; i < {name}.Length; i++) {name}[i] = NGXMarshal.PtrToString({local}[i], NGXEncoding.Utf8)!;");
                continue;
            }
            if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
            {
                JsonElement element = type.GetProperty("element");
                string ek = element.Text("kind");
                if (ek is "FUNCTIONPROTO" or "FUNCTIONNOPROTO")
                {
                    if (CallbackName(type) is string cb)
                    {
                        declarations.Add($"{cb}? {name}");
                        locals.Add($"nint {local} = 0;");
                        setup.Add($"{local} = NgxCallbacks.Acquire({name});");
                        cleanup.Add($"NgxCallbacks.Release({local});");
                        call.Add(local);
                    }
                    else
                    {
                        declarations.Add($"nint {name}");
                        call.Add(name);
                    }
                    forward.Add(name);
                }
                else if (ek is "CHAR_S" or "CHAR_U" or "WCHAR")
                {
                    string encoding = ek == "WCHAR" ? "NativeWide" : "Utf8";
                    declarations.Add($"string{(original == "InName" ? "" : "?")} {name}");
                    forward.Add(name);
                    locals.Add($"{raw} {local} = null;");
                    if (original == "InName") setup.Add($"ArgumentNullException.ThrowIfNull({name});");
                    setup.Add($"{local} = ({raw}){(retained ? "storage!.String" : "NGXMarshal.StringToPtr")}({name}, NGXEncoding.{encoding});");
                    if (!retained) cleanup.Add($"NGXMarshal.Free({local});");
                    call.Add(local);
                }
                else if (ek == "RECORD" && element.Text("name") is "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle")
                {
                    declarations.Add($"{ManagedRecord(element.Text("name"))} {name}");
                    call.Add(name + ".Value");
                    forward.Add(name);
                    if (element.Text("name") == "NVSDK_NGX_Parameter") parameterHandle = name + ".Value";
                    setup.Add($"if ({name}.IsNull) throw new ArgumentException(\"A non-null NGX handle is required.\", nameof({name}));");
                }
                else if (ek == "RECORD" && element.Text("name") == "NVSDK_NGX_CUDADevice")
                {
                    cudaDevice = true;
                    optionalRecords.Add((declarations.Count, "NGXCUDADevice", name));
                    declarations.Add($"NGXCUDADevice? {name}");
                    forward.Add(name);
                    locals.Add($"NGXCUDADeviceNative* {local} = null;");
                    setup.Add($"if ({name} is NGXCUDADevice deviceValue) {local} = NgxLifetime.CudaDevice(deviceValue);");
                    call.Add(local);
                    device = "(nint)" + local;
                }
                else if (IsRecord(element))
                {
                    string managed = PublicType(element), native = Type(element);
                    bool output = original == "OutSupported";
                    bool optional = original is "InFeatureInfo" or "pInDlssgOptEvalParams";
                    declarations.Add(output ? $"out {managed} {name}" : optional ? $"{managed}? {name}" : $"in {managed} {name}");
                    forward.Add((output ? "out " : optional ? "" : "in ") + name);
                    locals.Add($"{native} {local} = default;");
                    if (output)
                    {
                        locals.Add($"{name} = default;");
                        call.Add("&" + local);
                        outputs.Add($"{name} = new(in {local});");
                    }
                    else
                    {
                        if (optional) optionalRecords.Add((declarations.Count - 1, managed, name));
                        if (original == "pInDlssgOptEvalParams") setup.Add($"storage!.HasFrameGenerationOptions = {name}.HasValue;");
                        setup.Add(optional ? $"if ({name} is {managed} {name}Value) {local} = new(in {name}Value);" : $"{local} = new(in {name});");
                        cleanup.Add($"{local}.Dispose();");
                        if (retained)
                        {
                            locals.Add($"{native}* {local}Pointer = null;");
                            setup.Add($"{local}Pointer = {(optional ? name + ".HasValue ? " : "")}storage!.Take(ref {local}){(optional ? " : null" : "")};");
                            call.Add(local + "Pointer");
                        }
                        else call.Add(optional ? $"{name}.HasValue ? &{local} : null" : "&" + local);
                    }
                }
                else if (ek == "POINTER" || ek is not ("RECORD" or "VOID"))
                {
                    if (!(original.StartsWith("Out", StringComparison.Ordinal) || original.StartsWith("ppOut", StringComparison.Ordinal) || original.StartsWith("pOut", StringComparison.Ordinal) || original is "pVRAMAllocatedBytes" or "pOptLevel" or "IsDevSnippetBranch" or "estimatedVRAMInBytes")) throw new InvalidOperationException("Unclassified pointer direction: " + function.Text("name") + "::" + original);
                    string managed = ek == "POINTER" ? PublicType(element) : Type(type)[..^1];
                    if (managed.EndsWith('?')) managed = managed[..^1];
                    declarations.Add($"out {managed} {name}");
                    forward.Add("out " + name);
                    locals.Add($"{name} = default;\n{body}{raw[..^1]} {local} = default;");
                    call.Add("&" + local);
                    bool handle = managed is "NGXParameter" or "NGXHandle";
                    outputs.Add($"{name} = {(handle ? "new(" + local + ")" : managed == "nint" ? "(nint)" + local : local)};");
                    if (managed == "NGXParameter") allocated = name + ".Value";
                }
                else
                {
                    declarations.Add($"nint {name}");
                    forward.Add(name);
                    call.Add(raw == "nint" ? name : $"({raw}){name}");
                    if (original == "InDevice") device = name;
                }
            }
            else if (IsRecord(type))
            {
                declarations.Add($"in {PublicType(type)} {name}");
                forward.Add("in " + name);
                locals.Add($"{raw} {local} = default;");
                setup.Add($"{local} = new(in {name});");
                cleanup.Add($"{local}.Dispose();");
                call.Add(local);
            }
            else
            {
                declarations.Add($"{PublicType(type)} {name}");
                forward.Add(name);
                call.Add(name);
            }
        }
        bool guarded = retained || cleanup.Count != 0 || cudaDevice;
        if (!guarded) inner = body;
        text.Append(Summary($"{function.Text("name")}. Source: {function.Text("header")}:{function.Number("line")}. Native input storage is managed internally; serialize NGX calls and keep GPU resources alive until completion.", indent));
        text.AppendLine($"{pad}public static {result} {method}({string.Join(", ", declarations)})\n{pad}{{");
        if (retained) text.AppendLine(body + "NativeCall? storage = new();\n" + body + "NGXResult result = NGXResult.Fail;\n" + body + "bool attached = false;\n" + body + "bool returned = false;");
        if (cudaDevice) locals.Add("bool cudaSucceeded = false;");
        foreach (string local in locals) text.AppendLine(body + local);
        if (guarded) text.AppendLine($"\n{body}try\n{body}{{");
        foreach (string statement in setup) text.AppendLine(inner + statement);
        if (allocated.Length != 0) text.AppendLine(inner + "NgxLifetime.PrepareParameters();");
        if (init) text.AppendLine($"{inner}NgxLifetime.BeginInitialization(\"{group}\", {device}, storage!);\n{inner}attached = true;");
        if (evaluate) text.AppendLine($"{inner}NgxLifetime.BeginParameters({parameterHandle}, \"{group}.{method}\", storage!);\n{inner}attached = true;");
        text.AppendLine($"{inner}{(result == "void" ? "" : retained ? "result = " : nativeResult + " result = ")}{method}Native({string.Join(", ", call)});");
        if (retained) text.AppendLine(inner + "returned = true;");
        if (outputs.Count != 0)
        {
            text.AppendLine(inner + (status ? "if (Succeeded(result))" : "") + "\n" + inner + "{");
            foreach (string output in outputs) text.AppendLine(inner + "    " + output);
            if (allocated.Length != 0) text.AppendLine($"{inner}    NgxLifetime.RegisterParameters(\"{group}\", {allocated});");
            text.AppendLine(inner + "}");
        }
        if (method == "DestroyParameters") text.AppendLine($"{inner}if (Succeeded(result)) NgxLifetime.ReleaseParameters({parameterHandle}, true);");
        if (method.StartsWith("Shutdown", StringComparison.Ordinal)) text.AppendLine($"{inner}if (Succeeded(result)) NgxLifetime.Shutdown(\"{group}\", {device});");
        if (cudaDevice && !method.StartsWith("Shutdown", StringComparison.Ordinal)) text.AppendLine(inner + "cudaSucceeded = Succeeded(result);");
        if (result != "void") text.AppendLine(inner + "return " + (IsRecord(resultType) ? "new(in result)" : resultType.Text("kind") == "POINTER" ? "NGXMarshal.PtrToString(result, NGXEncoding.NativeWide)" : "result") + ";");
        if (guarded)
        {
            text.AppendLine($"{body}}}\n{body}finally\n{body}{{");
            if (init) text.AppendLine($"{inner}if (attached) NgxLifetime.EndInitialization(\"{group}\", {device}, returned && Succeeded(result), ref storage);");
            if (evaluate) text.AppendLine($"{inner}if (attached) NgxLifetime.EndParameters({parameterHandle}, \"{group}.{method}\", returned, Succeeded(result), ref storage);");
            if (retained) text.AppendLine(inner + "storage?.Dispose();");
            foreach (string statement in cleanup.AsEnumerable().Reverse()) text.AppendLine(inner + statement);
            if (cudaDevice) text.AppendLine($"{inner}NgxLifetime.FinishCudaDevice({device}, cudaSucceeded);");
            text.AppendLine($"{body}}}");
        }
        text.AppendLine($"{pad}}}\n");
        // Preserve ergonomic `in info` calls while keeping null representable.
        if (optionalRecords.Count != 0)
        {
            List<string> nonNullable = [.. declarations], arguments = [.. forward];
            foreach ((int index, string type, string name) in optionalRecords)
            {
                nonNullable[index] = $"in {type} {name}";
                arguments[index] = $"({type}?){name}";
            }
            text.Append(Summary($"{function.Text("name")}. Overload for a present optional structure.", indent));
            text.AppendLine($"{pad}public static {result} {method}({string.Join(", ", nonNullable)})\n{pad}{{\n{body}{(result == "void" ? "" : "return ")}{method}({string.Join(", ", arguments)});\n{pad}}}\n");
        }
    }
}
