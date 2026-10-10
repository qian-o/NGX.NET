namespace NGX.NET.Generator;

internal class DelegateEmitter(Models models, TypeMapper mapper, Dictionary<string, string> files)
{
    internal void WriteCallbacks()
    {
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

        }
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
