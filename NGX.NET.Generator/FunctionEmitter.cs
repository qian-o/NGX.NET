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
        foreach (AstFunction function in ordered)
        {
            string method = GetFunctionName(function.Name).Method;
            string parameters = string.Join(", ", function.Parameters.Select(parameter => $"{mapper.GetManagedType(parameter.Type)} {GetParameterName(parameter.Name)}"));
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
            WriteOverload(text, function);
        }

        if (group.Length is not 0)
        {
            text.EndBlock();
        }

        text.EndBlock();
        files[$"API/{(group.Length is 0 ? "Core" : group)}.g.cs"] = text.ToString();
    }

    private void WriteOverload(CodeWriter text, AstFunction function)
    {
        string[] declarations = new string[function.Parameters.Length];
        string[] arguments = new string[function.Parameters.Length];
        List<string> fixedStatements = [];
        List<string> strings = [];
        bool hasOverload = false;
        for (int i = 0; i < function.Parameters.Length; i++)
        {
            AstParameter parameter = function.Parameters[i];
            AstType type = parameter.Type;
            string native = mapper.GetManagedType(type);
            string name = GetParameterName(parameter.Name);
            string identifier = name.TrimStart('@');
            string pointer = $"p{char.ToUpperInvariant(identifier[0])}{identifier[1..]}";
            declarations[i] = $"{native} {name}";
            arguments[i] = name;

            if (type.Kind is NativeTypeKind.Bool)
            {
                declarations[i] = $"bool {name}";
                arguments[i] = $"{name} ? (byte)1 : (byte)0";
                hasOverload = true;
            }
            else if (type.Element is { Kind: NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar } element && (parameter.Name is "InName" || GetFunctionName(function.Name).Method.StartsWith("Init", StringComparison.Ordinal)))
            {
                string allocation = element.Kind is NativeTypeKind.WChar ? "AllocWide" : "AllocUtf8";
                declarations[i] = $"string {name}";
                arguments[i] = pointer;
                strings.Add($"{native} {pointer} = strings.{allocation}({name});");
                hasOverload = true;
            }
            else if (function.Name is "NVSDK_NGX_VULKAN_Init" or "NVSDK_NGX_VULKAN_Init_with_ProjectID" && type.Cpp is "PFN_vkGetInstanceProcAddr" or "PFN_vkGetDeviceProcAddr")
            {
                declarations[i] = $"nint {name}";
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

        string method = GetFunctionName(function.Name).Method;
        text.BeginBlock($"public static {mapper.GetManagedType(function.Result)} {method}({string.Join(", ", declarations)})");

        if (strings.Count is not 0)
        {
            text.WriteLine("using NGXStrings strings = new();");

            foreach (string statement in strings)
            {
                text.WriteLine(statement);
            }

            text.WriteBlankLine();
        }

        foreach (string statement in fixedStatements)
        {
            text.BeginBlock(statement);
        }

        text.WriteLine($"{(function.Result.Kind is NativeTypeKind.Void ? string.Empty : "return ")}{method}({string.Join(", ", arguments)});");

        for (int i = 0; i < fixedStatements.Count; i++)
        {
            text.EndBlock();
        }

        text.EndBlock();
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
}
