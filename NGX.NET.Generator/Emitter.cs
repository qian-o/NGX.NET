using System.Globalization;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace NGX.NET.Generator;

internal partial class Emitter
{
    private static readonly HashSet<string> acronyms = ["NGX", "DLSS", "DLSSD", "DLSSG", "DLAA", "DLISP", "CUDA", "D3D11", "D3D12", "VK", "UI", "ULL", "F", "D", "I", "API", "HDR", "SR", "RR", "RW", "VRAM"];
    private static readonly string[] FunctionGroups = ["D3D11", "D3D12", "CUDA", "VULKAN", "VK", "Parameter", "DLSSD", "DLSS"];

    private readonly JsonElement[] platforms;
    private readonly Dictionary<string, JsonElement> records;
    private readonly Dictionary<string, JsonElement> enums;
    private readonly Dictionary<string, JsonElement> functions;
    private readonly Dictionary<string, JsonElement> aliases;
    private readonly Dictionary<string, string> files = [];
    private readonly Dictionary<string, string> unionNames = [];

    public Emitter(JsonElement ast)
    {
        platforms = [.. ast.GetProperty("platforms").EnumerateObject().Select(static p => p.Value)];
        records = Merge("records");
        enums = Merge("enums");
        functions = Merge("functions");
        aliases = platforms.SelectMany(static p => p.GetProperty("aliases").EnumerateObject()).GroupBy(static p => p.Name).ToDictionary(static g => g.Key, static g => g.First().Value);

        foreach (JsonElement parent in records.Values)
        {
            foreach (JsonElement field in parent.Items("fields"))
            {
                JsonElement type = field.GetProperty("type");
                if (type.Text("kind") is "RECORD" && records[type.Text("name")].Text("kind") is "UNION_DECL")
                {
                    unionNames[type.Text("name")] = TypeName(parent.Text("name")) + "Union";
                }
            }
        }
    }

    public Dictionary<string, string> Generate()
    {
        files.Clear();
        signatures.Clear();
        resultTypes.Clear();

        foreach ((string name, JsonElement value) in enums)
        {
            WriteEnum(name, value);
        }

        foreach ((string name, JsonElement value) in records)
        {
            WriteRecord(name, value);
        }

        foreach (IGrouping<string, JsonElement> group in functions.Values.GroupBy(static f => FunctionName(f.Text("name")).Group))
        {
            WriteFunctions(group.Key, group);
        }

        WriteCallbacks();
        WriteConstants();
        WriteResultTypes();

        return files;
    }

    private Dictionary<string, JsonElement> Merge(string key)
    {
        return platforms.SelectMany(p => p.Items(key)).GroupBy(static e => e.Text("name")).ToDictionary(static g => g.Key, static g => g.First());
    }

    private void WriteConstants()
    {
        CodeWriter text = CreateFile();
        text.BeginBlock("public static unsafe partial class Ngx");

        Dictionary<string, JsonElement> macros = Merge("macros");
        foreach (JsonElement macro in macros.Values.OrderBy(static value => value.Text("name"), StringComparer.Ordinal))
        {
            string native = macro.Text("name");
            string[] tokens = [.. macro.Items("tokens").Select(static token => token.GetString()!)];
            if (macro.GetProperty("functionLike").GetBoolean() || tokens.Length is 0)
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

    private (string Type, string Modifier, string Attribute) CallbackParameter(JsonElement type)
    {
        if (type.Text("kind") is "POINTER" or "LVALUEREFERENCE")
        {
            JsonElement element = type.GetProperty("element");
            string kind = element.Text("kind");
            if (kind is "CHAR_S" or "CHAR_U")
            {
                return ("string?", "", "[MarshalAs(UnmanagedType.LPUTF8Str)] ");
            }

            if (kind is "BOOL")
            {
                return ("bool", "ref ", "[MarshalAs(UnmanagedType.I1)] ");
            }

            if (kind is "POINTER")
            {
                return ("nint", "out ", "");
            }

            if (kind is "RECORD")
            {
                return (element.Text("name") is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter" ? ManagedRecord(element.Text("name")) : "nint", "", "");
            }

            if (kind is "VOID")
            {
                return ("nint", "", "");
            }

            return (Type(type)[..^1], "out ", "");
        }

        return (PublicType(type), "", "");
    }

    private void WriteCallbacks()
    {
        CodeWriter guards = CreateFile();
        guards.BeginBlock("internal static partial class NgxCallbacks");

        foreach ((string name, JsonElement alias) in aliases)
        {
            JsonElement signature = alias.GetProperty("element");
            if (signature.Text("kind") is not "FUNCTIONPROTO")
            {
                continue;
            }

            string managed = TypeName(name);
            string result = PublicType(signature.GetProperty("result"));
            (string Type, string Modifier, string Attribute)[] parameters = [.. signature.Items("arguments").Select(CallbackParameter)];
            string[] names = CallbackArguments(name);
            if (names.Length != parameters.Length)
            {
                throw new InvalidOperationException($"Callback signature changed: {name}.");
            }

            string arguments = string.Join(", ", parameters.Select((parameter, i) => $"{parameter.Attribute}{parameter.Modifier}{parameter.Type} {names[i]}"));
            CodeWriter callback = CreateFile("System.Runtime.InteropServices");
            WriteSummary(callback, name);
            callback.Line("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
            callback.Line($"public delegate {result} {managed}({arguments});");
            files[$"Callbacks/{managed}.g.cs"] = callback.ToString();

            bool used = functions.Values.SelectMany(static function => function.Items("parameters")).Concat(records.Values.SelectMany(static record => record.Items("fields"))).Any(parameter => CallbackName(parameter.GetProperty("type")) == managed);
            if (!used)
            {
                continue;
            }

            guards.BeginBlock($"internal static nint Acquire({managed}? callback)");
            guards.BeginBlock("if (callback is null)");
            guards.Line("return 0;");
            guards.EndBlock();
            guards.BeginBlock($"{managed} guarded = ({string.Join(", ", parameters.Select((parameter, i) => $"{parameter.Modifier}{parameter.Type} {names[i]}"))}) =>");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Modifier is "out ")
                {
                    guards.Line($"{names[i]} = default;");
                    guards.BlankLine();
                }
            }

            guards.BeginBlock("try");
            guards.Line($"{(result is "void" ? string.Empty : "return ")}callback({string.Join(", ", parameters.Select((parameter, i) => parameter.Modifier + names[i]))});");
            guards.EndBlock();
            guards.BeginBlock("catch (Exception exception)", continuation: true);
            guards.Line("Report(exception);");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].Modifier is "ref " && parameters[i].Type is "bool")
                {
                    guards.Line($"{names[i]} = true;");
                }
            }

            if (result is not "void")
            {
                guards.BlankLine();
                guards.Line($"return {(result is "NGXResult" ? "NGXResult.Fail" : "default")};");
            }

            guards.EndBlock();
            guards.EndBlock(";");
            guards.Line("return Register(guarded);");
            guards.EndBlock();
        }

        guards.EndBlock();
        files["Callbacks/NgxCallbacks.g.cs"] = guards.ToString();
    }

    private void WriteFunctions(string group, IEnumerable<JsonElement> source)
    {
        CodeWriter text = CreateFile("System.Runtime.CompilerServices", "System.Runtime.InteropServices");
        text.BeginBlock("public static unsafe partial class Ngx");

        if (group.Length is not 0)
        {
            WriteSummary(text, $"{group} application API and native helpers.");
            text.BeginBlock($"public static partial class {group}");
        }

        JsonElement[] ordered = [.. source.OrderBy(static function => function.Text("name"), StringComparer.Ordinal)];
        foreach (JsonElement function in ordered)
        {
            string method = FunctionName(function.Text("name")).Method;
            string arguments = string.Join(", ", function.Items("parameters").Select(parameter => $"{Type(parameter.GetProperty("type"))} {ParameterName(parameter.Text("name"))}"));
            text.Line($"[LibraryImport(LibraryName, EntryPoint = \"{function.Text("export")}\")]");
            text.Line("[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            text.Line($"private static partial {Type(function.GetProperty("result"))} {method}Native({arguments});");
            text.BlankLine();
        }

        text.BeginBlock($"static {(group.Length is 0 ? "Ngx" : group)}()");
        text.Line("NativeLoader.Register();");
        text.EndBlock();

        foreach (JsonElement function in ordered)
        {
            WriteFunction(text, group, function);
        }

        if (group.Length is not 0)
        {
            text.EndBlock();
        }

        text.EndBlock();
        files[$"API/{(group.Length is 0 ? "Core" : group)}.g.cs"] = text.ToString();
    }

    private void WriteFunction(CodeWriter text, string group, JsonElement function)
    {
        string method = FunctionName(function.Text("name")).Method;
        JsonElement resultType = function.GetProperty("result");
        string nativeResult = Type(resultType);
        string result = resultType.Text("kind") is "POINTER" ? "string?" : PublicType(resultType);
        bool init = method.StartsWith("Init", StringComparison.Ordinal);
        bool evaluate = function.Text("name").StartsWith("NGX_", StringComparison.Ordinal) && method.StartsWith("Evaluate", StringComparison.Ordinal);
        bool retained = init || evaluate;
        bool status = result is "NGXResult";
        bool cudaDevice = false;
        List<string> declarations = [];
        List<string> call = [];
        List<string> locals = [];
        List<Action<CodeWriter>> setup = [];
        List<string> cleanup = [];
        List<Action<CodeWriter>> outputs = [];
        List<string> forward = [];
        List<(int Index, string Type, string Name)> resultParameters = [];
        List<(int Index, string Type, string Name)> optionalRecords = [];
        string device = "0";
        string parameterHandle = "0";
        string allocated = string.Empty;
        foreach (JsonElement parameter in function.Items("parameters"))
        {
            string original = parameter.Text("name");
            string name = ParameterName(original);
            string local = name + "Native";
            JsonElement type = parameter.GetProperty("type");
            string kind = type.Text("kind");
            string raw = Type(type);

            if (original is "OutExtensionCount" or "OutInstanceExtCount" or "OutDeviceExtCount")
            {
                locals.Add($"uint {local} = 0;");
                call.Add("&" + local);

                continue;
            }

            if (original is "OutExtensionProperties" or "OutInstanceExts" or "OutDeviceExts")
            {
                bool properties = original is "OutExtensionProperties";
                string element = properties ? "NGXVkExtensionProperties" : "string";
                string native = properties ? "NGXVkExtensionPropertiesNative*" : "sbyte**";
                string count = original switch
                {
                    "OutExtensionProperties" => "outExtensionCountNative",
                    "OutInstanceExts" => "outInstanceExtCountNative",
                    _ => "outDeviceExtCountNative"
                };
                resultParameters.Add((declarations.Count, element + "[]", name));
                declarations.Add($"out {element}[] {name}");
                forward.Add("out " + name);
                locals.Add($"{name} = [];");
                locals.Add($"{native} {local} = null;");
                call.Add("&" + local);
                outputs.Add(writer =>
                {
                    writer.Line($"{name} = new {element}[checked((int){count})];");
                    WriteGuard(writer, $"{name}.Length is not 0 && {local} == null", "throw new InvalidOperationException(\"NGX returned a null extension array.\");");
                    writer.BeginBlock($"for (int i = 0; i < {name}.Length; i++)");
                    writer.Line($"{name}[i] = {(properties ? $"new(in {local}[i])" : $"NGXMarshal.PtrToString({local}[i], NGXEncoding.Utf8)!")};");
                    writer.EndBlock();
                });

                continue;
            }

            if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
            {
                JsonElement element = type.GetProperty("element");
                string elementKind = element.Text("kind");
                if (elementKind is "FUNCTIONPROTO" or "FUNCTIONNOPROTO")
                {
                    if (CallbackName(type) is string callback)
                    {
                        declarations.Add($"{callback}? {name}");
                        locals.Add($"nint {local} = 0;");
                        setup.Add(writer => writer.Line($"{local} = NgxCallbacks.Acquire({name});"));
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
                else if (elementKind is "CHAR_S" or "CHAR_U" or "WCHAR")
                {
                    string encoding = elementKind is "WCHAR" ? "NativeWide" : "Utf8";
                    declarations.Add($"string{(original is "InName" ? string.Empty : "?")} {name}");
                    forward.Add(name);
                    locals.Add($"{raw} {local} = null;");

                    if (original is "InName")
                    {
                        setup.Add(writer => writer.Line($"ArgumentNullException.ThrowIfNull({name});"));
                    }

                    setup.Add(writer => writer.Line($"{local} = ({raw}){(retained ? "storage!.String" : "NGXMarshal.StringToPtr")}({name}, NGXEncoding.{encoding});"));

                    if (!retained)
                    {
                        cleanup.Add($"NGXMarshal.Free({local});");
                    }

                    call.Add(local);
                }
                else if (elementKind is "RECORD" && element.Text("name") is "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle")
                {
                    declarations.Add($"{ManagedRecord(element.Text("name"))} {name}");
                    call.Add(name + ".Value");
                    forward.Add(name);

                    if (element.Text("name") is "NVSDK_NGX_Parameter")
                    {
                        parameterHandle = name + ".Value";
                    }

                    setup.Add(writer => WriteGuard(writer, $"{name}.IsNull", $"throw new ArgumentException(\"A non-null NGX handle is required.\", nameof({name}));"));
                }
                else if (elementKind is "RECORD" && element.Text("name") is "NVSDK_NGX_CUDADevice")
                {
                    cudaDevice = true;
                    optionalRecords.Add((declarations.Count, "NGXCUDADevice", name));
                    declarations.Add($"NGXCUDADevice? {name}");
                    forward.Add(name);
                    locals.Add($"NGXCUDADeviceNative* {local} = null;");
                    setup.Add(writer => WriteGuard(writer, $"{name} is NGXCUDADevice deviceValue", $"{local} = NgxLifetime.CudaDevice(deviceValue);"));
                    call.Add(local);
                    device = "(nint)" + local;
                }
                else if (IsRecord(element))
                {
                    string managed = PublicType(element);
                    string native = Type(element);
                    bool output = original is "OutSupported";
                    bool optional = original is "InFeatureInfo" or "pInDlssgOptEvalParams";
                    string modifier = (output, optional) switch
                    {
                        (true, _) => "out ",
                        (_, true) => string.Empty,
                        _ => "in "
                    };
                    declarations.Add($"{modifier}{managed}{(optional ? "?" : string.Empty)} {name}");
                    forward.Add(modifier + name);
                    locals.Add($"{native} {local} = default;");

                    if (output)
                    {
                        resultParameters.Add((declarations.Count - 1, managed, name));
                        locals.Add($"{name} = default;");
                        call.Add("&" + local);
                        outputs.Add(writer => writer.Line($"{name} = new(in {local});"));
                    }
                    else
                    {
                        if (optional)
                        {
                            optionalRecords.Add((declarations.Count - 1, managed, name));
                        }

                        if (original is "pInDlssgOptEvalParams")
                        {
                            setup.Add(writer => writer.Line($"storage!.HasFrameGenerationOptions = {name}.HasValue;"));
                        }

                        setup.Add(writer =>
                        {
                            if (optional)
                            {
                                WriteGuard(writer, $"{name} is {managed} {name}Value", $"{local} = new(in {name}Value);");
                            }
                            else
                            {
                                writer.Line($"{local} = new(in {name});");
                            }
                        });
                        cleanup.Add($"{local}.Dispose();");

                        if (retained)
                        {
                            locals.Add($"{native}* {local}Pointer = null;");
                            setup.Add(writer => writer.Line($"{local}Pointer = {(optional ? name + ".HasValue ? " : string.Empty)}storage!.Take(ref {local}){(optional ? " : null" : string.Empty)};"));
                            call.Add(local + "Pointer");
                        }
                        else
                        {
                            call.Add(optional ? $"{name}.HasValue ? &{local} : null" : "&" + local);
                        }
                    }
                }
                else if (elementKind is "POINTER" or not ("RECORD" or "VOID"))
                {
                    if (!(original.StartsWith("Out", StringComparison.Ordinal) || original.StartsWith("ppOut", StringComparison.Ordinal) || original.StartsWith("pOut", StringComparison.Ordinal) || original is "pVRAMAllocatedBytes" or "pOptLevel" or "IsDevSnippetBranch" or "estimatedVRAMInBytes"))
                    {
                        throw new InvalidOperationException($"Unclassified pointer direction: {function.Text("name")}::{original}.");
                    }

                    string managed = elementKind is "POINTER" ? PublicType(element) : Type(type)[..^1];
                    if (managed.EndsWith('?'))
                    {
                        managed = managed[..^1];
                    }

                    resultParameters.Add((declarations.Count, managed, name));
                    declarations.Add($"out {managed} {name}");
                    forward.Add("out " + name);
                    locals.Add($"{name} = default;");
                    locals.Add($"{raw[..^1]} {local} = default;");
                    call.Add("&" + local);
                    string output = managed switch
                    {
                        "NGXParameter" or "NGXHandle" => $"new({local})",
                        "nint" => $"(nint){local}",
                        _ => local
                    };
                    outputs.Add(writer => writer.Line($"{name} = {output};"));

                    if (managed is "NGXParameter")
                    {
                        allocated = name + ".Value";
                    }
                }
                else
                {
                    declarations.Add($"nint {name}");
                    forward.Add(name);
                    call.Add(raw is "nint" ? name : $"({raw}){name}");

                    if (original is "InDevice")
                    {
                        device = name;
                    }
                }
            }
            else if (IsRecord(type))
            {
                declarations.Add($"in {PublicType(type)} {name}");
                forward.Add("in " + name);
                locals.Add($"{raw} {local} = default;");
                setup.Add(writer => writer.Line($"{local} = new(in {name});"));
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

        bool guarded = retained || cleanup.Count is not 0 || cudaDevice;
        RegisterFunction(group, method, declarations);
        WriteSummary(text, function.Text("name"));
        text.BeginBlock($"public static {result} {method}({string.Join(", ", declarations)})");

        if (retained)
        {
            text.Line("NativeCall? storage = new();");
            text.Line("NGXResult result = NGXResult.Fail;");
            text.Line("bool attached = false;");
            text.Line("bool returned = false;");
        }

        if (cudaDevice)
        {
            locals.Add("bool cudaSucceeded = false;");
        }

        foreach (string local in locals)
        {
            text.Line(local);
        }

        if (guarded)
        {
            text.BlankLine();
            text.BeginBlock("try");
        }

        foreach (Action<CodeWriter> statement in setup)
        {
            statement(text);
        }

        if (allocated.Length is not 0)
        {
            text.Line("NgxLifetime.PrepareParameters();");
        }

        if (init)
        {
            text.Line($"NgxLifetime.BeginInitialization(\"{group}\", {device}, storage!);");
            text.Line("attached = true;");
        }

        if (evaluate)
        {
            text.Line($"NgxLifetime.BeginParameters({parameterHandle}, \"{group}.{method}\", storage!);");
            text.Line("attached = true;");
        }

        string assignment = (result, retained) switch
        {
            ("void", _) => string.Empty,
            (_, true) => "result = ",
            _ => $"{nativeResult} result = "
        };
        text.Line($"{assignment}{method}Native({string.Join(", ", call)});");

        if (retained)
        {
            text.Line("returned = true;");
        }

        if (outputs.Count is not 0)
        {
            if (status)
            {
                text.BeginBlock("if (result is NGXResult.Success)");
            }

            foreach (Action<CodeWriter> output in outputs)
            {
                output(text);
            }

            if (allocated.Length is not 0)
            {
                text.Line($"NgxLifetime.RegisterParameters(\"{group}\", {allocated});");
            }

            if (status)
            {
                text.EndBlock();
            }
        }

        if (method is "DestroyParameters")
        {
            WriteGuard(text, "result is NGXResult.Success", $"NgxLifetime.ReleaseParameters({parameterHandle}, destroyed: true);");
        }

        if (method.StartsWith("Shutdown", StringComparison.Ordinal))
        {
            WriteGuard(text, "result is NGXResult.Success", $"NgxLifetime.Shutdown(\"{group}\", {device});");
        }

        if (cudaDevice && !method.StartsWith("Shutdown", StringComparison.Ordinal))
        {
            text.Line("cudaSucceeded = result is NGXResult.Success;");
        }

        if (result is not "void")
        {
            string returned = resultType.Text("kind") switch
            {
                "RECORD" when IsRecord(resultType) => "new(in result)",
                "POINTER" => "NGXMarshal.PtrToString(result, NGXEncoding.NativeWide)",
                _ => "result"
            };
            text.BlankLine();
            text.Line($"return {returned};");
        }

        if (guarded)
        {
            text.EndBlock();
            text.BeginBlock("finally", continuation: true);

            if (init)
            {
                WriteGuard(text, "attached", $"NgxLifetime.EndInitialization(\"{group}\", {device}, returned && result is NGXResult.Success, ref storage);");
            }

            if (evaluate)
            {
                WriteGuard(text, "attached", $"NgxLifetime.EndParameters({parameterHandle}, \"{group}.{method}\", returned, result is NGXResult.Success, ref storage);");
            }

            if (retained)
            {
                text.Line("storage?.Dispose();");
            }

            foreach (string statement in cleanup.AsEnumerable().Reverse())
            {
                text.Line(statement);
            }

            if (cudaDevice)
            {
                text.Line($"NgxLifetime.FinishCudaDevice({device}, cudaSucceeded);");
            }

            text.EndBlock();
        }

        text.EndBlock();

        if (status && resultParameters.Count is not 0)
        {
            WriteResultFunction(text, group, method, declarations, forward, resultParameters);
        }

        if (optionalRecords.Count is not 0)
        {
            List<string> nonNullable = [.. declarations];
            List<string> arguments = [.. forward];
            foreach ((int index, string type, string name) in optionalRecords)
            {
                nonNullable[index] = $"in {type} {name}";
                arguments[index] = $"({type}?){name}";
            }

            RegisterFunction(group, method, nonNullable);
            WriteSummary(text, $"{function.Text("name")}. Overload for a present optional structure.");
            text.BeginBlock($"public static {result} {method}({string.Join(", ", nonNullable)})");
            text.Line($"{(result is "void" ? string.Empty : "return ")}{method}({string.Join(", ", arguments)});");
            text.EndBlock();

            if (status && resultParameters.Count is not 0)
            {
                WriteResultFunction(text, group, method, nonNullable, arguments, resultParameters);
            }
        }
    }

    private string ManagedRecord(string name)
    {
        return unionNames.GetValueOrDefault(name, TypeName(name));
    }

    private bool IsRecord(JsonElement type)
    {
        return type.Text("kind") is "RECORD" && !records[type.Text("name")].GetProperty("opaque").GetBoolean();
    }

    private string? CallbackName(JsonElement type)
    {
        string cpp = type.Text("cpp").Replace("const ", "", StringComparison.Ordinal);

        return aliases.ContainsKey(cpp) ? TypeName(cpp) : null;
    }

    private string PublicType(JsonElement type)
    {
        string kind = type.Text("kind");
        if (kind is "BOOL")
        {
            return "bool";
        }

        if (kind is "RECORD")
        {
            return ManagedRecord(type.Text("name"));
        }

        if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
        {
            if (CallbackName(type) is string callback)
            {
                return callback + "?";
            }

            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U" or "WCHAR")
            {
                return "string?";
            }

            if (IsRecord(element))
            {
                return PublicType(element) + "?";
            }

            if (element.Text("kind") is "RECORD" && element.Text("name") is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter")
            {
                return TypeName(element.Text("name"));
            }

            if (element.Text("kind") is "ULONGLONG" or "ULONG")
            {
                return "ulong?";
            }

            return "nint";
        }

        return Type(type);
    }

    private string PublicFieldType(string record, JsonElement field)
    {
        if (record is "NVSDK_NGX_PathListInfo" && field.Text("name") is "Path")
        {
            return "string[]?";
        }

        if (record is "NVSDK_NGX_FeatureCommonInfo" && field.Text("name") is "InternalData")
        {
            return "nint";
        }

        JsonElement type = field.GetProperty("type");
        if (records[record].Text("kind") is "UNION_DECL" && IsRecord(type))
        {
            return PublicType(type) + "?";
        }

        if (MathFieldType(record, field) is string math)
        {
            return math.Replace("*", "?", StringComparison.Ordinal);
        }

        if (type.Text("kind") is "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U")
            {
                return "string?";
            }

            return PublicType(element) + "[]?";
        }

        return PublicType(type);
    }

    private void WriteRecord(string name, JsonElement record)
    {
        if (record.GetProperty("opaque").GetBoolean())
        {
            if (name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter")
            {
                WriteHandle(name);
            }

            return;
        }

        string managed = ManagedRecord(name);
        JsonElement[] fields = [.. record.Items("fields")];
        bool union = record.Text("kind") is "UNION_DECL";
        bool math = fields.Any(field => MathFieldType(name, field) is not null);
        bool callbacks = fields.Any(field => CallbackName(field.GetProperty("type")) is not null);
        List<string> publicImports = [];
        List<string> nativeImports = [];

        if (math)
        {
            publicImports.Add("System.Numerics");
            nativeImports.Add("System.Numerics");
        }

        if (callbacks && !union)
        {
            publicImports.Add("System.Runtime.InteropServices");
        }

        List<(string Name, string Element, int Count)> buffers = [];
        foreach (JsonElement field in fields)
        {
            JsonElement type = field.GetProperty("type");
            if (type.Text("kind") is not "CONSTANTARRAY" || MathFieldType(name, field) is not null)
            {
                continue;
            }

            string element = Type(type.GetProperty("element"));
            if (element is "sbyte" or "byte" or "float" or "uint" or "int")
            {
                continue;
            }

            if (element.EndsWith('*'))
            {
                element = $"NGXPointer<{element[..^1]}>";
            }

            buffers.Add((Name(field.Text("name")) + "Buffer", element, type.Number("count")));
        }

        if (buffers.Count is not 0)
        {
            nativeImports.Add("System.Runtime.CompilerServices");
        }

        nativeImports.Add("System.Runtime.InteropServices");
        CodeWriter publicText = CreateFile([.. publicImports]);
        WriteSummary(publicText, name);
        publicText.BeginBlock($"public struct {managed}");
        CodeWriter nativeText = CreateFile([.. nativeImports]);
        WriteSummary(nativeText, name + ". Owns storage allocated by managed conversion.");
        nativeText.Line($"[StructLayout(LayoutKind.Explicit, Size = {record.Number("size")})]");
        nativeText.BeginBlock($"internal unsafe struct {managed}Native : IDisposable");

        foreach (JsonElement field in fields)
        {
            string fieldName = field.Text("name");
            JsonElement type = field.GetProperty("type");

            if (!(name is "NVSDK_NGX_PathListInfo" && fieldName is "Length"))
            {
                WriteSummary(publicText, name + "::" + fieldName);
                publicText.Line($"public {PublicFieldType(name, field)} {PublicFieldName(name, fieldName)};");
                publicText.BlankLine();
            }

            WriteSummary(nativeText, name + "::" + fieldName);
            nativeText.Line($"[FieldOffset({field.Number("offset") / 8})]");

            if (MathFieldType(name, field) is string mathType)
            {
                nativeText.Line($"public {mathType} {Name(fieldName)};");
            }
            else if (type.Text("kind") is "CONSTANTARRAY")
            {
                string element = Type(type.GetProperty("element"));
                if (element is "sbyte" or "byte" or "float" or "uint" or "int")
                {
                    nativeText.Line($"public fixed {element} {Name(fieldName)}[{type.Number("count")}];");
                }
                else
                {
                    nativeText.Line($"public {Name(fieldName)}Buffer {Name(fieldName)};");
                }
            }
            else
            {
                nativeText.Line($"public {Type(type)} {Name(fieldName)};");
            }

            nativeText.BlankLine();
        }

        WriteDefaults(publicText, managed, fields);
        nativeText.BeginBlock($"public {managed}Native(in {managed} value)");
        nativeText.Line("this = default;");
        nativeText.BlankLine();
        nativeText.BeginBlock("try");

        if (union)
        {
            JsonElement[] branches = [.. fields.Where(field => IsRecord(field.GetProperty("type")))];
            if (branches.Length > 1)
            {
                WriteGuard(nativeText, $"value.{Name(branches[0].Text("name"))}.HasValue && value.{Name(branches[1].Text("name"))}.HasValue", "throw new ArgumentException(\"Only one union member may be specified.\", nameof(value));");
            }

            for (int i = 0; i < branches.Length; i++)
            {
                string field = Name(branches[i].Text("name"));
                string type = PublicType(branches[i].GetProperty("type"));
                nativeText.BeginBlock($"{(i is 0 ? "if" : "else if")} (value.{field} is {type} member{i})", continuation: i is not 0);
                nativeText.Line($"{field} = new(in member{i});");
                nativeText.EndBlock();
            }

            foreach (JsonElement field in fields.Where(field => !IsRecord(field.GetProperty("type"))))
            {
                nativeText.BeginBlock("else", continuation: true);
                nativeText.Line($"{Name(field.Text("name"))} = value.{Name(field.Text("name"))};");
                nativeText.EndBlock();
            }
        }
        else
        {
            if (name is "NVSDK_NGX_LoggingInfo")
            {
                WriteGuard(nativeText, "value.DisableOtherLoggingSinks && value.LoggingCallback is null", "throw new ArgumentException(\"A logging callback is required when disabling other logging sinks.\", nameof(value));");
            }

            if (name is "NVSDK_NGX_Application_Identifier")
            {
                WriteGuard(nativeText, "(value.IdentifierType is NGXApplicationIdentifierType.ProjectId) != value.V.ProjectDesc.HasValue", "throw new ArgumentException(\"Application identifier and active union member disagree.\", nameof(value));");
                WriteGuard(nativeText, "value.IdentifierType is not (NGXApplicationIdentifierType.ProjectId or NGXApplicationIdentifierType.ApplicationId)", "throw new ArgumentOutOfRangeException(nameof(value));");
            }

            if (name is "NVSDK_NGX_Resource_VK")
            {
                WriteGuard(nativeText, "(value.Type is NGXResourceVKType.VkImageView && value.Resource.BufferInfo.HasValue) || (value.Type is NGXResourceVKType.VkBuffer && value.Resource.ImageViewInfo.HasValue)", "throw new ArgumentException(\"Resource type and union member disagree.\", nameof(value));");
            }

            foreach (JsonElement field in fields)
            {
                EmitFieldConstruction(nativeText, name, field);
            }
        }

        nativeText.EndBlock();
        nativeText.BeginBlock("catch", continuation: true);
        nativeText.Line("Dispose();");
        nativeText.BlankLine();
        nativeText.Line("throw;");
        nativeText.EndBlock();
        nativeText.EndBlock();
        nativeText.BeginBlock("public void Dispose()");

        if (!union)
        {
            foreach (JsonElement field in fields.Reverse())
            {
                EmitFieldDisposal(nativeText, name, field);
            }
        }

        nativeText.Line("this = default;");
        nativeText.EndBlock();

        foreach ((string bufferName, string element, int count) in buffers)
        {
            nativeText.Line($"[InlineArray({count})]");
            nativeText.BeginBlock($"internal struct {bufferName}");
            nativeText.Line($"private {element} element;");
            nativeText.EndBlock();
        }

        nativeText.EndBlock();

        if (!union)
        {
            publicText.BeginBlock($"internal unsafe {managed}(in {managed}Native native)");
            publicText.Line("this = default;");

            foreach (JsonElement field in fields)
            {
                EmitFieldRead(publicText, name, field);
            }

            publicText.EndBlock();
        }

        publicText.EndBlock();
        files[$"Types/{managed}.g.cs"] = publicText.ToString();
        files[$"Types/Native/{managed}Native.g.cs"] = nativeText.ToString();
    }

    private void WriteHandle(string native)
    {
        string name = TypeName(native);
        CodeWriter text = CreateFile("System.Runtime.InteropServices");
        WriteSummary(text, native + ". Borrowed handle; release through the matching NGX API.");
        text.Line("[StructLayout(LayoutKind.Sequential)]");
        text.BeginBlock($"public readonly struct {name}(nint value) : IEquatable<{name}>");
        WriteSummary(text, "Native handle value.");
        text.Line("public readonly nint Value = value;");
        text.BlankLine();
        WriteSummary(text, "Whether this handle is null.");
        text.Line("public bool IsNull => Value is 0;");
        text.BlankLine();
        WriteSummary(text, "Compares native handle values.");
        text.BeginBlock($"public bool Equals({name} other)");
        text.Line("return Value == other.Value;");
        text.EndBlock();
        WriteSummary(text, "Compares native handle values.");
        text.BeginBlock("public override bool Equals(object? obj)");
        text.Line($"return obj is {name} other && Equals(other);");
        text.EndBlock();
        WriteSummary(text, "Returns the hash code of the native handle value.");
        text.BeginBlock("public override int GetHashCode()");
        text.Line("return Value.GetHashCode();");
        text.EndBlock();
        WriteSummary(text, "Returns the handle value and null state.");
        text.BeginBlock("public override string ToString()");
        text.Line($"return $\"{name} {{{{ Value = {{Value}}, IsNull = {{IsNull}} }}}}\";");
        text.EndBlock();
        WriteSummary(text, "Retrieves the native handle value.");
        text.BeginBlock("public void Deconstruct(out nint value)");
        text.Line("value = Value;");
        text.EndBlock();
        WriteSummary(text, "Compares native handle values.");
        text.BeginBlock($"public static bool operator ==({name} left, {name} right)");
        text.Line("return left.Equals(right);");
        text.EndBlock();
        WriteSummary(text, "Compares native handle values.");
        text.BeginBlock($"public static bool operator !=({name} left, {name} right)");
        text.Line("return !left.Equals(right);");
        text.EndBlock();
        text.EndBlock();
        files[$"Types/{name}.g.cs"] = text.ToString();
    }

    private void EmitFieldConstruction(CodeWriter text, string record, JsonElement field)
    {
        string name = field.Text("name");
        string target = Name(name);
        string source = "value." + PublicFieldName(record, name);
        JsonElement type = field.GetProperty("type");

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Length")
            {
                return;
            }

            text.BlankLine();
            text.BeginBlock("if (value.Paths is string[] paths && paths.Length > 0)");
            text.Line("Path = (void**)NativeMemory.AllocZeroed(checked((nuint)paths.Length * (nuint)sizeof(void*)));");
            text.Line("Length = checked((uint)paths.Length);");
            text.BlankLine();
            text.BeginBlock("for (int i = 0; i < paths.Length; i++)");
            text.Line("ArgumentNullException.ThrowIfNull(paths[i]);");
            text.Line("Path[i] = NGXMarshal.TextToPtr(paths[i], NGXEncoding.NativeWide);");
            text.EndBlock();
            text.EndBlock();

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            text.Line(math.EndsWith('*') ? $"{target} = {source}.HasValue ? NGXMarshal.AllocValue({source}.Value) : null;" : $"{target} = {source};");

            return;
        }

        if (type.Text("kind") is "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            int count = type.Number("count");
            text.BlankLine();

            if (element.Text("kind") is "CHAR_S" or "CHAR_U")
            {
                text.BeginBlock($"fixed (sbyte* buffer = {target})");
                text.Line($"NGXMarshal.WriteUtf8({source}, new Span<byte>(buffer, {count}));");
                text.EndBlock();
            }
            else
            {
                string input = "items" + target;
                text.BeginBlock($"if ({source} is {PublicFieldType(record, field).TrimEnd('?')} {input})");
                WriteGuard(text, $"{input}.Length > {count}", $"throw new ArgumentException(\"{target} accepts at most {count} elements.\", nameof(value));");
                text.BeginBlock($"for (int i = 0; i < {input}.Length; i++)");
                EmitAssignment(text, element, $"{target}[i]", $"{input}[i]");
                text.EndBlock();
                text.EndBlock();
            }

            return;
        }

        EmitAssignment(text, type, target, source);
    }

    private void EmitAssignment(CodeWriter text, JsonElement type, string target, string source)
    {
        string expression;
        string kind = type.Text("kind");
        if (kind is "RECORD")
        {
            expression = $"new(in {source})";
        }
        else if (kind is "POINTER")
        {
            JsonElement element = type.GetProperty("element");
            string elementKind = element.Text("kind");
            if (CallbackName(type) is not null)
            {
                expression = $"NgxCallbacks.Acquire({source})";
            }
            else if (elementKind is "CHAR_S" or "CHAR_U" or "WCHAR")
            {
                expression = $"({Type(type)})NGXMarshal.TextToPtr({source}, NGXEncoding.{(elementKind is "WCHAR" ? "NativeWide" : "Utf8")})";
            }
            else if (IsRecord(element))
            {
                string local = "item" + Regex.Replace(target, "[^a-zA-Z0-9]", string.Empty);
                WriteGuard(text, $"{source} is {PublicType(element)} {local}", $"{target} = NGXMarshal.AllocNative<{Type(element)}>(new(in {local}));");

                return;
            }
            else if (elementKind is "ULONGLONG" or "ULONG")
            {
                expression = $"{source}.HasValue ? NGXMarshal.AllocValue({source}.GetValueOrDefault()) : null";
            }
            else
            {
                expression = Type(type) is "nint" ? source : $"({Type(type)}){source}";
            }
        }
        else
        {
            expression = source;
        }

        text.Line($"{target} = {expression};");
    }

    private void EmitFieldDisposal(CodeWriter text, string record, JsonElement field)
    {
        string name = field.Text("name");
        string target = Name(name);
        JsonElement type = field.GetProperty("type");

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Path")
            {
                text.BeginBlock("if (Path != null)");
                text.BeginBlock("for (int i = checked((int)Length) - 1; i >= 0; i--)");
                text.Line("NGXMarshal.Free(Path[i]);");
                text.EndBlock();
                text.Line("NativeMemory.Free(Path);");
                text.EndBlock();
            }

            return;
        }

        if (record is "NVSDK_NGX_Application_Identifier" && name is "v")
        {
            WriteGuard(text, "IdentifierType is NGXApplicationIdentifierType.ProjectId", "V.ProjectDesc.Dispose();");

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            if (math.EndsWith('*'))
            {
                text.Line($"NGXMarshal.Free({target});");
            }

            return;
        }

        if (type.Text("kind") is "CONSTANTARRAY")
        {
            string? release = ReleaseExpression(type.GetProperty("element"), $"{target}[i]");
            if (release is not null)
            {
                text.BlankLine();
                text.BeginBlock($"for (int i = {type.Number("count") - 1}; i >= 0; i--)");
                text.Line(release);
                text.EndBlock();
            }
        }
        else if (ReleaseExpression(type, target) is string release)
        {
            text.Line(release);
        }
    }

    private string? ReleaseExpression(JsonElement type, string value)
    {
        if (IsRecord(type))
        {
            return value + ".Dispose();";
        }

        if (type.Text("kind") is not "POINTER")
        {
            return null;
        }

        if (CallbackName(type) is not null)
        {
            return $"NgxCallbacks.Release({value});";
        }

        JsonElement element = type.GetProperty("element");
        if (IsRecord(element))
        {
            return $"NGXMarshal.FreeNative(({Type(element)}*){value});";
        }

        return element.Text("kind") is "CHAR_S" or "CHAR_U" or "WCHAR" or "ULONGLONG" or "ULONG" ? $"NGXMarshal.Free({value});" : null;
    }

    private void EmitFieldRead(CodeWriter text, string record, JsonElement field)
    {
        string name = field.Text("name");
        string target = PublicFieldName(record, name);
        string source = "native." + Name(name);
        JsonElement type = field.GetProperty("type");

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Path")
            {
                text.Line("Paths = new string[checked((int)native.Length)];");
                text.BeginBlock("for (int i = 0; i < Paths.Length; i++)");
                text.Line("Paths[i] = NGXMarshal.PtrToString(native.Path[i], NGXEncoding.NativeWide)!;");
                text.EndBlock();
            }

            return;
        }

        if (record is "NVSDK_NGX_Application_Identifier" && name is "v")
        {
            text.Line("V = native.IdentifierType is NGXApplicationIdentifierType.ProjectId ? new() { ProjectDesc = new NGXProjectIdDescription(in native.V.ProjectDesc) } : new() { ApplicationId = native.V.ApplicationId };");

            return;
        }

        if (record is "NVSDK_NGX_Resource_VK" && name is "Resource")
        {
            text.Line("Resource = native.Type is NGXResourceVKType.VkImageView ? new() { ImageViewInfo = new NGXImageViewInfoVK(in native.Resource.ImageViewInfo) } : new() { BufferInfo = new NGXBufferInfoVK(in native.Resource.BufferInfo) };");

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            text.Line($"{target} = {(math.EndsWith('*') ? source + " == null ? null : *" + source : source)};");

            return;
        }

        if (type.Text("kind") is "CONSTANTARRAY")
        {
            JsonElement element = type.GetProperty("element");
            if (element.Text("kind") is "CHAR_S" or "CHAR_U")
            {
                text.BlankLine();
                text.BeginBlock($"fixed (sbyte* buffer = {source})");
                text.Line($"{target} = NGXMarshal.ReadUtf8(new ReadOnlySpan<byte>(buffer, {type.Number("count")}));");
                text.EndBlock();
            }
            else
            {
                text.Line($"{target} = new {PublicType(element)}[{type.Number("count")}];");
                text.BeginBlock($"for (int i = 0; i < {target}.Length; i++)");
                text.Line($"{target}[i] = {ReadExpression(element, source + "[i]")};");
                text.EndBlock();
            }
        }
        else
        {
            text.Line($"{target} = {ReadExpression(type, source)};");
        }
    }

    private string ReadExpression(JsonElement type, string source)
    {
        if (IsRecord(type))
        {
            return $"new(in {source})";
        }

        if (type.Text("kind") is "POINTER")
        {
            if (CallbackName(type) is string cb)
            {
                return $"{source} is 0 ? null : Marshal.GetDelegateForFunctionPointer<{cb}>({source})";
            }

            JsonElement element = type.GetProperty("element");
            string kind = element.Text("kind");
            if (kind is "CHAR_S" or "CHAR_U" or "WCHAR")
            {
                return $"NGXMarshal.PtrToString({source}, NGXEncoding.{(kind is "WCHAR" ? "NativeWide" : "Utf8")})";
            }

            if (IsRecord(element))
            {
                return $"({Type(type)}){source} == null ? null : new {PublicType(element)}(in *({Type(type)}){source})";
            }

            if (kind is "ULONGLONG" or "ULONG")
            {
                return $"({Type(type)}){source} == null ? null : *({Type(type)}){source}";
            }

            return "(nint)" + source;
        }

        return source;
    }

    private void WriteEnum(string name, JsonElement value)
    {
        string managed = TypeName(name);
        JsonElement[] values = [.. value.Items("values")];
        bool unsigned = name is "NVSDK_NGX_Result" || values.Any(static item => item.GetProperty("value").GetInt64() > int.MaxValue);
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

        foreach (JsonElement item in values)
        {
            string native = item.Text("name");
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
            long numeric = item.GetProperty("value").GetInt64();
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

    private string Type(JsonElement type)
    {
        string kind = type.Text("kind");
        string cpp = type.Text("cpp").Replace("const ", "", StringComparison.Ordinal).Trim();
        if (cpp is "size_t")
        {
            return "nuint";
        }

        if (cpp is "size_t *")
        {
            return "nuint*";
        }

        if (kind is "POINTER" or "LVALUEREFERENCE" or "RVALUEREFERENCE")
        {
            JsonElement element = type.GetProperty("element");
            string elementKind = element.Text("kind");
            if (elementKind is "FUNCTIONPROTO" or "FUNCTIONNOPROTO")
            {
                return "nint";
            }

            if (elementKind is "RECORD" && records[element.Text("name")].GetProperty("opaque").GetBoolean())
            {
                return "nint";
            }

            return (elementKind is "WCHAR" ? "void" : Type(element)) + "*";
        }

        if (kind is "ENUM" or "RECORD")
        {
            return ManagedRecord(type.Text("name")) + (kind is "RECORD" ? "Native" : "");
        }

        return kind switch
        {
            "VOID" => "void",
            "BOOL" => "Bool8",
            "CHAR_S" or "CHAR_U" or "SCHAR" => "sbyte",
            "UCHAR" => "byte",
            "SHORT" => "short",
            "USHORT" => "ushort",
            "INT" => "int",
            "UINT" => "uint",
            "LONG" => type.Number("size") is 8 ? "long" : "int",
            "ULONG" => type.Number("size") is 8 ? "ulong" : "uint",
            "LONGLONG" => "long",
            "ULONGLONG" => "ulong",
            "FLOAT" => "float",
            "DOUBLE" => "double",
            "WCHAR" => throw new InvalidOperationException("Unsupported native wchar_t value."),
            _ => throw new InvalidOperationException($"Unsupported native type: {type}")
        };
    }

    private static void WriteGuard(CodeWriter text, string condition, string statement)
    {
        text.BlankLine();
        text.BeginBlock($"if ({condition})");
        text.Line(statement);
        text.EndBlock();
    }

    private static string Escape(string value)
    {
        return SecurityElement.Escape(value)!;
    }

    private static CodeWriter CreateFile(params ReadOnlySpan<string> imports)
    {
        CodeWriter text = new();
        text.Line("// <auto-generated/>");
        text.Line("// Generated by NGX.NET.Generator. Changes belong in the generator.");
        text.BlankLine();
        text.Line("#nullable enable");
        text.BlankLine();

        foreach (string import in imports)
        {
            text.Line($"using {import};");
        }

        text.BlankLine();
        text.Line("namespace NGX.NET;");
        text.BlankLine();

        return text;
    }

    private static void WriteSummary(CodeWriter text, string summary)
    {
        text.Line("/// <summary>");
        text.Line($"/// {Escape(summary)}");
        text.Line("/// </summary>");
    }

    private static string[] CallbackArguments(string name)
    {
        if (name is "NVSDK_NGX_AppLogCallback")
        {
            return ["message", "loggingLevel", "sourceComponent"];
        }

        if (name.Contains("ProgressCallback", StringComparison.Ordinal))
        {
            return ["progress", "shouldCancel"];
        }

        if (name.Contains("_Parameter_", StringComparison.Ordinal))
        {
            return ["parameters", "name", "value"];
        }

        if (name.Contains("D3D12_ResourceAlloc", StringComparison.Ordinal))
        {
            return ["description", "state", "heap", "resource"];
        }

        if (name.Contains("D3D11_BufferAlloc", StringComparison.Ordinal))
        {
            return ["description", "buffer"];
        }

        if (name.Contains("D3D11_Tex2DAlloc", StringComparison.Ordinal))
        {
            return ["description", "texture"];
        }

        if (name.Contains("ResourceRelease", StringComparison.Ordinal))
        {
            return ["resource"];
        }

        if (name.Contains("GetCurrentSettings", StringComparison.Ordinal))
        {
            return ["handle", "parameters"];
        }

        if (name.Contains("EstimateVRAM", StringComparison.Ordinal))
        {
            return ["motionDepthWidth", "motionDepthHeight", "colorWidth", "colorHeight", "colorFormat", "motionFormat", "depthFormat", "hudlessFormat", "uiFormat", "estimatedBytes"];
        }

        if (name.Contains("GetStats", StringComparison.Ordinal) || name.Contains("GetOptimalSettings", StringComparison.Ordinal))
        {
            return ["parameters"];
        }

        throw new InvalidOperationException("Unclassified callback: " + name);
    }

    // PascalCase names cannot collide with C#'s lowercase keywords.
    private static string Name(string name)
    {
        name = name.Replace("NVSDK_NGX_", "", StringComparison.Ordinal);

        return string.Concat(name.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static p => acronyms.Contains(p) || p.Any(char.IsLower) ? char.ToUpperInvariant(p[0]) + p[1..] : CultureInfo.InvariantCulture.TextInfo.ToTitleCase(p.ToLowerInvariant())));
    }

    private static string TypeName(string native)
    {
        string name = Name(native);

        return name.StartsWith("NGX", StringComparison.Ordinal) ? name : "NGX" + name;
    }

    private static string ParameterName(string native)
    {
        string name = Name(native);
        // Preserve existing camelCase prefixes, such as pVRAMAllocatedBytes.
        name = char.IsLower(native[0]) ? char.ToLowerInvariant(name[0]) + name[1..] : JsonNamingPolicy.CamelCase.ConvertName(name);

        return name switch
        {
            "abstract" or "as" or "base" or "bool" or "break" or "byte" or "case" or "catch" or "char" or "checked" or "class" or "const" or "continue" or "decimal" or "default" or "delegate" or "do" or "double" or "else" or "enum" or "event" or "explicit" or "extern" or "false" or "finally" or "fixed" or "float" or "for" or "foreach" or "goto" or "if" or "implicit" or "in" or "int" or "interface" or "internal" or "is" or "lock" or "long" or "namespace" or "new" or "null" or "object" or "operator" or "out" or "override" or "params" or "private" or "protected" or "public" or "readonly" or "ref" or "return" or "sbyte" or "sealed" or "short" or "sizeof" or "stackalloc" or "static" or "string" or "struct" or "switch" or "this" or "throw" or "true" or "try" or "typeof" or "uint" or "ulong" or "unchecked" or "unsafe" or "ushort" or "using" or "virtual" or "void" or "volatile" or "while" => "@" + name,
            _ => name
        };
    }

    private static (string Group, string Method) FunctionName(string native)
    {
        if (native is "GetNGXResultAsString")
        {
            return ("", "GetResultAsString");
        }

        string name = native.StartsWith("NVSDK_NGX_", StringComparison.Ordinal) ? native[10..] : native[4..];
        foreach (string prefix in FunctionGroups)
        {
            if (name.StartsWith(prefix + "_", StringComparison.Ordinal))
            {
                return (prefix is "VULKAN" or "VK" ? "Vulkan" : prefix, Name(name[(prefix.Length + 1)..]));
            }
        }

        return ("", Name(name));
    }

    private static string EnumMember(string value)
    {
        if (value.Replace("_", "", StringComparison.Ordinal) is "VKIMAGEVIEW")
        {
            return "VkImageView";
        }

        if (value.Replace("_", "", StringComparison.Ordinal) is "VKBUFFER")
        {
            return "VkBuffer";
        }

        return string.Concat(value.Split('_', StringSplitOptions.RemoveEmptyEntries).Select(static part =>
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(part, @"^[RGBADESX0-9]+$") && part.Any(char.IsDigit))
            {
                return part;
            }

            return string.Concat(System.Text.RegularExpressions.Regex.Matches(part, @"[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+").Select(static match => char.ToUpperInvariant(match.Value[0]) + match.Value[1..].ToLowerInvariant()));
        }));
    }

    private static string PublicFieldName(string record, string field)
    {
        return record is "NVSDK_NGX_PathListInfo" && field is "Path" ? "Paths" : Name(field);
    }

    private static void WriteDefaults(CodeWriter text, string name, JsonElement[] fields)
    {
        List<(string Field, string Value)> defaults = [];
        foreach (JsonElement field in fields)
        {
            if (!field.TryGetProperty("declaration", out JsonElement declaration) || !declaration.GetString()!.Contains('='))
            {
                continue;
            }

            string value = declaration.GetString()!.Split('=', 2)[1].Trim();
            if (NumericInitializerRegex().IsMatch(value))
            {
                if (field.GetProperty("type").Text("kind") is "FLOAT")
                {
                    value = float.Parse(value.TrimEnd('f', 'F'), CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);

                    if (!value.Contains('.') && !value.Contains('E'))
                    {
                        value += ".0";
                    }

                    value += "f";
                }

                defaults.Add((Name(field.Text("name")), value));
            }
            else if (!ZeroInitializerRegex().IsMatch(value))
            {
                throw new InvalidOperationException($"Unsupported initializer: {value}.");
            }
        }

        if (defaults.Count is 0)
        {
            return;
        }

        WriteSummary(text, "Initializes the defaults declared by the SDK.");
        text.BeginBlock($"public {name}()");
        text.Line("this = default;");

        foreach ((string field, string value) in defaults)
        {
            text.Line($"{field} = {value};");
        }

        text.EndBlock();
    }

    [GeneratedRegex(@"^-?\d+(?:\.\d+f)?$")]
    private static partial Regex NumericInitializerRegex();

    [GeneratedRegex(@"^\{[0 ,.f]+\}$")]
    private static partial Regex ZeroInitializerRegex();

    // These fields have explicit vector/matrix semantics in the SDK. Array
    // length alone is not sufficient to map arbitrary native data to math types.
    private static string? MathFieldType(string record, JsonElement field)
    {
        return record switch
        {
            "NVSDK_NGX_DLSSG_Opt_Eval_Params" => field.Text("name") switch
            {
                "cameraViewToClip" or "clipToCameraView" or "clipToLensClip" or "clipToPrevClip" or "prevClipToClip" => "Matrix4x4",
                "jitterOffset" or "mvecScale" or "cameraPinholeOffset" => "Vector2",
                "cameraPos" or "cameraUp" or "cameraRight" or "cameraFwd" => "Vector3",
                _ => null
            },
            "NVSDK_NGX_CUDA_DLSSD_Eval_Params" or "NVSDK_NGX_D3D11_DLSSD_Eval_Params" or "NVSDK_NGX_D3D12_DLSSD_Eval_Params" or "NVSDK_NGX_VK_DLSSD_Eval_Params" when field.Text("name") is "pInWorldToViewMatrix" or "pInViewToClipMatrix" => "Matrix4x4*",
            _ => null
        };
    }
}

internal class CodeWriter
{
    private readonly StringBuilder text = new();

    private int indent;
    private bool blankLine;
    private bool blockStart;

    public override string ToString()
    {
        return text.ToString();
    }

    internal void Line(string value)
    {
        if (blankLine && text.Length is not 0)
        {
            text.Append('\n');
        }

        blankLine = false;
        blockStart = false;
        text.Append(' ', indent * 4);
        text.Append(value);
        text.Append('\n');
    }

    internal void BlankLine()
    {
        blankLine = !blockStart;
    }

    internal void BeginBlock(string declaration, bool continuation = false)
    {
        if (continuation)
        {
            blankLine = false;
        }

        Line(declaration);
        Line("{");
        indent++;
        blockStart = true;
    }

    internal void EndBlock(string suffix = "")
    {
        blankLine = false;
        indent--;
        Line("}" + suffix);
        BlankLine();
    }
}
