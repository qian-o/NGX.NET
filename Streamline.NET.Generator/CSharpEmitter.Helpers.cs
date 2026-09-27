using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private bool TryEmitHelper(NativeDeclaration declaration)
    {
        if (declaration.File == "include/sl_helpers_vk.h")
        {
            EmitVulkanHelper(declaration);
            return true;
        }

        if (declaration.Name.Contains("AsStr", StringComparison.Ordinal))
        {
            EmitStringHelper(declaration);
            return true;
        }

        if (declaration.Name.StartsWith("operator", StringComparison.Ordinal))
        {
            if (declaration.Name == "operator&")
            {
                NativeType type = declaration.Parameters.First().Type;
                string csType = mapper.Map(type);
                StringBuilder builder = File(TypeMapper.Group(declaration), "SL.Flags");
                builder.AppendLine();
                builder.AppendLine("    /// <summary>Returns whether any bit in the mask is present, preserving the native Boolean operator&amp; semantics.</summary>");
                builder.AppendLine($"    public static bool HasAnyFlags({csType} value, {csType} mask) => (value & mask) != 0;");
            }
            Record(declaration, declaration.Name == "operator&" ? "SL.HasAnyFlags" : "C# enum bitwise operator " + declaration.Name);
            return true;
        }

        if (declaration.Name is "resolveDLSSPreset" or "resolveDLSSDPreset")
        {
            StringBuilder builder = File("Helpers", "SL.Presets");
            builder.AppendLine();
            Comment(builder, declaration, "    ");
            builder.AppendLine($"    public static {mapper.Map(declaration.ResultType!)} {TypeMapper.PascalCase(declaration.Name)}({string.Join(", ", declaration.Parameters.Select(ParameterDeclaration))})");
            builder.AppendLine("    {");
            if (declaration.Name == "resolveDLSSPreset")
            {
                List<string> cases = Regex.Matches(declaration.Source, @"case\s+(\w+::\w+)\s*:").Select(match => TranslateExpression(match.Groups[1].Value)).ToList();
                Match fallback = Regex.Match(declaration.Source, @"default:\s*return\s+(\w+::\w+);");
                if (cases.Count == 0 || !fallback.Success)
                {
                    throw new InvalidDataException("Unsupported preset resolution body.");
                }
                builder.AppendLine($"        return preset is {string.Join(" or ", cases)} ? preset : {TranslateExpression(fallback.Groups[1].Value)};");
            }
            else
            {
                if (!declaration.Source.Contains("static_cast<DLSSDPreset>(resolveDLSSPreset(static_cast<DLSSPreset>(preset)))", StringComparison.Ordinal))
                {
                    throw new InvalidDataException("Unsupported DLSSD preset resolution body.");
                }
                builder.AppendLine("        return (DLSSDPreset)ResolveDLSSPreset((DLSSPreset)preset);");
            }
            builder.AppendLine("    }");
            Record(declaration, "SL." + TypeMapper.PascalCase(declaration.Name));
            return true;
        }

        if (declaration.File == "include/sl_matrix_helpers.h" || declaration.Name == "transpose")
        {
            EmitMathHelper(declaration);
            return true;
        }

        return false;
    }

    private void EmitStringHelper(NativeDeclaration declaration)
    {
        StringBuilder builder = File(TypeMapper.Group(declaration), "SL.Strings");
        List<(string Value, string Text)> cases = [];
        foreach (Match match in Regex.Matches(declaration.Source, @"SL_CASE_STR\(([^)]+)\)"))
        {
            cases.Add((TranslateSymbol(match.Groups[1].Value), match.Groups[1].Value));
        }
        foreach (Match match in Regex.Matches(declaration.Source, "case\\s+([^:]+):\\s*return\\s+\"([^\"]*)\";"))
        {
            cases.Add((TranslateSymbol(match.Groups[1].Value.Trim()), match.Groups[2].Value));
        }
        MatchCollection returns = Regex.Matches(declaration.Source, "return\\s+\"([^\"]*)\";");
        if (cases.Count == 0 || returns.Count == 0)
        {
            throw new InvalidDataException($"Unsupported string helper: {declaration.Name}");
        }
        string fallback = returns[^1].Groups[1].Value;
        builder.AppendLine();
        Comment(builder, declaration, "    ");
        builder.AppendLine($"    public static string {TypeMapper.PascalCase(declaration.Name)}({string.Join(", ", declaration.Parameters.Select(ParameterDeclaration))})");
        builder.AppendLine("    {");
        builder.AppendLine($"        return {declaration.Parameters.Single().Name} switch");
        builder.AppendLine("        {");
        foreach ((string value, string text) in cases)
        {
            builder.AppendLine($"            {value} => \"{text}\",");
        }
        builder.AppendLine($"            _ => \"{fallback}\"");
        builder.AppendLine("        };");
        builder.AppendLine("    }");
        Record(declaration, "SL." + TypeMapper.PascalCase(declaration.Name));
    }

    private string TranslateSymbol(string symbol)
    {
        symbol = symbol.Replace("sl::", "", StringComparison.Ordinal);
        return symbol.StartsWith('k') ? "SL." + TypeMapper.ConstantName(symbol) : TranslateExpression(symbol);
    }

    private void EmitMathHelper(NativeDeclaration declaration)
    {
        StringBuilder builder = File("Helpers", "SL.Math");
        string body = declaration.Source[(declaration.Source.IndexOf('{') + 1)..declaration.Source.LastIndexOf('}')];
        body = Regex.Replace(body, @"\bstatic float4x4 (\w+) = \{(.*?)\};", match =>
        {
            string initializer = TranslateMathBody(match.Groups[2].Value, []);
            StringBuilder state = File("Helpers", "SL.MathState");
            state.AppendLine();
            state.AppendLine("    // Shared history retained from the upstream helper. Not thread-safe or per-viewport.");
            state.AppendLine($"    private static Float4x4 {match.Groups[1].Value} = new({initializer.Trim().TrimEnd(',')});");
            return "";
        }, RegexOptions.Singleline);
        body = TranslateMathBody(body, declaration.Parameters.ToList());
        builder.AppendLine();
        Comment(builder, declaration, "    ", declaration.Name == "recalculateCameraMatrices"
            ? "Maintains shared previous-camera state. Not thread-safe and not isolated by viewport; preserves the upstream helper's limitations."
            : "Preserves the upstream formula, precision and defined boundary behavior.");
        builder.AppendLine($"    public static {mapper.Map(declaration.ResultType!)} {TypeMapper.PascalCase(declaration.Name)}({string.Join(", ", declaration.Parameters.Select(ParameterDeclaration))})");
        builder.AppendLine("    {");
        foreach (string line in body.Trim('\n', '\r').Split('\n'))
        {
            builder.AppendLine(string.IsNullOrWhiteSpace(line) ? "" : "    " + line.TrimEnd());
        }
        builder.AppendLine("    }");
        EmitMathReferenceOverload(builder, declaration);
        Record(declaration, "SL." + TypeMapper.PascalCase(declaration.Name));
    }

    private static string TranslateMathBody(string body, List<NativeDeclaration> parameters)
    {
        body = Regex.Replace(body, @"\bconst\s+", "");
        body = Regex.Replace(body, @"\b(\d+)\.f\b", "$1.0f");
        body = body.Replace("sqrtf(", "MathF.Sqrt(", StringComparison.Ordinal);
        body = Regex.Replace(body, @"\bfloat([234])x4\b", "Float$1x4");
        body = Regex.Replace(body, @"\bfloat([234])\s*\(", "new Float$1(");
        body = Regex.Replace(body, @"\bfloat([234])\b", "Float$1");
        body = Regex.Replace(body, @"\bFloat4x4\s+(\w+)\s*;", "Float4x4 $1 = new();");
        body = Regex.Replace(body, @"(Float4x4\s+\w+\s*=)\s*\{(.*?)\};", "$1 new($2);", RegexOptions.Singleline);
        body = Regex.Replace(body, @"(\w+\[\d+\]\s*=)\s*\{([^}]+)\};", "$1 new($2);");
        foreach (string function in new[] { "matrixMul", "matrixFullInvert", "matrixOrthoNormalInvert", "vectorNormalize", "vectorCrossProduct", "calcCameraToPrevCamera" })
        {
            body = Regex.Replace(body, @"\b" + function + @"\(([^;()]*)\)", match => TypeMapper.PascalCase(function) + "(" + string.Join(", ", match.Groups[1].Value.Split(',').Select((argument, index) => (index == 0 ? "ref " : "in ") + argument.Trim())) + ")");
        }
        body = Regex.Replace(body, @"\.(\w+)", match => "." + TypeMapper.MemberName(match.Groups[1].Value));
        foreach (NativeDeclaration parameter in parameters.Where(parameter => parameter.Type.Kind == "LVALUEREFERENCE"))
        {
            body = Regex.Replace(body, @"\b" + Regex.Escape(parameter.Name) + @"\b", "(*" + parameter.Name + ")");
        }
        body = Regex.Replace(body, @"float\* (\w+) = &([^;]+);", "float* $1 = (float*)Unsafe.AsPointer(ref $2);");
        return body;
    }

    private void EmitMathReferenceOverload(StringBuilder builder, NativeDeclaration declaration)
    {
        builder.AppendLine();
        Comment(builder, declaration, "    ", declaration.Name == "recalculateCameraMatrices"
            ? "References are fixed for this call only. Maintains shared previous-camera state; not thread-safe or isolated by viewport."
            : "References are fixed for the duration of the corresponding pointer helper.");
        string parameters = string.Join(", ", declaration.Parameters.Select(parameter => (parameter.Type.Element!.Const ? "in " : "ref ") + mapper.Map(parameter.Type.Element) + " " + parameter.Name));
        builder.AppendLine($"    public static {mapper.Map(declaration.ResultType!)} {TypeMapper.PascalCase(declaration.Name)}({parameters})");
        builder.AppendLine("    {");
        foreach (NativeDeclaration parameter in declaration.Parameters)
        {
            builder.AppendLine($"        fixed ({mapper.Map(parameter.Type)} {parameter.Name}Pointer = &{parameter.Name})");
        }
        builder.AppendLine("        {");
        builder.AppendLine("            " + (declaration.ResultType!.Kind == "VOID" ? "" : "return ") + TypeMapper.PascalCase(declaration.Name) + "(" + string.Join(", ", declaration.Parameters.Select(parameter => parameter.Name + "Pointer")) + ");");
        builder.AppendLine("        }");
        builder.AppendLine("    }");
    }
}
