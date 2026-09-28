using System.Text;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private void EmitValueReturnOverload(StringBuilder builder, NativeDeclaration declaration)
    {
        List<NativeDeclaration> outputs = [.. declaration.Parameters.Where(parameter => parameter.Contract.ReturnValue)];

        if (outputs.Count == 0)
        {
            return;
        }

        if (outputs.Count != 1 || declaration.ResultType?.Declaration != "sl::Result")
        {
            throw new InvalidDataException("A value-return overload requires one reviewed output and an SLResult result: " + declaration.Name);
        }

        NativeDeclaration output = outputs[0];

        if (output.Contract.Convenience != "ref" || output.Type.Element?.Kind != "RECORD")
        {
            throw new InvalidDataException("Missing output-structure contract: " + output.QualifiedName);
        }

        string outputType = mapper.Map(output.Type.Element);
        List<string> parameters = [];
        List<string> arguments = [];

        foreach (NativeDeclaration parameter in declaration.Parameters)
        {
            string name = TypeMapper.Identifier(parameter.Name);

            if (parameter == output)
            {
                arguments.Add("ref " + name);
                continue;
            }

            switch (parameter.Contract.Convenience)
            {
                case "in":
                    parameters.Add("in " + mapper.Map(parameter.Type.Element!) + " " + name);
                    arguments.Add("in " + name);
                    break;
                case "frame-token":
                    parameters.Add("FrameToken " + name);
                    arguments.Add(name);
                    break;
                case "raw":
                    parameters.Add(ParameterDeclaration(parameter));
                    arguments.Add(name);
                    break;
                default:
                    throw new InvalidDataException("Review additional value-return input semantics: " + parameter.QualifiedName);
            }
        }

        string method = FunctionName(declaration);
        string outputName = TypeMapper.Identifier(output.Name);
        NativeDeclaration documentation = snapshot.Declarations.FirstOrDefault(item => item.Name == "PFun_" + declaration.Name && item.Comment.Length > 0) ?? declaration;
        builder.AppendLine();
        Comment(builder, documentation, "    ", "Returns a newly initialized output structure using its default version and an empty Next chain. Use the ref overload to supply an extension chain or a different version. Nested pointers keep their native ownership and lifetime requirements.");
        builder.AppendLine("    /// <returns>The output structure when the SDK returns SLResult.Ok.</returns>");
        builder.AppendLine("    /// <exception cref=\"SLException\">The SDK returns any result other than SLResult.Ok, including a non-success warning.</exception>");
        builder.AppendLine($"    public static {outputType} {method}({string.Join(", ", parameters)})");
        builder.AppendLine("    {");
        builder.AppendLine($"        {outputType} {outputName} = new();");
        builder.AppendLine($"        SLResult result = {method}({string.Join(", ", arguments)});");
        builder.AppendLine("        if (result != SLResult.Ok)");
        builder.AppendLine("        {");
        builder.AppendLine($"            throw new SLException(result, \"{declaration.Name}\");");
        builder.AppendLine("        }");
        builder.AppendLine($"        return {outputName};");
        builder.AppendLine("    }");
    }
}
