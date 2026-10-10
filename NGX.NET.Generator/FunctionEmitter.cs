namespace NGX.NET.Generator;

internal class FunctionEmitter(TypeMapper mapper, Dictionary<string, string> files, ResultEmitter results)
{
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
            text.Line($"[LibraryImport(LibraryName, EntryPoint = \"{function.Export}\")]");
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
        ParameterPlan[] parameters = [.. function.Parameters.Select(parameter => PlanParameter(parameter, init || evaluate))];

        return new(group, function, method, nativeResult, result, init, evaluate, parameters);
    }

    private ParameterPlan PlanParameter(AstParameter parameter, bool retained)
    {
        string name = ParameterName(parameter.Name);
        string local = LocalName(parameter, name);
        AstType type = parameter.Type;
        string raw = mapper.Type(type);

        if (parameter.Role is ParameterRole.ExtensionCount)
        {
            return new(null, "out " + local, string.Empty) { Locals = [$"uint {local} = 0;"] };
        }

        if (parameter.Role is ParameterRole.ExtensionProperties or ParameterRole.ExtensionNames)
        {
            return PlanExtensionArray(parameter, name, local);
        }

        if (type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference)
        {
            AstType element = type.Element!;

            if (element.Kind is NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto)
            {
                return PlanCallback(type, name, local);
            }

            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                return PlanString(parameter, name, local, raw, retained);
            }

            if (element.Kind is NativeTypeKind.Record && element.Name is "NVSDK_NGX_Parameter" or "NVSDK_NGX_Handle")
            {
                return PlanHandle(element, name);
            }

            if (parameter.Role is ParameterRole.CudaDevice)
            {
                return PlanCudaDevice(name, local);
            }

            if (mapper.IsRecord(element))
            {
                return PlanRecord(parameter, name, local, retained);
            }

            if (parameter.Direction is ParameterDirection.Out)
            {
                return PlanOutput(parameter, name);
            }

            return new($"nint {name}", raw is "nint" ? name : $"({raw}){name}", name) { Device = parameter.Role is ParameterRole.Device ? name : null };
        }

        if (mapper.IsRecord(type))
        {
            return new($"in {mapper.PublicType(type)} {name}", local, "in " + name)
            {
                Locals = [$"{raw} {local} = default;"],
                Setup = [writer => writer.Line($"{local} = new(in {name});")],
                Cleanup = [$"{local}.Dispose();"]
            };
        }

        return new($"{mapper.PublicType(type)} {name}", name, name);
    }

    private ParameterPlan PlanExtensionArray(AstParameter parameter, string name, string local)
    {
        bool properties = parameter.Role is ParameterRole.ExtensionProperties;
        string element = properties ? "NGXVkExtensionProperties" : "string";
        string native = properties ? "NGXVkExtensionPropertiesNative*" : "sbyte**";
        string count = ParameterName(parameter.CountParameter);

        return new($"out {element}[] {name}", "out " + local, "out " + name)
        {
            Locals = [$"{name} = [];", $"{native} {local} = null;"],
            Outputs = [writer =>
            {
                writer.Line($"{name} = new {element}[checked((int){count})];");
                WriteGuard(writer, $"{name}.Length is not 0 && {local} is null", "throw new InvalidOperationException(\"NGX returned a null extension array.\");");
                writer.BeginBlock($"for (int i = 0; i < {name}.Length; i++)");
                writer.Line($"{name}[i] = {(properties ? $"new(in {local}[i])" : $"NGXMarshal.PtrToString({local}[i], NGXEncoding.Utf8)!")};");
                writer.EndBlock();
            }],
            Output = (element + "[]", name),
            NativeOutputName = parameter.Name
        };
    }

    private ParameterPlan PlanCallback(AstType type, string name, string local)
    {
        if (mapper.CallbackName(type) is not string callback)
        {
            return new($"nint {name}", name, name);
        }

        return new($"{callback}? {name}", local, name)
        {
            Locals = [$"nint {local} = 0;"],
            Setup = [writer => writer.Line($"{local} = NgxCallbacks.Acquire({name});")],
            Cleanup = [$"NgxCallbacks.Release({local});"]
        };
    }

    private ParameterPlan PlanHandle(AstType type, string name)
    {
        return new($"{mapper.ManagedRecord(type.Name)} {name}", name, name)
        {
            Setup = [writer => WriteGuard(writer, $"{name}.IsNull", $"throw new ArgumentException(\"A non-null NGX handle is required.\", nameof({name}));")],
            ParameterHandle = type.Name is "NVSDK_NGX_Parameter" ? name + ".Value" : null
        };
    }

    private ParameterPlan PlanRecord(AstParameter parameter, string name, string local, bool retained)
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

        if (output)
        {
            return new(declaration, "out " + local, forward)
            {
                Locals = [$"{native} {local} = default;", $"{name} = default;"],
                Outputs = [writer => writer.Line($"{name} = new(in {local});")],
                Output = (managed, name),
                NativeOutputName = parameter.Name
            };
        }

        List<Action<CodeWriter>> setup = [];

        if (parameter.Role is ParameterRole.FrameGenerationOptions)
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

        List<string> locals = [$"{native} {local} = default;"];
        string argument = optional ? $"{name}.HasValue ? &{local} : null" : "&" + local;

        if (retained)
        {
            string pointer = "p" + char.ToUpperInvariant(name[0]) + name[1..];
            locals.Add($"{native}* {pointer} = null;");
            setup.Add(writer => writer.Line($"{pointer} = {(optional ? name + ".HasValue ? " : string.Empty)}storage!.Take(ref {local}){(optional ? " : null" : string.Empty)};"));
            argument = pointer;
        }

        return new(declaration, argument, forward)
        {
            Locals = [.. locals],
            Setup = [.. setup],
            Cleanup = [$"{local}.Dispose();"],
            Optional = optional ? (managed, name) : null
        };
    }

    private ParameterPlan PlanOutput(AstParameter parameter, string name)
    {
        string managed = mapper.NativeParameterType(parameter)[4..];

        return new($"out {managed} {name}", "out " + name, "out " + name)
        {
            Locals = [$"{name} = default;"],
            Output = (managed, name),
            NativeOutputName = parameter.Name,
            Allocated = managed is "NGXParameter" ? name + ".Value" : null
        };
    }

    private void WriteFunction(CodeWriter text, FunctionPlan plan)
    {
        results.RegisterFunction(plan.Group, plan.Method, plan.Declarations);
        text.BeginBlock($"public static {plan.Result} {plan.Method}({string.Join(", ", plan.Declarations)})");
        WriteBody(text, plan);
        text.EndBlock();

        if (plan.IsStatus && plan.Outputs.Length is not 0)
        {
            results.WriteResultFunction(text, plan.Group, plan.Method, plan.Declarations, plan.Forward, plan.Outputs);
        }

        if (plan.Optional.Length is not 0)
        {
            WriteOptionalFunction(text, plan);
        }
    }

    private void WriteBody(CodeWriter text, FunctionPlan plan)
    {
        if (plan.IsRetained)
        {
            text.Line("NativeCall? storage = new();");
            text.Line("NGXResult result = NGXResult.Fail;");
            text.Line("bool attached = false;");
            text.Line("bool returned = false;");
        }

        foreach (string local in plan.Parameters.SelectMany(static parameter => parameter.Locals))
        {
            text.Line(local);
        }

        if (plan.HasCudaDevice)
        {
            text.Line("bool cudaSucceeded = false;");
        }

        if (plan.IsGuarded)
        {
            text.BlankLine();
            text.BeginBlock("try");
        }

        foreach (Action<CodeWriter> statement in plan.Parameters.SelectMany(static parameter => parameter.Setup))
        {
            statement(text);
        }

        if (plan.Allocated.Length is not 0)
        {
            text.Line("NgxLifetime.PrepareParameters();");
        }

        if (plan.IsInitialization)
        {
            text.Line($"NgxLifetime.BeginInitialization(\"{plan.Group}\", {plan.Device}, storage!);");
            text.Line("attached = true;");
        }

        if (plan.IsEvaluation)
        {
            text.Line($"NgxLifetime.BeginParameters({plan.ParameterHandle}, \"{plan.Group}.{plan.Method}\", storage!);");
            text.Line("attached = true;");
        }

        if (plan.CanReturnDirectly)
        {
            text.BlankLine();
            text.Line($"return {plan.Method}Native({string.Join(", ", plan.Parameters.Select(static parameter => parameter.Argument))});");

            return;
        }

        string assignment = (plan.Result, plan.IsRetained) switch
        {
            ("void", _) => string.Empty,
            (_, true) => "result = ",
            _ => $"{plan.NativeResult} result = "
        };
        text.Line($"{assignment}{plan.Method}Native({string.Join(", ", plan.Parameters.Select(static parameter => parameter.Argument))});");

        if (plan.IsRetained)
        {
            text.Line("returned = true;");
        }

        WriteOutputs(text, plan);

        if (plan.Method is "DestroyParameters")
        {
            WriteGuard(text, "result is NGXResult.Success", $"NgxLifetime.ReleaseParameters({plan.ParameterHandle}, destroyed: true);");
        }

        if (plan.IsShutdown)
        {
            WriteGuard(text, "result is NGXResult.Success", $"NgxLifetime.Shutdown(\"{plan.Group}\", {plan.Device});");
        }

        if (plan.HasCudaDevice && !plan.IsShutdown)
        {
            text.Line("cudaSucceeded = result is NGXResult.Success;");
        }

        if (plan.Result is not "void")
        {
            string returned = plan.Function.Result.Kind switch
            {
                NativeTypeKind.Record when mapper.IsRecord(plan.Function.Result) => "new(in result)",
                NativeTypeKind.Pointer => "NGXMarshal.PtrToString(result, NGXEncoding.NativeWide)",
                _ => "result"
            };
            text.BlankLine();
            text.Line($"return {returned};");
        }

        if (plan.IsGuarded)
        {
            WriteCleanup(text, plan);
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

        results.RegisterFunction(plan.Group, plan.Method, nonNullable);
        text.BeginBlock($"public static {plan.Result} {plan.Method}({string.Join(", ", nonNullable)})");
        text.Line($"{(plan.Result is "void" ? string.Empty : "return ")}{plan.Method}({string.Join(", ", arguments)});");
        text.EndBlock();

        if (plan.IsStatus && plan.Outputs.Length is not 0)
        {
            results.WriteResultFunction(text, plan.Group, plan.Method, nonNullable, arguments, plan.Outputs);
        }
    }

    private static ParameterPlan PlanString(AstParameter parameter, string name, string local, string raw, bool retained)
    {
        string encoding = parameter.Type.Element!.Kind is NativeTypeKind.WChar ? "NativeWide" : "Utf8";
        bool required = parameter.Role is ParameterRole.RequiredName;
        List<Action<CodeWriter>> setup = [];

        if (required)
        {
            setup.Add(writer => writer.Line($"ArgumentNullException.ThrowIfNull({name});"));
        }

        setup.Add(writer => writer.Line($"{local} = {(raw is "void*" ? string.Empty : $"({raw})")}{(retained ? "storage!.String" : "NGXMarshal.StringToPtr")}({name}, NGXEncoding.{encoding});"));

        return new($"string{(required ? string.Empty : "?")} {name}", local, name)
        {
            Locals = [$"{raw} {local} = null;"],
            Setup = [.. setup],
            Cleanup = retained ? [] : [$"NGXMarshal.Free({local});"]
        };
    }

    private static ParameterPlan PlanCudaDevice(string name, string local)
    {
        return new($"NGXCUDADevice? {name}", local, name)
        {
            Locals = [$"NGXCUDADeviceNative* {local} = null;"],
            Setup = [writer => WriteGuard(writer, $"{name} is NGXCUDADevice deviceValue", $"{local} = NgxLifetime.CudaDevice(deviceValue);")],
            Optional = ("NGXCUDADevice", name),
            Device = "(nint)" + local,
            HasCudaDevice = true
        };
    }

    private static void WriteOutputs(CodeWriter text, FunctionPlan plan)
    {
        bool conversions = plan.Parameters.Any(static parameter => parameter.Outputs.Length is not 0);
        if (!conversions && plan.Outputs.Length is 0)
        {
            return;
        }

        if (conversions || plan.Allocated.Length is not 0)
        {
            if (plan.IsStatus)
            {
                text.BeginBlock("if (result is NGXResult.Success)");
            }

            foreach (Action<CodeWriter> output in plan.Parameters.SelectMany(static parameter => parameter.Outputs))
            {
                output(text);
            }

            if (plan.Allocated.Length is not 0)
            {
                text.Line($"NgxLifetime.RegisterParameters(\"{plan.Group}\", {plan.Allocated});");
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
            text.BeginBlock("if (result is not NGXResult.Success)");
            WriteOutputReset(text, plan);
            text.EndBlock();
        }
    }

    private static void WriteOutputReset(CodeWriter text, FunctionPlan plan)
    {
        foreach ((int _, string type, string name, string _) in plan.Outputs)
        {
            text.Line($"{name} = {(type.EndsWith("[]", StringComparison.Ordinal) ? "[]" : "default")};");
        }
    }

    private static string LocalName(AstParameter parameter, string name)
    {
        if (parameter.Role is ParameterRole.ExtensionCount)
        {
            return name;
        }

        if (parameter.Role is ParameterRole.ExtensionProperties or ParameterRole.ExtensionNames)
        {
            return "p" + char.ToUpperInvariant(name[0]) + name[1..];
        }

        if (parameter.Type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference && parameter.Type.Element!.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
        {
            return "p" + char.ToUpperInvariant(name[0]) + name[1..];
        }

        if (parameter.Role is ParameterRole.CudaDevice)
        {
            return "p" + char.ToUpperInvariant(name[0]) + name[1..];
        }

        return name + "Native";
    }

    private static void WriteCleanup(CodeWriter text, FunctionPlan plan)
    {
        text.EndBlock();
        text.BeginBlock("finally", continuation: true);

        if (plan.IsInitialization)
        {
            WriteGuard(text, "attached", $"NgxLifetime.EndInitialization(\"{plan.Group}\", {plan.Device}, returned && result is NGXResult.Success, ref storage);");
        }

        if (plan.IsEvaluation)
        {
            WriteGuard(text, "attached", $"NgxLifetime.EndParameters({plan.ParameterHandle}, \"{plan.Group}.{plan.Method}\", returned, result is NGXResult.Success, ref storage);");
        }

        if (plan.IsRetained)
        {
            text.Line("storage?.Dispose();");
        }

        foreach (string statement in plan.Parameters.SelectMany(static parameter => parameter.Cleanup).Reverse())
        {
            text.Line(statement);
        }

        if (plan.HasCudaDevice)
        {
            text.Line($"NgxLifetime.FinishCudaDevice({plan.Device}, cudaSucceeded);");
        }

        text.EndBlock();
    }

    private class ParameterPlan(string? declaration, string argument, string forward)
    {
        public readonly string? Declaration = declaration;

        public readonly string Argument = argument;

        public readonly string Forward = forward;

        public string[] Locals { get; init; } = [];

        public Action<CodeWriter>[] Setup { get; init; } = [];

        public string[] Cleanup { get; init; } = [];

        public Action<CodeWriter>[] Outputs { get; init; } = [];

        public (string Type, string Name)? Output { get; init; }

        public string? NativeOutputName { get; init; }

        public (string Type, string Name)? Optional { get; init; }

        public string? Device { get; init; }

        public string? ParameterHandle { get; init; }

        public string? Allocated { get; init; }

        public bool HasCudaDevice { get; init; }
    }

    private class FunctionPlan(string group, AstFunction function, string method, string nativeResult, string result, bool initialization, bool evaluation, ParameterPlan[] parameters)
    {
        public readonly string Group = group;

        public readonly AstFunction Function = function;

        public readonly string Method = method;

        public readonly string NativeResult = nativeResult;

        public readonly string Result = result;

        public readonly bool IsInitialization = initialization;

        public readonly bool IsEvaluation = evaluation;

        public readonly ParameterPlan[] Parameters = parameters;

        public readonly string[] Declarations = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static parameter => parameter.Declaration!)];

        public readonly string[] Forward = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static parameter => parameter.Forward)];

        public readonly (int Index, string Type, string Name, string NativeName)[] Outputs = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static (parameter, index) => (Parameter: parameter, Index: index)).Where(static item => item.Parameter.Output.HasValue).Select(static item => (item.Index, item.Parameter.Output!.Value.Type, item.Parameter.Output.Value.Name, item.Parameter.NativeOutputName!))];

        public readonly (int Index, string Type, string Name)[] Optional = [.. parameters.Where(static parameter => parameter.Declaration is not null).Select(static (parameter, index) => (Parameter: parameter, Index: index)).Where(static item => item.Parameter.Optional.HasValue).Select(static item => (item.Index, item.Parameter.Optional!.Value.Type, item.Parameter.Optional.Value.Name))];

        public string Device => Parameters.Select(static parameter => parameter.Device).LastOrDefault(static value => value is not null) ?? "0";

        public string ParameterHandle => Parameters.Select(static parameter => parameter.ParameterHandle).LastOrDefault(static value => value is not null) ?? "0";

        public string Allocated => Parameters.Select(static parameter => parameter.Allocated).LastOrDefault(static value => value is not null) ?? string.Empty;

        public bool IsRetained => IsInitialization || IsEvaluation;

        public bool IsStatus => Result is "NGXResult";

        public bool HasCudaDevice => Parameters.Any(static parameter => parameter.HasCudaDevice);

        public bool IsGuarded => IsRetained || Parameters.Any(static parameter => parameter.Cleanup.Length is not 0) || HasCudaDevice;

        public bool IsShutdown => Method.StartsWith("Shutdown", StringComparison.Ordinal);

        public bool CanReturnDirectly => !IsGuarded && Outputs.Length is 0 && Result is not "void" && Result == NativeResult && Method is not "DestroyParameters" && !IsShutdown;
    }
}
