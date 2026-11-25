namespace NGX.NET.Generator;

internal class CallbackEmitter(TypeMapper mapper, Dictionary<string, string> files)
{
    internal void WriteCallbacks(Dictionary<string, AstType> aliases)
    {
        CodeWriter text = CreateFile();
        foreach ((string name, AstType type) in aliases.OrderBy(static alias => alias.Key, StringComparer.Ordinal))
        {
            if (type is not { Kind: NativeTypeKind.Pointer, Element.Kind: NativeTypeKind.FunctionProto or NativeTypeKind.FunctionNoProto })
            {
                continue;
            }

            AstType function = type.Element;
            string parameters = string.Join(", ", function.Arguments.Select((argument, index) => $"{mapper.GetManagedType(argument)} arg{index}"));
            string convention = GetCallingConvention(function) is "Stdcall" ? "StdCall" : "Cdecl";
            text.WriteLine($"[UnmanagedFunctionPointer(CallingConvention.{convention})]");
            text.WriteLine($"public unsafe delegate {mapper.GetManagedType(function.Result!)} {GetTypeName(name)}({parameters});");
            text.WriteBlankLine();
        }

        files.Add("API/Callbacks.g.cs", text.ToString());
    }
}
