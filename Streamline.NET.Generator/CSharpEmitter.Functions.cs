using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private void EmitFunction(NativeDeclaration declaration)
    {
        string name = declaration.Name[2..];
        string result = mapper.Map(declaration.ResultType ?? throw new InvalidDataException("Missing function result."));
        bool exported = snapshot.Exports.Contains(declaration.Name, StringComparer.Ordinal);
        List<NativeDeclaration> parameters = [.. declaration.Parameters];
        string arguments = string.Join(", ", parameters.Select(parameter => TypeMapper.Identifier(parameter.Name)));
        string signature = string.Join(", ", parameters.Select(ParameterDeclaration));
        StringBuilder builder = File(TypeMapper.Group(declaration), "SL.Functions");
        NativeDeclaration? alias = snapshot.Declarations.FirstOrDefault(item => item.Name == "PFun_" + declaration.Name);
        NativeDeclaration documented = alias is not null && declaration.Comment.Length == 0 ? alias : declaration;
        builder.AppendLine();
        builder.AppendLine("public static unsafe partial class SL");
        builder.AppendLine("{");
        Comment(builder, documented, "    ");
        builder.AppendLine($"    public static {result} {name}({signature})");
        builder.AppendLine("    {");

        if (exported)
        {
            if (name is "Shutdown" or "SetFeatureLoaded")
            {
                builder.AppendLine($"        SLResult result = SLNative.{name}({arguments});");
                builder.AppendLine("        if (result == SLResult.Ok)");
                builder.AppendLine("        {");
                builder.AppendLine("            FeatureFunctions.Invalidate();");
                builder.AppendLine("        }");
                builder.AppendLine("        return result;");
            }
            else
            {
                builder.AppendLine($"        return SLNative.{name}({arguments});");
            }

            StringBuilder imports = File("Interop", "SLNative");
            imports.AppendLine();
            imports.AppendLine("internal static unsafe partial class SLNative");
            imports.AppendLine("{");
            imports.AppendLine($"    [LibraryImport(StreamlineLibrary.ImportName, EntryPoint = \"{declaration.Name}\")]");
            imports.AppendLine("    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            imports.AppendLine($"    internal static partial {result} {name}({string.Join(", ", parameters.Select(parameter => mapper.Map(parameter.Type) + " " + TypeMapper.Identifier(parameter.Name)))});");
            imports.AppendLine("}");
        }
        else
        {
            Match feature = Regex.Match(declaration.Source, @"SL_FEATURE_FUN_IMPORT_STATIC\(sl::(k\w+),\s*(\w+)\)");
            if (!feature.Success || feature.Groups[2].Value != declaration.Name || result != "SLResult")
            {
                throw new InvalidDataException($"Unresolved feature entry: {declaration.Name}");
            }

            builder.AppendLine($"        SLResult result = FeatureFunctions.Get({feature.Groups[1].Value[1..]}, \"{declaration.Name}\"u8, out nint address);");
            builder.AppendLine("        if (result != SLResult.Ok)");
            builder.AppendLine("        {");
            builder.AppendLine("            return result;");
            builder.AppendLine("        }");
            builder.AppendLine($"        return (({mapper.Map(declaration.Type)})address)({arguments});");
        }

        builder.AppendLine("    }");
        EmitReferenceOverload(builder, declaration);
        builder.AppendLine("}");
        Record(declaration, "SL." + name);
        foreach (NativeDeclaration parameter in parameters)
        {
            Record(parameter, "SL." + name + " parameter " + parameter.Name);
        }
        if (alias is not null)
        {
            Record(alias, "Unmanaged function pointer signature for SL." + name);
            foreach (NativeDeclaration parameter in alias.Parameters)
            {
                Record(parameter, "Function pointer parameter " + parameter.Name);
            }
        }
    }

    private void EmitReferenceOverload(StringBuilder builder, NativeDeclaration declaration)
    {
        List<string> signatures = [];
        List<string> arguments = [];
        List<string> pins = [];
        List<string> initializers = [];
        bool changed = false;

        foreach (NativeDeclaration parameter in declaration.Parameters)
        {
            string name = TypeMapper.Identifier(parameter.Name);
            NativeType type = parameter.Type;
            NativeType? element = type.Element;
            if (type.Kind == "LVALUEREFERENCE" && element?.Declaration == "sl::FrameToken")
            {
                signatures.Add("FrameToken " + name);
                arguments.Add(name + ".Handle");
                changed = true;
            }
            else if (type.Kind == "LVALUEREFERENCE" && element?.Kind == "RECORD")
            {
                string csType = mapper.Map(element);
                signatures.Add((element.Const ? "in " : "ref ") + csType + " " + name);
                pins.Add($"fixed ({csType}* {parameter.Name}Pointer = &{name})");
                arguments.Add(parameter.Name + "Pointer");
                changed = true;
            }
            else if (type.Kind == "LVALUEREFERENCE" && element is not null && element.Kind != "RECORD")
            {
                string csType = mapper.Map(element);
                bool output = declaration.Name is "slIsFeatureLoaded" or "slGetFeatureFunction" or "slGetNewFrameToken";
                signatures.Add((output ? "out " : "ref ") + csType + " " + name);
                if (output)
                {
                    initializers.Add(name + " = default;");
                }
                pins.Add($"fixed ({csType}* {parameter.Name}Pointer = &{name})");
                arguments.Add(parameter.Name + "Pointer");
                changed = true;
            }
            else
            {
                signatures.Add(ParameterDeclaration(parameter));
                arguments.Add(name);
            }
        }

        if (!changed)
        {
            return;
        }

        builder.AppendLine();
        builder.AppendLine("    /// <summary>Convenience overload. References are fixed only until the native call returns; nested pointers retain their original lifetime requirements.</summary>");
        if (declaration.Deprecated)
        {
            builder.AppendLine("    [Obsolete(\"Deprecated by Streamline; see the source documentation.\")]");
        }
        builder.AppendLine($"    public static SLResult {declaration.Name[2..]}({string.Join(", ", signatures)})");
        builder.AppendLine("    {");
        foreach (string initializer in initializers)
        {
            builder.AppendLine("        " + initializer);
        }
        foreach (string pin in pins)
        {
            builder.AppendLine("        " + pin);
        }
        if (pins.Count > 0)
        {
            builder.AppendLine("        {");
        }
        builder.AppendLine((pins.Count > 0 ? "            " : "        ") + $"return {declaration.Name[2..]}({string.Join(", ", arguments)});");
        if (pins.Count > 0)
        {
            builder.AppendLine("        }");
        }
        builder.AppendLine("    }");
    }
}
