namespace NGX.NET.Generator;

internal class FunctionEmitter(TypeMapper mapper, Dictionary<string, string> files)
{
    private readonly HashSet<string> signatures = [];

    internal void WriteFunctions(string group, IEnumerable<AstFunction> source)
    {
        CodeWriter text = CreateFile();
        text.BeginBlock("public static unsafe partial class Ngx");

        if (group.Length is not 0)
        {
            text.BeginBlock($"public static partial class {group}");
        }

        AstFunction[] ordered = [.. source.OrderBy(static function => function.Name, StringComparer.Ordinal)];
        foreach (AstFunction function in ordered)
        {
            string method = FunctionName(function.Name).Method;
            string arguments = string.Join(", ", function.Parameters.Select(parameter => $"{mapper.NativeParameterType(parameter)} {NativeParameterName(parameter.Name)}"));
            string marshalling = function.Parameters.Any(static parameter => parameter.Role is ParameterRole.RequiredName) ? ", StringMarshalling = StringMarshalling.Utf8" : string.Empty;
            text.Line($"[LibraryImport(LibraryName, EntryPoint = \"{function.Export}\"{marshalling})]");
            text.Line("[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            text.Line($"private static partial {mapper.Type(function.Result)} {method}Native({arguments});");
            text.BlankLine();
        }

        text.BeginBlock($"static {(group.Length is 0 ? "Ngx" : group)}()");
        text.Line("NativeLoader.Register();");
        text.EndBlock();

        foreach (AstFunction function in ordered)
        {
            WriteFunction(text, PlanFunction(group, function));
        }

        if (group.Length is not 0)
        {
            text.EndBlock();
        }

        text.EndBlock();
        files[$"API/{(group.Length is 0 ? "Core" : group)}.g.cs"] = text.ToString();
    }

    private FunctionPlan PlanFunction(string group, AstFunction function)
    {
        string method = FunctionName(function.Name).Method;
        string nativeResult = mapper.Type(function.Result);
        string result = function.Result.Kind is NativeTypeKind.Pointer ? "string?" : mapper.PublicType(function.Result);
        bool init = method.StartsWith("Init", StringComparison.Ordinal);
        bool evaluate = function.Name.StartsWith("NGX_", StringComparison.Ordinal) && method.StartsWith("Evaluate", StringComparison.Ordinal);
        ParameterPlan[] parameters = [.. function.Parameters.Select(parameter => PlanParameter(parameter, init, evaluate))];

        return new(group, function, method, nativeResult, result, init, parameters);
    }

    private ParameterPlan PlanParameter(AstParameter parameter, bool initialization, bool evaluation)
    {
        string name = ParameterName(parameter.Name);
        AstType type = parameter.Type;
        string raw = mapper.Type(type);

        if (parameter.Role is ParameterRole.ExtensionCount)
        {
            return new(null, "out " + name, string.Empty) { Initializers = [$"uint {name} = 0;"] };
        }

        if (parameter.Role is ParameterRole.ExtensionProperties or ParameterRole.ExtensionNames)
        {
            return PlanExtensionArray(parameter, name);
        }

        if (type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            AstType element = type.Element!;

            if (element.Kind is NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto)
            {
                return PlanCallback(type, name);
            }

            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                return PlanString(parameter, name, initialization);
            }

            if (element.Kind is NativeTypeKind.Record && element.Name is "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle")
            {
                return PlanHandle(element, name);
            }

            if (parameter.Role is ParameterRole.CudaDevice)
            {
                return PlanCudaDevice(name);
            }

            if (mapper.IsRecord(element))
            {
                return PlanRecord(parameter, name, evaluation);
            }

            if (parameter.Direction is ParameterDirection.Out)
            {
                return PlanOutput(parameter, name);
            }

            return new($"nint {name}", raw is "nint" ? name : $"({raw}){name}", name) { Device = parameter.Role is ParameterRole.Device ? name : null };
        }

        if (mapper.IsRecord(type))
        {
            string local = name + "Native";
            string scope = mapper.Allocates(type.Name) ? ", scope" : string.Empty;

            return new($"in {mapper.PublicType(type)} {name}", local, "in " + name)
            {
                Setup = [writer => writer.Line($"{raw} {local} = new(in {name}{scope});")],
                UsesCallScope = scope.Length is not 0
            };
        }

        return new($"{mapper.PublicType(type)} {name}", name, name);
    }

    private ParameterPlan PlanExtensionArray(AstParameter parameter, string name)
    {
        bool properties = parameter.Role is ParameterRole.ExtensionProperties;
        string element = properties ? "NGXVkExtensionProperties" : "string";
        string native = properties ? "NGXVkExtensionPropertiesNative*" : "byte**";
        string local = "p" + char.ToUpperInvariant(name[0]) + name[1..];
        string count = ParameterName(parameter.CountParameter);

        return new($"out {element}[] {name}", "out " + local, "out " + name)
        {
            Initializers = [$"{name} = [];", $"{native} {local} = null;"],
            Outputs = [writer =>
            {
                writer.Line($"{name} = new {element}[checked((int){count})];");
                WriteGuard(writer, $"{name}.Length is not 0 && {local} is null", "throw new InvalidOperationException(\"NGX returned a null extension array.\");");
                writer.BeginBlock($"for (int i = 0; i < {name}.Length; i++)");
                writer.Line($"{name}[i] = {(properties ? $"new(in {local}[i])" : $"Marshal.PtrToStringUTF8((nint){local}[i])!")};");
                writer.EndBlock();
            }],
            Output = (element + "[]", name)
        };
    }

    private ParameterPlan PlanCallback(AstType type, string name)
    {
        if (mapper.CallbackName(type) is not string callback)
        {
            return new($"nint {name}", name, name);
        }

        string local = "guarded" + char.ToUpperInvariant(name[0]) + name[1..];

        return new($"{callback}? {name}", $"{local} is null ? 0 : Marshal.GetFunctionPointerForDelegate({local})", name)
        {
            Setup = [writer => writer.Line($"{callback}? {local} = CallbackGuard.Wrap({name});")],
            KeepAlive = local
        };
    }

    private ParameterPlan PlanHandle(AstType type, string name)
    {
        return new($"{mapper.ManagedRecord(type.Name)} {name}", name, name)
        {
            Setup = [writer => writer.Line($"ArgumentNullException.ThrowIfNull((void*){name}.Value, nameof({name}));")],
            ParameterHandle = type.Name is "NVSDK_NGX_Parameter" ? name : null,
            IsValidation = true
        };
    }

    private ParameterPlan PlanRecord(AstParameter parameter, string name, bool evaluation)
    {
        AstType element = parameter.Type.Element!;
        string managed = mapper.PublicType(element);
        string native = mapper.Type(element);
        bool output = parameter.Direction is ParameterDirection.Out;
        bool optional = parameter.Role is ParameterRole.OptionalRecord or ParameterRole.FrameGenerationOptions;
        string modifier = (output, optional) switch
        {
            (true, _) => "out ",
            (_, true) => string.Empty,
            _ => "in "
        };
        string declaration = $"{modifier}{managed}{(optional ? "?" : string.Empty)} {name}";
        string forward = modifier + name;
        string local = name + "Native";

        if (output)
        {
            return new(declaration, "out " + local, forward)
            {
                Initializers = [$"{name} = default;", $"{native} {local} = default;"],
                Outputs = [writer => writer.Line($"{name} = new(in {local});")],
                Output = (managed, name)
            };
        }

        if (evaluation && mapper.RequiresScope(element.Name))
        {
            return PlanRetainedRecord(parameter, declaration, forward, name, native, optional);
        }

        string scope = mapper.Allocates(element.Name) ? ", scope" : string.Empty;

        if (optional)
        {
            return new(declaration, $"{name}.HasValue ? &{local} : null", forward)
            {
                Initializers = [$"{native} {local} = default;"],
                Setup = [writer => WriteGuard(writer, $"{name} is {managed} {name}Value", $"{local} = new(in {name}Value{scope});")],
                Optional = (managed, name),
                UsesCallScope = scope.Length is not 0
            };
        }

        return new(declaration, "&" + local, forward)
        {
            Setup = [writer => writer.Line($"{native} {local} = new(in {name}{scope});")],
            UsesCallScope = scope.Length is not 0
        };
    }

    private ParameterPlan PlanRetainedRecord(AstParameter parameter, string declaration, string forward, string name, string native, bool optional)
    {
        AstType element = parameter.Type.Element!;
        string scope = name + "Scope";
        string pointer = "p" + char.ToUpperInvariant(name[0]) + name[1..];
        string conversionScope = mapper.Allocates(element.Name) ? ", " + scope : string.Empty;
        string managed = mapper.PublicType(element);

        if (optional)
        {
            return new(declaration, pointer, forward)
            {
                Initializers = [$"NativeScope? {scope} = null;", $"{native}* {pointer} = null;"],
                Setup = [writer =>
                {
                    writer.BlankLine();
                    writer.BeginBlock($"if ({name} is {managed} {name}Value)");
                    writer.Line($"{scope} = new();");
                    writer.Line($"{pointer} = {scope}.Alloc(new {native}(in {name}Value{conversionScope}));");
                    writer.EndBlock();
                }],
                Optional = (managed, name),
                RetainedScope = scope,
                RetainedOptional = true,
                SlotParameter = parameter.Name
            };
        }

        return new(declaration, pointer, forward)
        {
            Setup = [writer =>
            {
                writer.Line($"NativeScope {scope} = new();");
                writer.Line($"{native}* {pointer} = {scope}.Alloc(new {native}(in {name}{conversionScope}));");
            }],
            RetainedScope = scope,
            SlotParameter = parameter.Name
        };
    }

    private ParameterPlan PlanOutput(AstParameter parameter, string name)
    {
        string managed = mapper.NativeParameterType(parameter)[4..];

        return new($"out {managed} {name}", "out " + name, "out " + name)
        {
            Initializers = [$"{name} = default;"],
            Output = (managed, name)
        };
    }

    private void WriteFunction(CodeWriter text, FunctionPlan plan)
    {
        RegisterFunction(plan.Group, plan.Method, plan.Declarations);
        text.BeginBlock($"public static {plan.Result} {plan.Method}({string.Join(", ", plan.Declarations)})");
        WriteBody(text, plan);
        text.EndBlock();

        if (plan.IsStatus && plan.Outputs.Length is 1)
        {
            WriteValueFunction(text, plan, plan.Declarations, plan.Forward);
        }

        if (plan.Optional.Length is not 0)
        {
            WriteOptionalFunction(text, plan);
        }
    }

    private void RegisterFunction(string group, string method, IReadOnlyList<string> declarations)
    {
        string parameters = string.Join(", ", declarations.Select(static declaration => Regex.Replace(declaration[..declaration.LastIndexOf(' ')], @"^(?:in|out|ref) ", "ref ")));
        string signature = $"{group}.{method}({parameters})";
        if (!signatures.Add(signature))
        {
            throw new InvalidOperationException($"Conflicting managed overload: {signature}.");
        }
    }

    private void WriteValueFunction(CodeWriter text, FunctionPlan plan, IReadOnlyList<string> declarations, IReadOnlyList<string> forward)
    {
        (int index, string type, string name) = plan.Outputs[0];
        string[] inputs = [.. declarations.Where((_, parameterIndex) => parameterIndex != index)];
        string[] arguments = [.. forward];
        arguments[index] = $"out {type} {name}";
        string operation = plan.Group.Length is 0 ? $"Ngx.{plan.Method}" : $"Ngx.{plan.Group}.{plan.Method}";
        RegisterFunction(plan.Group, plan.Method, inputs);
        text.BeginBlock($"public static {type} {plan.Method}({string.Join(", ", inputs)})");
        text.Line($"{plan.Method}({string.Join(", ", arguments)}).CheckError(\"{operation}\");");
        text.BlankLine();
        text.Line($"return {name};");
        text.EndBlock();
    }

    private void WriteBody(CodeWriter text, FunctionPlan plan)
    {
        foreach (string local in plan.Parameters.SelectMany(static parameter => parameter.Initializers))
        {
            text.Line(local);
        }

        text.BlankLine();

        if (plan.IsInitialization)
        {
            text.Line("NativeScope scope = new();");
            text.BlankLine();
        }
        else if (plan.UsesCallScope)
        {
            text.Line("using NativeScope scope = new();");
            text.BlankLine();
        }

        bool previousValidation = false;
        foreach (ParameterPlan parameter in plan.Parameters)
        {
            if (previousValidation && !parameter.IsValidation)
            {
                text.BlankLine();
            }

            foreach (Action<CodeWriter> statement in parameter.Setup)
            {
                statement(text);
            }

            if (parameter.Setup.Length is not 0)
            {
                previousValidation = parameter.IsValidation;
            }
        }

        if (previousValidation)
        {
            text.BlankLine();
        }

        string arguments = string.Join(", ", plan.Parameters.Select(static parameter => parameter.Argument));
        if (plan.CanReturnDirectly)
        {
            text.BlankLine();
            text.Line($"return {plan.Method}Native({arguments});");

            return;
        }

        text.Line($"{(plan.Result is "void" ? string.Empty : $"{plan.NativeResult} result = ")}{plan.Method}Native({arguments});");

        foreach (ParameterPlan parameter in plan.Parameters.Where(static parameter => parameter.KeepAlive is not null))
        {
            text.Line($"GC.KeepAlive({parameter.KeepAlive});");
        }

        WriteLifetime(text, plan);
        WriteOutputs(text, plan);

        if (plan.Result is not "void")
        {
            string returned = plan.Function.Result.Kind switch
            {
                NativeTypeKind.Record when mapper.IsRecord(plan.Function.Result) => "new(in result)",
                NativeTypeKind.Pointer => "NativeTextHelper.ReadWide(result)",
                _ => "result"
            };
            text.BlankLine();
            text.Line($"return {returned};");
        }
    }

    private void WriteOptionalFunction(CodeWriter text, FunctionPlan plan)
    {
        string[] nonNullable = [.. plan.Declarations];
        string[] arguments = [.. plan.Forward];
        foreach ((int index, string type, string name) in plan.Optional)
        {
            nonNullable[index] = $"in {type} {name}";
            arguments[index] = $"({type}?){name}";
        }

        RegisterFunction(plan.Group, plan.Method, nonNullable);
        text.BeginBlock($"public static {plan.Result} {plan.Method}({string.Join(", ", nonNullable)})");
        text.Line($"{(plan.Result is "void" ? string.Empty : "return ")}{plan.Method}({string.Join(", ", arguments)});");
        text.EndBlock();

        if (plan.IsStatus && plan.Outputs.Length is 1)
        {
            WriteValueFunction(text, plan, nonNullable, arguments);
        }
    }

    private static ParameterPlan PlanString(AstParameter parameter, string name, bool initialization)
    {
        if (parameter.Role is ParameterRole.RequiredName)
        {
            return new($"string {name}", name, name)
            {
                Setup = [writer => writer.Line($"ArgumentNullException.ThrowIfNull({name});")],
                IsValidation = true
            };
        }

        string allocation = parameter.Type.Element!.Kind is NativeTypeKind.WChar ? "AllocWide" : "AllocUtf8";

        return new($"string? {name}", $"scope.{allocation}({name})", name) { UsesCallScope = !initialization };
    }

    private static ParameterPlan PlanCudaDevice(string name)
    {
        string local = "p" + char.ToUpperInvariant(name[0]) + name[1..];

        return new($"NGXCUDADevice? {name}", local, name)
        {
            Setup = [writer => writer.Line($"NGXCUDADeviceNative* {local} = {name} is NGXCUDADevice {name}Value ? NativeLifetime.GetCudaDevice({name}Value) : null;")],
            Optional = ("NGXCUDADevice", name),
            Device = "(nint)" + local
        };
    }

    private static void WriteLifetime(CodeWriter text, FunctionPlan plan)
    {
        if (plan.IsInitialization)
        {
            text.Line($"NativeLifetime.Retain({GraphicsApi(plan.Group)}, {plan.Device}, scope, result);");
        }

        foreach (ParameterPlan parameter in plan.Parameters.Where(static parameter => parameter.RetainedScope is not null))
        {
            string retain = $"NativeLifetime.Retain({GraphicsApi(plan.Group)}, {plan.ParameterHandle}, \"{plan.Group}.{plan.Method}.{parameter.SlotParameter}\", {parameter.RetainedScope}, result);";

            if (parameter.RetainedOptional)
            {
                WriteGuard(text, $"{parameter.RetainedScope} is not null", retain);
            }
            else
            {
                text.Line(retain);
            }
        }

        if (plan.Method is "DestroyParameters")
        {
            WriteGuard(text, "result.IsSuccess", $"NativeLifetime.Release({plan.ParameterHandle});");
        }

        if (plan.IsShutdown)
        {
            WriteGuard(text, "result.IsSuccess", $"NativeLifetime.Release({GraphicsApi(plan.Group)}, {plan.Device});");
        }
    }

    private static void WriteOutputs(CodeWriter text, FunctionPlan plan)
    {
        bool conversions = plan.Parameters.Any(static parameter => parameter.Outputs.Length is not 0);
        if (!conversions && plan.Outputs.Length is 0)
        {
            return;
        }

        if (conversions)
        {
            if (plan.IsStatus)
            {
                text.BeginBlock("if (result.IsSuccess)");
            }

            foreach (Action<CodeWriter> output in plan.Parameters.SelectMany(static parameter => parameter.Outputs))
            {
                output(text);
            }

            if (plan.IsStatus)
            {
                text.EndBlock();
                text.BeginBlock("else", continuation: true);
                WriteOutputReset(text, plan);
                text.EndBlock();
            }

            return;
        }

        if (plan.IsStatus)
        {
            text.BeginBlock("if (result.IsFailure)");
            WriteOutputReset(text, plan);
            text.EndBlock();
        }
    }

    private static void WriteOutputReset(CodeWriter text, FunctionPlan plan)
    {
        foreach ((int _, string type, string name) in plan.Outputs)
        {
            text.Line($"{name} = {(type.EndsWith("[]", StringComparison.Ordinal) ? "[]" : "default")};");
        }
    }

    private static string GraphicsApi(string group)
    {
        return "NGXGraphicsAPI." + (group is "CUDA" ? "Cuda" : group);
    }

    private class ParameterPlan(string? declaration, string argument, string forward)
    {
        public readonly string? Declaration = declaration;

        public readonly string Argument = argument;

        public readonly string Forward = forward;

        public string[] Initializers { get; init; } = [];

        public Action<CodeWriter>[] Setup { get; init; } = [];

        public Action<CodeWriter>[] Outputs { get; init; } = [];

        public (string Type, string Name)? Output { get; init; }

        public (string Type, string Name)? Optional { get; init; }

        public string? Device { get; init; }

        public string? ParameterHandle { get; init; }

        public string? KeepAlive { get; init; }

        public string? RetainedScope { get; init; }

        public bool RetainedOptional { get; init; }

        public string? SlotParameter { get; init; }

        public bool UsesCallScope { get; init; }

        public bool IsValidation { get; init; }
    }

    private class FunctionPlan(string group, AstFunction function, string method, string nativeResult, string result, bool initialization, ParameterPlan[] parameters)
    {
        public readonly string Group = group;

        public readonly AstFunction Function = function;

        public readonly string Method = method;

        public readonly string NativeResult = nativeResult;

        public readonly string Result = result;

        public readonly bool IsInitialization = initialization;

        public readonly ParameterPlan[] Parameters = parameters;

        public readonly string[] Declarations = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static parameter => parameter.Declaration!)];

        public readonly string[] Forward = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static parameter => parameter.Forward)];

        public readonly (int Index, string Type, string Name)[] Outputs = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static (parameter, index) => (Parameter: parameter, Index: index)).Where(static item => item.Parameter.Output.HasValue).Select(static item => (item.Index, item.Parameter.Output!.Value.Type, item.Parameter.Output.Value.Name))];

        public readonly (int Index, string Type, string Name)[] Optional = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static (parameter, index) => (Parameter: parameter, Index: index)).Where(static item => item.Parameter.Optional.HasValue).Select(static item => (item.Index, item.Parameter.Optional!.Value.Type, item.Parameter.Optional.Value.Name))];

        public string Device => Parameters.Select(static parameter => parameter.Device).LastOrDefault(static value => value is not null) ?? "0";

        public string ParameterHandle => Parameters.Select(static parameter => parameter.ParameterHandle).LastOrDefault(static value => value is not null) ?? "default";

        public bool UsesCallScope => Parameters.Any(static parameter => parameter.UsesCallScope);

        public bool IsStatus => Result is "NGXResult";

        public bool IsShutdown => Method.StartsWith("Shutdown", StringComparison.Ordinal);

        public bool CanReturnDirectly => !IsInitialization && !Parameters.Any(static parameter => parameter.RetainedScope is not null || parameter.KeepAlive is not null) && Outputs.Length is 0 && Result is not "void" && Result == NativeResult && Method is not "DestroyParameters" && !IsShutdown;
    }
}
