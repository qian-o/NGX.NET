namespace NGX.NET.Generator;

internal class StructEmitter(TypeMapper mapper, Dictionary<string, string> files)
{
    private static readonly Regex NumericInitializer = new(@"^-?\d+(?:\.\d+f)?$");
    private static readonly Regex ZeroInitializer = new(@"^\{[0 ,.f]+\}$");

    internal void WriteRecord(string name, AstRecord record)
    {
        if (record.IsOpaque)
        {
            if (name is "NVSDK_NGX_Handle" or "NVSDK_NGX_Parameter")
            {
                WriteHandle(name);
            }

            return;
        }

        string managed = mapper.GetRecordName(name);
        CodeWriter text = CreateFile();
        text.WriteLine(record.IsUnion ? $"[StructLayout(LayoutKind.Explicit, Size = {record.SizeInBytes})]" : "[StructLayout(LayoutKind.Sequential)]");
        string modifier = record.Fields.Any(field => mapper.GetFieldType(name, field).Contains('*')) ? "unsafe " : string.Empty;
        text.BeginBlock($"public {modifier}struct {managed}");

        foreach (AstField field in record.Fields)
        {
            if (record.IsUnion)
            {
                text.WriteLine($"[FieldOffset({field.OffsetInBits / 8})]");
            }

            text.WriteLine($"public {mapper.GetFieldType(name, field)} {GetFieldName(name, field.Name)};");
            text.WriteBlankLine();
        }

        WriteDefaults(text, name, managed, record.Fields);

        foreach (AstField field in record.Fields.Where(field => field.Type.Kind is NativeTypeKind.ConstantArray && GetMathFieldType(name, field) is null))
        {
            string element = mapper.GetManagedType(field.Type.Element!);
            text.WriteLine($"[InlineArray({field.Type.Count})]");
            text.BeginBlock($"public struct {GetFieldName(name, field.Name)}Buffer");
            text.WriteLine($"private {(element.Contains('*') ? "nint" : element)} element;");
            text.EndBlock();
        }

        text.EndBlock();
        files[$"Structs/{managed}.g.cs"] = text.ToString();
    }

    private void WriteHandle(string native)
    {
        string name = GetTypeName(native);
        CodeWriter text = CreateFile();
        text.WriteLine("[StructLayout(LayoutKind.Sequential)]");
        text.BeginBlock($"public readonly struct {name}(nint value) : IEquatable<{name}>");
        text.WriteLine("public readonly nint Value = value;");
        text.WriteBlankLine();
        text.WriteLine("public bool IsNull => Value is 0;");
        text.WriteBlankLine();
        text.BeginBlock($"public bool Equals({name} other)");
        text.WriteLine("return Value == other.Value;");
        text.EndBlock();
        text.BeginBlock("public override bool Equals(object? obj)");
        text.WriteLine($"return obj is {name} other && Equals(other);");
        text.EndBlock();
        text.BeginBlock("public override int GetHashCode()");
        text.WriteLine("return Value.GetHashCode();");
        text.EndBlock();
        text.BeginBlock("public override string ToString()");
        text.WriteLine($"return $\"{name} {{{{ Value = {{Value}}, IsNull = {{IsNull}} }}}}\";");
        text.EndBlock();
        text.BeginBlock($"public static bool operator ==({name} left, {name} right)");
        text.WriteLine("return left.Equals(right);");
        text.EndBlock();
        text.BeginBlock($"public static bool operator !=({name} left, {name} right)");
        text.WriteLine("return !left.Equals(right);");
        text.EndBlock();
        text.EndBlock();
        files[$"Structs/{name}.g.cs"] = text.ToString();
    }

    private static void WriteDefaults(CodeWriter text, string record, string name, ReadOnlySpan<AstField> fields)
    {
        List<(string Field, string Value)> defaults = [];
        foreach (AstField field in fields)
        {
            if (field.Declaration is not string declaration || !declaration.Contains('='))
            {
                continue;
            }

            string value = declaration.Split('=', 2)[1].Trim();
            if (NumericInitializer.IsMatch(value))
            {
                if (decimal.Parse(value.TrimEnd('f', 'F'), CultureInfo.InvariantCulture) is 0)
                {
                    continue;
                }

                if (field.Type.Kind is NativeTypeKind.Float)
                {
                    value = float.Parse(value.TrimEnd('f', 'F'), CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);

                    if (!value.Contains('.') && !value.Contains('E'))
                    {
                        value += ".0";
                    }

                    value += "f";
                }
                else if (field.Type.Kind is NativeTypeKind.UInt)
                {
                    value += "u";
                }

                defaults.Add((GetFieldName(record, field.Name), value));
            }
            else if (!ZeroInitializer.IsMatch(value))
            {
                throw new InvalidOperationException($"Unsupported initializer: {value}.");
            }
        }

        if (defaults.Count is 0)
        {
            return;
        }

        text.BeginBlock($"public {name}()");
        text.WriteLine("this = default;");

        foreach ((string field, string value) in defaults)
        {
            text.WriteLine($"{field} = {value};");
        }

        text.EndBlock();
    }
}
