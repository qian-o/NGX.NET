namespace NGX.NET.Generator;

internal class FunctionEmitter(TypeMapper mapper, Dictionary<string, string> files)
{
    private static readonly HashSet<string> BorrowedRecords =
    [
        "NVSDK_NGX_Application_Identifier",
        "NVSDK_NGX_FeatureDiscoveryInfo",
        "NVSDK_NGX_Feature_Create_Params",
        "NVSDK_NGX_DLSS_Create_Params",
        "NVSDK_NGX_DLSSD_Create_Params",
        "NVSDK_NGX_DLSSG_Create_Params",
        "NVSDK_NGX_CUDA_DLSSD_Create_Params",
        "NVSDK_NGX_CUDA_DLISP_Eval_Params",
        "NVSDK_NGX_CUDA_DLSSD_Eval_Params",
        "NVSDK_NGX_D3D11_DLISP_Eval_Params",
        "NVSDK_NGX_D3D11_DLSS_Eval_Params",
        "NVSDK_NGX_D3D11_DLSSD_Eval_Params",
        "NVSDK_NGX_D3D12_DLISP_Eval_Params",
        "NVSDK_NGX_D3D12_DLSS_Eval_Params",
        "NVSDK_NGX_D3D12_DLSSD_Eval_Params",
        "NVSDK_NGX_D3D12_DLSSG_Eval_Params",
        "NVSDK_NGX_VK_DLISP_Eval_Params",
        "NVSDK_NGX_VK_DLSS_Eval_Params",
        "NVSDK_NGX_VK_DLSSD_Eval_Params",
        "NVSDK_NGX_VK_DLSSG_Eval_Params"
    ];

    internal void WriteFunctions(string group, IEnumerable<AstFunction> source)
    {
        CodeWriter text = CreateFile();
        text.BeginBlock("public static unsafe partial class Ngx");

        if (group.Length is not 0)
        {
            text.BeginBlock($"public static partial class {group}");
        }

        AstFunction[] ordered = [.. source.OrderBy(static function => function.Name, StringComparer.Ordinal)];
        ValidateNames(ordered.Select(static function => (function.Name, GetFunctionName(function.Name).Method)), $"functions in {(group.Length is 0 ? "Ngx" : group)}");
        foreach (AstFunction function in ordered)
        {
            ValidateNames(function.Parameters.Select(static parameter => (parameter.Name, GetParameterName(parameter.Name))), $"parameters in {function.Name}");
            string method = GetFunctionName(function.Name).Method;
            string parameters = string.Join(", ", function.Parameters.Select(parameter =>
            {
                string managed = mapper.GetManagedType(parameter.Type);

                return $"{managed} {GetParameterName(parameter.Name)}{GetDefaultValue(function, parameter, managed, isManagedOverload: false)}";
            }));
            text.WriteLine($"[LibraryImport(LibraryName, EntryPoint = \"{function.Export}\")]");
            text.WriteLine("[UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            text.WriteLine($"public static partial {mapper.GetManagedType(function.Result)} {method}({parameters});");
            text.WriteBlankLine();
        }

        text.BeginBlock($"static {(group.Length is 0 ? "Ngx" : group)}()");
        text.WriteLine("NativeLoader.Register();");
        text.EndBlock();

        foreach (AstFunction function in ordered)
        {
            WriteOverload(text, function, useSpanName: false);
            if (function.Name.StartsWith("NVSDK_NGX_Parameter_", StringComparison.Ordinal))
            {
                WriteOverload(text, function, useSpanName: true);
            }
        }

        if (group.Length is not 0)
        {
            text.EndBlock();
        }

        text.EndBlock();
        files.Add($"API/{(group.Length is 0 ? "Core" : group)}.g.cs", text.ToString());
    }

    private void WriteOverload(CodeWriter text, AstFunction function, bool useSpanName)
    {
        string[] declarations = new string[function.Parameters.Length];
        string[] arguments = new string[function.Parameters.Length];
        List<string> fixedStatements = [];
        List<string> strings = [];
        bool hasOverload = false;
        string method = GetFunctionName(function.Name).Method;
        bool isInitMethod = method.StartsWith("Init", StringComparison.Ordinal);
        string? spanName = null;
        string? spanPointer = null;
        for (int i = 0; i < function.Parameters.Length; i++)
        {
            AstParameter parameter = function.Parameters[i];
            AstType type = parameter.Type;
            string native = mapper.GetManagedType(type);
            string name = GetParameterName(parameter.Name);
            string identifier = name.TrimStart('@');
            string pointer = $"p{char.ToUpperInvariant(identifier[0])}{identifier[1..]}";
            declarations[i] = $"{native} {name}{GetDefaultValue(function, parameter, native, isManagedOverload: true)}";
            arguments[i] = name;

            if (type.Kind is NativeTypeKind.Bool)
            {
                declarations[i] = $"bool {name}";
                arguments[i] = $"{name} ? (byte)1 : (byte)0";
                hasOverload = true;
            }
            else if (type.Element is { Kind: NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar } element && (parameter.Name is "InName" || isInitMethod))
            {
                if (useSpanName)
                {
                    declarations[i] = $"ReadOnlySpan<byte> {name}";
                    arguments[i] = pointer;
                    spanName = name;
                    spanPointer = pointer;
                    hasOverload = true;
                }
                else
                {
                    string allocation = element.Kind is NativeTypeKind.WChar ? "AllocWide" : "AllocUtf8";
                    declarations[i] = $"string {name}";
                    arguments[i] = pointer;
                    strings.Add($"{native} {pointer} = strings.{allocation}({name});");
                    hasOverload = true;
                }
            }
            else if (function.Name is "NVSDK_NGX_VULKAN_Init" or "NVSDK_NGX_VULKAN_Init_with_ProjectID" && type.Cpp is "PFN_vkGetInstanceProcAddr" or "PFN_vkGetDeviceProcAddr")
            {
                declarations[i] = $"nint {name}{GetDefaultValue(function, parameter, native, isManagedOverload: true)}";
                arguments[i] = $"({native}){name}";
                hasOverload = true;
            }
            else if (function.Name is "NVSDK_NGX_Parameter_GetVoidPointer" && parameter.Name is "OutValue" && type is { Kind: NativeTypeKind.Pointer, Element: { Kind: NativeTypeKind.Pointer, Element.Kind: NativeTypeKind.Void } })
            {
                declarations[i] = $"out nint {name}";
                arguments[i] = $"({native}){pointer}";
                fixedStatements.Add($"fixed (nint* {pointer} = &{name})");
                hasOverload = true;
            }
            else if (function.Name is "NVSDK_NGX_Parameter_SetVoidPointer" && parameter.Name is "InValue" && type is { Kind: NativeTypeKind.Pointer, Element.Kind: NativeTypeKind.Void })
            {
                declarations[i] = $"nint {name}";
                arguments[i] = $"({native}){name}";
                hasOverload = true;
            }
            else
            {
                string modifier = GetBorrowModifier(parameter);
                if (modifier.Length is not 0)
                {
                    string value = native[..^1];
                    arguments[i] = pointer;
                    declarations[i] = $"{modifier} {value} {name}";
                    fixedStatements.Add($"fixed ({value}* {pointer} = &{name})");
                    hasOverload = true;
                }
            }
        }

        if (!hasOverload)
        {
            return;
        }

        text.BeginBlock($"public static {mapper.GetManagedType(function.Result)} {method}({string.Join(", ", declarations)})");

        if (spanName is not null && spanPointer is not null)
        {
            text.BeginBlock($"if (!{spanName}.IsEmpty && ({spanName}[^1] is 0))");
            text.BeginBlock($"fixed (byte* {spanPointer} = {spanName})");
            WriteInvocation(text, function.Result, method, arguments, fixedStatements);
            text.EndBlock();
            text.EndBlock();
            string fallbackPointer = $"{spanPointer}Terminated";
            string[] fallbackArguments = [.. arguments.Select(argument => argument == spanPointer ? fallbackPointer : argument)];
            text.WriteLine("using NGXStrings strings = new();");
            text.WriteLine($"byte* {fallbackPointer} = strings.AllocUtf8({spanName});");
            text.WriteBlankLine();
            WriteInvocation(text, function.Result, method, fallbackArguments, fixedStatements);
            text.EndBlock();
            return;
        }

        if (strings.Count is not 0)
        {
            text.WriteLine("using NGXStrings strings = new();");

            foreach (string statement in strings)
            {
                text.WriteLine(statement);
            }

            text.WriteBlankLine();
        }

        WriteInvocation(text, function.Result, method, arguments, fixedStatements);
        text.EndBlock();
    }

    private static void WriteInvocation(CodeWriter text, AstType result, string method, string[] arguments, List<string> fixedStatements)
    {
        foreach (string statement in fixedStatements)
        {
            text.BeginBlock(statement);
        }

        text.WriteLine($"{(result.Kind is NativeTypeKind.Void ? string.Empty : "return ")}{method}({string.Join(", ", arguments)});");

        for (int i = 0; i < fixedStatements.Count; i++)
        {
            text.EndBlock();
        }
    }

    private string GetBorrowModifier(AstParameter parameter)
    {
        AstType type = parameter.Type;
        if (type.Kind is not (NativeTypeKind.Pointer or NativeTypeKind.LValueReference or NativeTypeKind.RValueReference) || !mapper.GetManagedType(type).EndsWith('*'))
        {
            return string.Empty;
        }

        AstType element = type.Element!;
        if (parameter.Direction is ParameterDirection.Out)
        {
            if (element.Kind is NativeTypeKind.Pointer && mapper.GetManagedType(element).EndsWith('*'))
            {
                return string.Empty;
            }

            return "out";
        }

        return mapper.IsRecord(element) && BorrowedRecords.Contains(element.Name) ? "in" : string.Empty;
    }

    private static string GetDefaultValue(AstFunction function, AstParameter parameter, string managedType, bool isManagedOverload)
    {
        string method = GetFunctionName(function.Name).Method;
        if (method.StartsWith("Init", StringComparison.Ordinal))
        {
            if (function.Name is "NVSDK_NGX_VULKAN_Init" or "NVSDK_NGX_VULKAN_Init_with_ProjectID" && parameter.Type.Cpp is "PFN_vkGetInstanceProcAddr" or "PFN_vkGetDeviceProcAddr")
            {
                return isManagedOverload ? " = 0" : " = null";
            }

            if (managedType is "NGXFeatureCommonInfo*")
            {
                return " = null";
            }

            if (managedType is "NGXVersion")
            {
                return " = NGXVersion.Api";
            }
        }

        if (method is "EvaluateDLSSG" && managedType is "NGXDLSSGOptEvalParams*")
        {
            return " = null";
        }

        if (method is "EvaluateFeature" or "EvaluateFeatureC" && parameter.Name is "InCallback")
        {
            return " = null";
        }

        return string.Empty;
    }
}
