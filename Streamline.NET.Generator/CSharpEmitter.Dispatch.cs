using System.Text;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private string EmitFeatureFunctionResolver(NativeDeclaration declaration, string feature, string pointerSignature)
    {
        string name = "Resolve" + declaration.Name[2..];
        StringBuilder builder = File("Interop", "FeatureFunctions");
        builder.AppendLine();
        builder.AppendLine($"    private nint {declaration.Name};");
        builder.AppendLine();
        builder.AppendLine($"    internal SLResult {name}(out {pointerSignature} function)");
        builder.AppendLine("    {");
        builder.AppendLine($"        SLResult result = Resolve(ref {declaration.Name}, SL.{TypeMapper.ConstantName(feature)}, \"{declaration.Name}\"u8, out nint address);");
        builder.AppendLine($"        function = ({pointerSignature})address;");
        builder.AppendLine("        return result;");
        builder.AppendLine("    }");

        return name;
    }
}
