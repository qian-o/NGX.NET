using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private void EmitFunction(NativeDeclaration declaration)
    {
        string name = declaration.Name[2..];
        string result = mapper.Map(declaration.ResultType ?? throw new InvalidDataException("Missing function result."));
        string pointerSignature = mapper.Map(declaration.Type);
        bool exported = snapshot.Exports.Contains(declaration.Name, StringComparer.Ordinal);
        List<NativeDeclaration> parameters = [.. declaration.Parameters];
        string arguments = string.Join(", ", parameters.Select(parameter => TypeMapper.Identifier(parameter.Name)));
        string signature = string.Join(", ", parameters.Select(ParameterDeclaration));
        StringBuilder builder = File(TypeMapper.Group(declaration), "SL.Functions");
        NativeDeclaration? alias = snapshot.Declarations.FirstOrDefault(item => item.Name == "PFun_" + declaration.Name);
        NativeDeclaration documented = alias is not null && declaration.Comment.Length == 0 ? alias : declaration;
        builder.AppendLine();
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
            imports.AppendLine($"    [LibraryImport(StreamlineLibrary.ImportName, EntryPoint = \"{declaration.Name}\")]");
            imports.AppendLine("    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]");
            imports.AppendLine($"    internal static partial {result} {name}({string.Join(", ", parameters.Select(parameter => mapper.Map(parameter.Type) + " " + TypeMapper.Identifier(parameter.Name)))});");
        }
        else
        {
            Match feature = Regex.Match(declaration.Source, @"SL_FEATURE_FUN_IMPORT_STATIC\(sl::(k\w+),\s*(\w+)\)");
            if (!feature.Success || feature.Groups[2].Value != declaration.Name || result != "SLResult")
            {
                throw new InvalidDataException($"Unresolved feature entry: {declaration.Name}");
            }

            builder.AppendLine($"        SLResult result = FeatureFunctions.Get({TypeMapper.ConstantName(feature.Groups[1].Value)}, \"{declaration.Name}\", \"{declaration.Name}\"u8, out nint address);");
            builder.AppendLine("        if (result != SLResult.Ok)");
            builder.AppendLine("        {");
            builder.AppendLine("            return result;");
            builder.AppendLine("        }");
            builder.AppendLine($"        return (({pointerSignature})address)({arguments});");
        }

        builder.AppendLine("    }");
        EmitReferenceOverload(builder, declaration);
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
        if (declaration.Parameters.Any(parameter => parameter.Contract.Convenience == "variant-query-or-fill"))
        {
            EmitVariantOverloads(builder, declaration);
            return;
        }

        List<string> signatures = [];
        List<string> arguments = [];
        List<string> pins = [];
        List<string> before = [];
        List<string> after = [];
        Dictionary<string, string> counts = declaration.Parameters
            .Where(parameter => parameter.Contract.CountParameter is not null)
            .ToDictionary(parameter => parameter.Contract.CountParameter!, parameter => parameter.Name, StringComparer.Ordinal);
        bool changed = false;

        foreach (NativeDeclaration parameter in declaration.Parameters)
        {
            string name = TypeMapper.Identifier(parameter.Name);
            NativeType? element = parameter.Type.Element;
            string convenience = parameter.Contract.Convenience;
            if (counts.TryGetValue(parameter.Name, out string? spanName))
            {
                arguments.Add("(uint)" + spanName + ".Length");
                continue;
            }
            if (convenience == "raw")
            {
                signatures.Add(ParameterDeclaration(parameter));
                arguments.Add(name);
                continue;
            }

            changed = true;
            switch (convenience)
            {
                case "frame-token":
                    signatures.Add("FrameToken " + name);
                    arguments.Add(name + ".Handle");
                    break;
                case "out-frame-token":
                    signatures.Add("out FrameToken " + name);
                    before.Add("nint " + parameter.Name + "Address = 0;");
                    arguments.Add("&" + parameter.Name + "Address");
                    after.Add(name + " = new(" + parameter.Name + "Address);");
                    break;
                case "in":
                case "ref":
                case "out":
                    string valueType = mapper.Map(element!);
                    signatures.Add(convenience + " " + valueType + " " + name);
                    if (convenience == "out")
                    {
                        before.Add(name + " = default;");
                    }
                    pins.Add($"fixed ({valueType}* {parameter.Name}Pointer = &{name})");
                    arguments.Add(parameter.Name + "Pointer");
                    break;
                case "out-address":
                case "ref-address":
                    signatures.Add((convenience == "out-address" ? "out " : "ref ") + "nint " + name);
                    if (convenience == "out-address")
                    {
                        before.Add(name + " = 0;");
                    }
                    pins.Add($"fixed (nint* {parameter.Name}Pointer = &{name})");
                    arguments.Add("(" + mapper.Map(parameter.Type) + ")" + parameter.Name + "Pointer");
                    break;
                case "utf8-string":
                    signatures.Add("string " + name);
                    before.Add($"using Utf8StringArray {parameter.Name}Utf8 = new([{name}]);");
                    arguments.Add(parameter.Name + "Utf8.Pointer[0]");
                    break;
                case "readonly-span":
                    string elementType = mapper.Map(element!);
                    signatures.Add("ReadOnlySpan<" + elementType + "> " + name);
                    pins.Add($"fixed ({elementType}* {parameter.Name}Pointer = {name})");
                    arguments.Add(parameter.Name + "Pointer");
                    break;
                default:
                    throw new InvalidDataException("Unsupported reviewed overload contract: " + convenience);
            }
        }

        if (!changed)
        {
            return;
        }

        builder.AppendLine();
        NativeDeclaration documentation = declaration.Comment.Length == 0
            ? snapshot.Declarations.FirstOrDefault(item => item.Name == "PFun_" + declaration.Name) ?? declaration
            : declaration;
        Comment(builder, documentation, "    ", "Temporary strings, references and spans remain fixed for this call only. Nested pointers and SDK objects retain their original ownership and lifetime requirements.");
        builder.AppendLine($"    public static SLResult {declaration.Name[2..]}({string.Join(", ", signatures)})");
        builder.AppendLine("    {");
        foreach (string statement in before)
        {
            builder.AppendLine("        " + statement);
        }
        foreach (string pin in pins)
        {
            builder.AppendLine("        " + pin);
        }
        if (pins.Count > 0)
        {
            builder.AppendLine("        {");
        }
        string indent = pins.Count > 0 ? "            " : "        ";
        string call = $"{declaration.Name[2..]}({string.Join(", ", arguments)})";
        if (after.Count == 0)
        {
            builder.AppendLine(indent + "return " + call + ";");
        }
        else
        {
            builder.AppendLine(indent + "SLResult result = " + call + ";");
            foreach (string statement in after)
            {
                builder.AppendLine(indent + statement);
            }
            builder.AppendLine(indent + "return result;");
        }
        if (pins.Count > 0)
        {
            builder.AppendLine("        }");
        }
        builder.AppendLine("    }");
    }

    private void EmitVariantOverloads(StringBuilder builder, NativeDeclaration declaration)
    {
        string type = mapper.Map(declaration.Parameters.Last().Type.Element!);
        string name = declaration.Name[2..];
        builder.AppendLine();
        builder.AppendLine("    /// <summary>Queries the available variant count using the original null-buffer form.</summary>");
        builder.AppendLine($"    public static SLResult {name}(out uint numVariants)");
        builder.AppendLine("    {");
        builder.AppendLine("        numVariants = 0;");
        builder.AppendLine("        fixed (uint* count = &numVariants)");
        builder.AppendLine("        {");
        builder.AppendLine($"            return {name}(count, null);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
        builder.AppendLine();
        builder.AppendLine("    /// <summary>Requests exactly the span length of variants. Initialize each versioned element before the call. This does not query, resize, retry or truncate the request.</summary>");
        builder.AppendLine($"    public static SLResult {name}(Span<{type}> variantInfo)");
        builder.AppendLine("    {");
        builder.AppendLine("        uint requested = (uint)variantInfo.Length;");
        builder.AppendLine($"        fixed ({type}* variants = variantInfo)");
        builder.AppendLine("        {");
        builder.AppendLine($"            return {name}(&requested, variants);");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }
}
