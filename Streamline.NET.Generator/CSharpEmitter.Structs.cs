using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private static string FieldName(NativeDeclaration record, NativeDeclaration field)
    {
        string name = field.Access == "PRIVATE" ? field.Name : TypeMapper.MemberName(field.Name);

        return record.Children.Any(child => child.Kind == "ENUM_DECL" && child.Name == name) ? name + "Value" : name;
    }

    private void EmitStruct(NativeDeclaration declaration)
    {
        if (declaration.Type.Size <= 0 || declaration.Type.Alignment <= 0)
        {
            throw new InvalidDataException($"Missing layout: {declaration.QualifiedName}");
        }

        string name = TypeMapper.TypeName(declaration.Name);
        StringBuilder builder = File(TypeMapper.Group(declaration), name, "System.Runtime.InteropServices");
        bool hasHeader = declaration.Children.Any(child => child.Kind == "CXX_BASE_SPECIFIER" && child.Type.Declaration == "sl::BaseStructure");
        NativeDeclaration? union = declaration.Children.FirstOrDefault(child => child.Kind == "UNION_DECL");
        List<NativeDeclaration> fields = [.. union?.Fields ?? declaration.Fields];
        string layout = union is null ? "Sequential" : "Explicit";

        if (fields.Any(field => field.Type.Kind == "CONSTANTARRAY"))
        {
            File(TypeMapper.Group(declaration), name, "System.Runtime.CompilerServices");
        }

        builder.AppendLine();
        Comment(builder, declaration);
        builder.AppendLine($"[StructLayout(LayoutKind.{layout}, Pack = {declaration.Type.Alignment}, Size = {declaration.Type.Size})]");
        builder.AppendLine($"public unsafe partial struct {name}" + (hasHeader ? " : ISLStructure" : ""));
        builder.AppendLine("{");

        if (hasHeader)
        {
            NativeDeclaration baseStructure = snapshot.Declarations.Single(item => item.QualifiedName == "sl::BaseStructure");

            foreach (NativeDeclaration field in baseStructure.Fields)
            {
                Comment(builder, field, "    ");
                builder.AppendLine($"    public {mapper.Map(field.Type)} {TypeMapper.MemberName(field.Name)};");
                builder.AppendLine();
            }

            EmitTypeId(builder, declaration);
            Record(declaration.Children.Single(child => child.Kind == "CXX_BASE_SPECIFIER"), "Flattened BaseStructure prefix");
        }

        foreach (NativeDeclaration field in fields)
        {
            if (field.BitWidth is not null)
            {
                throw new InvalidDataException($"Unhandled bitfield: {field.QualifiedName}");
            }

            Comment(builder, field, "    ", field.Type.Element?.Kind == "FUNCTIONPROTO"
                ? "Keep the callback and any referenced state alive for the native contract's full duration. Managed exceptions must not cross the callback boundary."
                : null);

            if (union is not null)
            {
                builder.AppendLine($"    [FieldOffset({field.OffsetBits / 8})]");
            }

            string fieldName = FieldName(declaration, field);
            string access = field.Access == "PRIVATE" ? "private" : "public";

            if (field.Type.Kind == "CONSTANTARRAY")
            {
                string element = mapper.Map(field.Type.Element ?? throw new InvalidDataException("Missing array element."));
                string buffer = TypeMapper.PascalCase(fieldName) + "Buffer";
                builder.AppendLine($"    {access} {buffer} {fieldName};");
                builder.AppendLine();
                builder.AppendLine($"    /// <summary>Inline storage for {field.Type.Count} native {element} elements.</summary>");
                builder.AppendLine($"    [InlineArray({field.Type.Count})]");
                builder.AppendLine($"    public struct {buffer}");
                builder.AppendLine("    {");
                builder.AppendLine($"        private {element} element;");
                builder.AppendLine("    }");
            }
            else
            {
                builder.AppendLine($"    {access} {mapper.Map(field.Type)} {fieldName};");
            }

            builder.AppendLine();
            Record(field, name + "." + fieldName);
        }

        foreach (NativeDeclaration nested in declaration.Children.Where(child => child.Kind == "ENUM_DECL"))
        {
            EmitEnum(nested, builder, "    ");
        }

        if (union is not null)
        {
            Record(union, "Explicit union fields in " + name);
        }
        else
        {
            EmitConstructors(builder, declaration, fields, hasHeader);
        }

        builder.AppendLine("}");
        Record(declaration, name);
    }

    private void EmitTypeId(StringBuilder builder, NativeDeclaration declaration)
    {
        Match match = Regex.Match(declaration.Source, @"SL_STRUCT_(?:PROTECTED_)?BEGIN\([^,]+,\s*StructType\(\s*\{(.*?)\}\s*\),\s*(\w+)\)", RegexOptions.Singleline);

        if (!match.Success)
        {
            throw new InvalidDataException($"Missing structure identity: {declaration.QualifiedName}");
        }

        string[] values = Regex.Matches(match.Groups[1].Value, @"0x[0-9a-fA-F]+|\d+").Select(value => value.Value).ToArray();

        if (values.Length != 11)
        {
            throw new InvalidDataException($"Invalid structure identity: {declaration.QualifiedName}");
        }

        builder.AppendLine("    /// <summary>Native structure type identifier.</summary>");
        builder.AppendLine($"    public static StructType TypeId => new({values[0]}, {values[1]}, {values[2]}, [{string.Join(", ", values.Skip(3))}]);");
        builder.AppendLine();
        Record(declaration.Children.Single(child => child.Name == "s_structType"), TypeMapper.TypeName(declaration.Name) + ".TypeId");
    }

    private void EmitConstructors(StringBuilder builder, NativeDeclaration declaration, List<NativeDeclaration> fields, bool hasHeader)
    {
        string name = TypeMapper.TypeName(declaration.Name);
        List<NativeDeclaration> constructors = [.. declaration.Children.Where(child => child.Kind == "CONSTRUCTOR")];
        NativeDeclaration? nativeDefault = constructors.FirstOrDefault(child => !child.Parameters.Any());
        bool deletedDefault = nativeDefault?.Source.Contains("= delete", StringComparison.Ordinal) == true;

        if (!deletedDefault)
        {
            builder.AppendLine("    /// <summary>Initializes native defaults, including nested structures. Unspecified native values remain CLR-zeroed.</summary>");
            builder.AppendLine($"    public {name}()");
            builder.AppendLine("    {");
            builder.AppendLine("        this = default;");
            builder.AppendLine();

            if (hasHeader)
            {
                Match version = Regex.Match(declaration.Source, @"\}\s*\),\s*(kStructVersion\d+)\)");
                builder.AppendLine("        StructType = TypeId;");
                builder.AppendLine($"        StructVersion = SL.{version.Groups[1].Value[1..]};");
            }

            foreach (NativeDeclaration field in fields)
            {
                string fieldName = FieldName(declaration, field);

                if (field.Type.Kind == "CONSTANTARRAY")
                {
                    if (field.Type.Element?.Kind == "RECORD")
                    {
                        builder.AppendLine($"        for (int index = 0; index < {field.Type.Count}; index++)");
                        builder.AppendLine("        {");
                        builder.AppendLine($"            {fieldName}[index] = new();");
                        builder.AppendLine("        }");
                    }
                }
                else if (field.Type.Kind == "RECORD")
                {
                    builder.AppendLine($"        {fieldName} = new();");
                }
                else if (field.Expressions.Count > 0)
                {
                    NativeExpression expression = field.Expressions[^1];

                    if (expression.Text != "{}")
                    {
                        string value = expression.Value is not null ? ConstantValue(expression, field.Type) : TranslateExpression(expression.Text);

                        if (expression.Value == "0" || expression.Value == "0.0")
                        {
                            continue;
                        }

                        builder.AppendLine($"        {fieldName} = {value};");
                    }
                }
            }

            if (nativeDefault is not null && !nativeDefault.Source.StartsWith("SL_STRUCT", StringComparison.Ordinal))
            {
                EmitInitializers(builder, declaration, nativeDefault);
            }

            builder.AppendLine("    }");
            builder.AppendLine();
        }

        if (nativeDefault is not null)
        {
            Record(nativeDefault, deletedDefault ? "No default construction convenience; CLR default remains possible" : name + "()");
        }

        foreach (NativeDeclaration constructor in constructors.Where(item => item.Parameters.Any()))
        {
            if (constructor.Source.Contains("= delete", StringComparison.Ordinal))
            {
                Record(constructor, "No copying convenience; native ownership restriction documented");
                continue;
            }

            Comment(builder, constructor, "    ");
            builder.AppendLine($"    public {name}({string.Join(", ", constructor.Parameters.Select(ParameterDeclaration))}) : this()");
            builder.AppendLine("    {");
            EmitInitializers(builder, declaration, constructor);
            string body = constructor.Source[(constructor.Source.IndexOf('{') + 1)..].Trim().TrimEnd('}', ';').Trim();

            if (body.Length > 0)
            {
                if (declaration.Name == "ResourceTag" && body == "if (e) extent = *e;")
                {
                    builder.AppendLine("        if (e != null)");
                    builder.AppendLine("        {");
                    builder.AppendLine("            Extent = *e;");
                    builder.AppendLine("        }");
                }
                else
                {
                    throw new InvalidDataException($"Unhandled constructor body: {constructor.Source}");
                }
            }

            builder.AppendLine("    }");
            builder.AppendLine();
            Record(constructor, name + " constructor");
        }
    }

    private string ParameterDeclaration(NativeDeclaration parameter)
    {
        string result = mapper.Map(parameter.Type) + " " + TypeMapper.Identifier(parameter.Name.TrimStart('_'));

        if (parameter.Source.Contains('=') && parameter.Expressions.Count > 0)
        {
            NativeExpression expression = parameter.Expressions[^1];
            result += " = " + (expression.Text == "nullptr" ? "null" : ConstantValue(expression, parameter.Type));
        }

        return result;
    }

    private void EmitInitializers(StringBuilder builder, NativeDeclaration declaration, NativeDeclaration constructor)
    {
        Match initializers = Regex.Match(constructor.Source, @"\)\s*:\s*(.*?)\{", RegexOptions.Singleline);

        if (!initializers.Success)
        {
            return;
        }

        Dictionary<string, string> parameters = constructor.Parameters.ToDictionary(parameter => parameter.Name, parameter => TypeMapper.Identifier(parameter.Name.TrimStart('_')), StringComparer.Ordinal);

        foreach (Match initializer in Regex.Matches(initializers.Groups[1].Value, @"(\w+)\(((?:[^()]|\([^()]*\))*)\)"))
        {
            if (initializer.Groups[1].Value == "BaseStructure")
            {
                continue;
            }

            NativeDeclaration field = declaration.Fields.Single(item => item.Name == initializer.Groups[1].Value);
            string expression = TranslateExpression(initializer.Groups[2].Value, parameters);
            // C++ implicit signed-to-unsigned constructor conversion is unchecked.
            if (field.Type.Kind == "UINT" && constructor.Parameters.Any(parameter => parameter.Name == initializer.Groups[2].Value && parameter.Type.Kind == "INT"))
            {
                expression = "unchecked((uint)" + expression + ")";
            }

            builder.AppendLine($"        {FieldName(declaration, field)} = {expression};");
        }
    }

    private string TranslateExpression(string expression, Dictionary<string, string>? parameters = null)
    {
        expression = expression.Replace("sl::", "", StringComparison.Ordinal);
        expression = Regex.Replace(expression, @"(\w+)::(e\w+)", match => TypeMapper.TypeName(match.Groups[1].Value) + "." + TypeMapper.EnumMember(match.Groups[2].Value));
        expression = Regex.Replace(expression, @"\b(?:INVALID_FLOAT|INVALID_UINT|MAX_FRAMES_IN_FLIGHT)\b", match => "SL." + TypeMapper.MemberName(match.Value));
        expression = expression.Replace("UINT_MAX", "uint.MaxValue", StringComparison.Ordinal).Replace("nullptr", "null", StringComparison.Ordinal);
        expression = Regex.Replace(expression, @"\bto_underlying\((\w+)\)", "(uint)$1");

        if (parameters is not null)
        {
            expression = Regex.Replace(expression, @"\b\w+\b", match => parameters.GetValueOrDefault(match.Value, match.Value));
        }

        return expression;
    }
}
