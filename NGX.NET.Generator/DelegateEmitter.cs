namespace NGX.NET.Generator;

internal class DelegateEmitter(Models models, TypeMapper mapper, Dictionary<string, string> files)
{
    internal void WriteCallbacks()
    {
        CodeWriter guards = CreateFile();
        guards.BeginBlock("internal static partial class NgxCallbacks");

        foreach (AstCallback model in models.Callbacks.Where(static callback => !callback.Name.StartsWith("PFN_NVSDK_NGX_Parameter_", StringComparison.Ordinal)))
        {
            string name = model.Name;
            string managed = TypeName(name);
            string result = mapper.PublicType(model.Result);
            (string Type, string Modifier, string Attribute)[] parameters = [.. model.Parameters.Select(CallbackParameter)];
            string[] names = [.. model.Parameters.Select(static parameter => parameter.Name)];

            string arguments = string.Join(", ", parameters.Select((parameter, i) => $"{parameter.Attribute}{parameter.Modifier}{parameter.Type} {names[i]}"));
            CodeWriter callback = CreateFile();
            callback.Line("[UnmanagedFunctionPointer(CallingConvention.Cdecl)]");
            callback.Line($"public delegate {result} {managed}({arguments});");
            files[$"Delegates/{managed}.g.cs"] = callback.ToString();

            bool used = models.Functions.Values.SelectMany(static function => function.Parameters.Select(static parameter => parameter.Type)).Concat(models.Records.Values.SelectMany(static record => record.Fields.Select(static field => field.Type))).Any(type => mapper.CallbackName(type) == managed);
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
        files["Delegates/NgxCallbacks.g.cs"] = guards.ToString();
    }

    private (string Type, string Modifier, string Attribute) CallbackParameter(AstCallbackParameter parameter)
    {
        AstType type = parameter.Type;
        string modifier = parameter.Direction switch
        {
            ParameterDirection.Out => "out ",
            ParameterDirection.Ref => "ref ",
            _ => string.Empty
        };

        if (type.Kind is NativeTypeKind.Pointer or NativeTypeKind.LValueReference)
        {
            AstType element = type.Element!;
            NativeTypeKind kind = element.Kind;
            if (kind is NativeTypeKind.CharS or NativeTypeKind.CharU)
            {
                return ("string?", "", "[MarshalAs(UnmanagedType.LPUTF8Str)] ");
            }

            if (kind is NativeTypeKind.Bool)
            {
                return ("bool", modifier, "[MarshalAs(UnmanagedType.I1)] ");
            }

            if (kind is NativeTypeKind.Pointer)
            {
                return ("nint", modifier, "");
            }

            if (kind is NativeTypeKind.Record)
            {
                return (element.Name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter" ? mapper.ManagedRecord(element.Name) : "nint", "", "");
            }

            if (kind is NativeTypeKind.Void)
            {
                return ("nint", "", "");
            }

            return (mapper.Type(type)[..^1], modifier, "");
        }

        return (mapper.PublicType(type), "", "");
    }
}
