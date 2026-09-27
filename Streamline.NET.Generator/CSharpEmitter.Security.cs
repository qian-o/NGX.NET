using System.Text;
using System.Text.RegularExpressions;

namespace Streamline.NET.Generator;

internal sealed partial class CSharpEmitter
{
    private static string SystemName(string name)
    {
        return string.Concat(name.Trim('_').Split('_').Select(part => TypeMapper.PascalCase(part.ToLowerInvariant())));
    }

    private void EmitSecurityData()
    {
        StringBuilder builder = File("Security", "SecurityNative.Data");
        builder.AppendLine();
        builder.AppendLine("internal static unsafe partial class SecurityNative");
        builder.AppendLine("{");
        Dictionary<string, NativeDeclaration> types = snapshot.Security.Types.Where(type => !type.Name.StartsWith('(')).ToDictionary(type => type.QualifiedName, StringComparer.Ordinal);

        string MapSystem(NativeType type)
        {
            if (type.Kind == "POINTER")
            {
                return type.Element?.Kind is "FUNCTIONPROTO" or "FUNCTIONNOPROTO" ? "nint" : MapSystem(type.Element!) + "*";
            }
            if (type.Kind == "RECORD")
            {
                return types.TryGetValue(type.Declaration!, out NativeDeclaration? record) ? SystemName(record.Name) : "void";
            }
            return mapper.Map(type);
        }

        foreach (NativeDeclaration type in types.Values)
        {
            builder.AppendLine($"    [StructLayout(LayoutKind.Explicit, Size = {type.Type.Size})]");
            builder.AppendLine($"    internal struct {SystemName(type.Name)}");
            builder.AppendLine("    {");
            foreach (NativeDeclaration field in type.LayoutFields)
            {
                if (field.OffsetBits < 0 || field.OffsetBits % 8 != 0)
                {
                    throw new InvalidDataException("Missing private system layout: " + field.QualifiedName);
                }
                builder.AppendLine($"        [FieldOffset({field.OffsetBits / 8})]");
                if (field.Type.Kind == "CONSTANTARRAY")
                {
                    builder.AppendLine($"        internal fixed {MapSystem(field.Type.Element!)} {TypeMapper.PascalCase(field.Name)}[{field.Type.Count}];");
                }
                else
                {
                    builder.AppendLine($"        internal {MapSystem(field.Type)} {TypeMapper.PascalCase(field.Name)};");
                }
                builder.AppendLine();
            }
            builder.AppendLine("    }");
            builder.AppendLine();
        }

        foreach ((string name, string expression) in snapshot.Security.Constants)
        {
            string symbol = SystemName(name);
            if (name == "WINTRUST_ACTION_GENERIC_VERIFY_V2")
            {
                uint[] values = Regex.Matches(expression, "0x[0-9a-fA-F]+|[0-9]+").Select(match => Convert.ToUInt32(match.Value, match.Value.StartsWith("0x", StringComparison.Ordinal) ? 16 : 10)).ToArray();
                if (values.Length != 11)
                {
                    throw new InvalidDataException("Missing WinTrust policy identifier.");
                }
                Guid guid = new(unchecked((int)values[0]), unchecked((short)values[1]), unchecked((short)values[2]), [.. values.Skip(3).Select(value => (byte)value)]);
                builder.AppendLine($"    internal static readonly System.Guid {symbol} = new(\"{guid:D}\");");
            }
            else if (name.StartsWith("szOID_", StringComparison.Ordinal))
            {
                builder.AppendLine($"    internal static ReadOnlySpan<byte> {symbol} => " + (expression.StartsWith('"') ? expression + "u8" : SystemName(expression)) + ";");
            }
            else
            {
                string value = Regex.Replace(expression, @"\(\s*LPCSTR\s*\)", "");
                value = Regex.Replace(value, @"\b[A-Za-z_]\w*\b", match => snapshot.Security.Constants.ContainsKey(match.Value) ? SystemName(match.Value) : match.Value);
                value = Regex.Replace(value, @"(?<=\d)[uUlL]+\b", "");
                value = Regex.Replace(value, @"(<<|>>)\s*(\w+)", "$1 (int)$2");
                builder.AppendLine($"    internal const uint {symbol} = {value};");
            }
        }

        NativeDeclaration signer = snapshot.Declarations.Single(declaration => declaration.Name == "isSignedByNVIDIA");
        Match key = Regex.Match(signer.Source, @"s_rsaStreamlinePublicKey\[\]\s*=\s*\{(.*?)\};", RegexOptions.Singleline);
        if (!key.Success)
        {
            throw new InvalidDataException("Missing NVIDIA signature identity data.");
        }
        builder.AppendLine();
        builder.AppendLine("    internal static ReadOnlySpan<byte> NvidiaPublicKey =>");
        builder.AppendLine("    [");
        foreach (string line in key.Groups[1].Value.Trim().Split('\n'))
        {
            builder.AppendLine("        " + line.Trim());
        }
        builder.AppendLine("    ];");
        builder.AppendLine("}");
    }
}
