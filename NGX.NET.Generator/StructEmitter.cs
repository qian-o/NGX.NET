namespace NGX.NET.Generator;

internal class StructEmitter(TypeMapper mapper, Dictionary<string, string> files)
{
    private static readonly Regex numericInitializer = new(@"^-?\d+(?:\.\d+f)?$");
    private static readonly Regex zeroInitializer = new(@"^\{[0 ,.f]+\}$");

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

        string managed = mapper.ManagedRecord(name);
        CodeWriter text = CreateFile();
        text.BeginBlock($"public struct {managed}");

        foreach (AstField field in record.Fields)
        {
            if (!(name is "NVSDK_NGX_PathListInfo" && field.Name is "Length"))
            {
                text.Line($"public {mapper.PublicFieldType(name, field)} {PublicFieldName(name, field.Name)};");
                text.BlankLine();
            }
        }

        WriteDefaults(text, name, managed, record.Fields);

        if (mapper.HasOutputConversion(name))
        {
            text.BeginBlock($"internal unsafe {managed}(in {managed}Native native)");

            foreach (AstField field in record.Fields)
            {
                EmitFieldRead(text, name, field);
            }

            text.EndBlock();
        }

        text.EndBlock();
        files[$"Structs/{managed}.g.cs"] = text.ToString();

        if (mapper.HasNativeRecord(name))
        {
            WriteNativeRecord(record, managed);
        }
    }

    private void WriteNativeRecord(AstRecord record, string managed)
    {
        bool primaryConstructor = mapper.HasInputConversion(record.Name) && !record.IsUnion && record.Name is not ("NVSDK_NGX_PathListInfo" or "NVSDK_NGX_LoggingInfo" or "NVSDK_NGX_Application_Identifier" or "NVSDK_NGX_Resource_VK") && record.Fields.All(field => (field.Type.Kind is not NativeTypeKind.ConstantArray || MathFieldType(record.Name, field) is not null) && mapper.CallbackName(field.Type) is null);
        string scope = mapper.Allocates(record.Name) ? ", NativeScope scope" : string.Empty;
        string parameters = primaryConstructor ? $"(in {managed} value{scope})" : string.Empty;
        CodeWriter text = CreateFile();
        text.Line($"[StructLayout(LayoutKind.Explicit, Size = {record.Size})]");
        text.BeginBlock($"internal unsafe struct {managed}Native{parameters}");
        List<(string Name, string Element, int Count)> buffers = [];

        foreach (AstField field in record.Fields)
        {
            string name = NativeFieldName(field.Name);
            AstType type = field.Type;
            string initializer = primaryConstructor ? $" = {FieldInitializer(record.Name, field)}" : string.Empty;
            text.Line($"[FieldOffset({field.Offset / 8})]");

            if (MathFieldType(record.Name, field) is string math)
            {
                text.Line($"public {math} {name}{initializer};");
            }
            else if (type.Kind is NativeTypeKind.ConstantArray)
            {
                string element = mapper.Type(type.Element!);

                if (element is "sbyte" or "byte" or "float" or "uint" or "int")
                {
                    text.Line($"public fixed {element} {name}[{type.Count}];");
                }
                else
                {
                    element = element.EndsWith('*') ? "nint" : element;
                    string buffer = name + "Buffer";
                    buffers.Add((buffer, element, type.Count));
                    text.Line($"public {buffer} {name};");
                }
            }
            else
            {
                text.Line($"public {mapper.Type(type)} {name}{initializer};");
            }

            text.BlankLine();
        }

        if (mapper.HasInputConversion(record.Name) && !primaryConstructor)
        {
            WriteNativeConstructor(text, record, managed);
        }

        foreach ((string name, string element, int count) in buffers)
        {
            text.Line($"[InlineArray({count})]");
            text.BeginBlock($"internal struct {name}");
            text.Line($"private {element} element;");
            text.EndBlock();
        }

        text.EndBlock();
        files[$"Structs/Native/{managed}Native.g.cs"] = text.ToString();
    }

    private void WriteNativeConstructor(CodeWriter text, AstRecord record, string managed)
    {
        string scope = mapper.Allocates(record.Name) ? ", NativeScope scope" : string.Empty;
        text.BeginBlock($"public {managed}Native(in {managed} value{scope})");

        if (record.IsUnion || record.Name is "NVSDK_NGX_PathListInfo" || record.Fields.Any(field => field.Type.Kind is NativeTypeKind.ConstantArray && MathFieldType(record.Name, field) is null))
        {
            text.Line("this = default;");
        }

        if (record.IsUnion)
        {
            WriteUnionConstructor(text, record);
        }
        else
        {
            WriteConstructorValidation(text, record.Name);

            foreach (AstField field in record.Fields)
            {
                EmitFieldConstruction(text, record.Name, field);
            }
        }

        text.EndBlock();
    }

    private void WriteUnionConstructor(CodeWriter text, AstRecord record)
    {
        AstField[] branches = [.. record.Fields.Where(field => mapper.IsRecord(field.Type))];
        if (branches.Length > 1)
        {
            WriteGuard(text, $"value.{PublicFieldName(record.Name, branches[0].Name)}.HasValue && value.{PublicFieldName(record.Name, branches[1].Name)}.HasValue", "throw new ArgumentException(\"Only one union member may be specified.\", nameof(value));");
        }

        for (int i = 0; i < branches.Length; i++)
        {
            AstField branch = branches[i];
            string target = NativeFieldName(branch.Name);
            string source = PublicFieldName(record.Name, branch.Name);
            string local = ParameterName(branch.Name);
            string type = mapper.PublicType(branch.Type);
            string scope = mapper.Allocates(branch.Type.Name) ? ", scope" : string.Empty;
            text.BeginBlock($"{(i is 0 ? "if" : "else if")} (value.{source} is {type} {local})", continuation: i is not 0);
            text.Line($"{target} = new(in {local}{scope});");
            text.EndBlock();
        }

        foreach (AstField field in record.Fields.Where(field => !mapper.IsRecord(field.Type)))
        {
            text.BeginBlock("else", continuation: true);
            text.Line($"{NativeFieldName(field.Name)} = value.{PublicFieldName(record.Name, field.Name)};");
            text.EndBlock();
        }
    }

    private void EmitFieldConstruction(CodeWriter text, string record, AstField field)
    {
        string target = NativeFieldName(field.Name);
        string source = "value." + PublicFieldName(record, field.Name);
        AstType type = field.Type;

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (field.Name is "Path")
            {
                text.BlankLine();
                text.BeginBlock("if (value.Paths is string[] paths && paths.Length > 0)");
                text.Line("Path = (void**)scope.Alloc<nint>(paths.Length);");
                text.Line("Length = (uint)paths.Length;");
                text.BlankLine();
                text.BeginBlock("for (int i = 0; i < paths.Length; i++)");
                text.Line("ArgumentNullException.ThrowIfNull(paths[i]);");
                text.Line("Path[i] = scope.AllocWide(paths[i]);");
                text.EndBlock();
                text.EndBlock();
            }

            return;
        }

        if (MathFieldType(record, field) is not null)
        {
            text.Line($"{target} = {FieldInitializer(record, field)};");

            return;
        }

        if (type.Kind is NativeTypeKind.ConstantArray)
        {
            AstType element = type.Element!;
            string input = ParameterName(field.Name);
            text.BlankLine();
            text.BeginBlock($"if ({source} is {mapper.PublicFieldType(record, field).TrimEnd('?')} {input})");
            WriteGuard(text, $"{input}.Length > {type.Count}", $"throw new ArgumentException(\"{target} accepts at most {type.Count} elements.\", nameof(value));");
            text.BeginBlock($"for (int i = 0; i < {input}.Length; i++)");
            EmitAssignment(text, element, $"{target}[i]", $"{input}[i]", mapper.Type(element).EndsWith('*'));
            text.EndBlock();
            text.EndBlock();

            return;
        }

        EmitAssignment(text, type, target, source);
    }

    private void EmitAssignment(CodeWriter text, AstType type, string target, string source, bool pointerStorage = false)
    {
        if (mapper.CallbackName(type) is string callback)
        {
            string local = ParameterName(target);
            text.Line($"{callback}? {local} = CallbackGuard.Wrap({source});");
            text.Line($"{target} = {local} is null ? 0 : scope.Keep({local});");

            return;
        }

        text.Line($"{target} = {AssignmentExpression(type, target, source, pointerStorage)};");
    }

    private string FieldInitializer(string record, AstField field)
    {
        string source = "value." + PublicFieldName(record, field.Name);
        if (MathFieldType(record, field) is string math)
        {
            return math.EndsWith('*') ? $"{source}.HasValue ? scope.Alloc({source}.Value) : null" : source;
        }

        return AssignmentExpression(field.Type, NativeFieldName(field.Name), source);
    }

    private string AssignmentExpression(AstType type, string target, string source, bool pointerStorage = false)
    {
        string expression;

        if (mapper.IsRecord(type))
        {
            expression = $"new(in {source}{(mapper.Allocates(type.Name) ? ", scope" : string.Empty)})";
        }
        else if (type.Kind is NativeTypeKind.Pointer)
        {
            AstType element = type.Element!;

            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                expression = $"scope.{(element.Kind is NativeTypeKind.WChar ? "AllocWide" : "AllocUtf8")}({source})";
            }
            else if (mapper.IsRecord(element))
            {
                string local = ParameterName(Regex.Replace(target, @"\[.*\]", string.Empty)) + (pointerStorage ? "Element" : string.Empty);
                string scope = mapper.Allocates(element.Name) ? ", scope" : string.Empty;
                expression = $"{source} is {mapper.PublicType(element)} {local} ? {(pointerStorage ? "(nint)" : string.Empty)}scope.Alloc(new {mapper.Type(element)}(in {local}{scope})) : {(pointerStorage ? "0" : "null")}";
            }
            else if (element.Kind is NativeTypeKind.ULongLong or NativeTypeKind.ULong)
            {
                expression = $"{source}.HasValue ? {(pointerStorage ? "(nint)" : string.Empty)}scope.Alloc({source}.GetValueOrDefault()) : {(pointerStorage ? "0" : "null")}";
            }
            else
            {
                expression = mapper.Type(type) is "nint" ? source : $"({mapper.Type(type)}){source}";
            }
        }
        else
        {
            expression = source;
        }

        return expression;
    }

    private void WriteHandle(string native)
    {
        string name = TypeName(native);
        CodeWriter text = CreateFile();
        text.Line("[StructLayout(LayoutKind.Sequential)]");
        text.BeginBlock($"public readonly struct {name}(nint value) : IEquatable<{name}>");
        text.Line("public readonly nint Value = value;");
        text.BlankLine();
        text.Line("public bool IsNull => Value is 0;");
        text.BlankLine();
        text.BeginBlock($"public bool Equals({name} other)");
        text.Line("return Value == other.Value;");
        text.EndBlock();
        text.BeginBlock("public override bool Equals(object? obj)");
        text.Line($"return obj is {name} other && Equals(other);");
        text.EndBlock();
        text.BeginBlock("public override int GetHashCode()");
        text.Line("return Value.GetHashCode();");
        text.EndBlock();
        text.BeginBlock("public override string ToString()");
        text.Line($"return $\"{name} {{{{ Value = {{Value}}, IsNull = {{IsNull}} }}}}\";");
        text.EndBlock();
        text.BeginBlock($"public static bool operator ==({name} left, {name} right)");
        text.Line("return left.Equals(right);");
        text.EndBlock();
        text.BeginBlock($"public static bool operator !=({name} left, {name} right)");
        text.Line("return !left.Equals(right);");
        text.EndBlock();
        text.EndBlock();
        files[$"Structs/{name}.g.cs"] = text.ToString();
    }

    private void EmitFieldRead(CodeWriter text, string record, AstField field)
    {
        string name = field.Name;
        string target = PublicFieldName(record, name);
        string source = "native." + Name(name);
        AstType type = field.Type;

        if (record is "NVSDK_NGX_Resource_VK" && name is "Resource")
        {
            text.Line("Resource = native.Type is NGXResourceVKType.VkImageView ? new() { ImageViewInfo = new NGXImageViewInfoVK(in native.Resource.ImageViewInfo) } : new() { BufferInfo = new NGXBufferInfoVK(in native.Resource.BufferInfo) };");

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            text.Line($"{target} = {(math.EndsWith('*') ? source + " is null ? null : *" + source : source)};");

            return;
        }

        if (type.Kind is NativeTypeKind.ConstantArray)
        {
            AstType element = type.Element!;
            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU)
            {
                text.BlankLine();
                text.BeginBlock($"fixed (byte* buffer = {source})");
                text.Line($"{target} = NativeTextHelper.ReadUtf8(new ReadOnlySpan<byte>(buffer, {type.Count}));");
                text.EndBlock();
            }
            else
            {
                text.Line($"{target} = new {mapper.PublicType(element)}[{type.Count}];");
                text.BeginBlock($"for (int i = 0; i < {target}.Length; i++)");
                text.Line($"{target}[i] = {ReadExpression(element, source + "[i]")};");
                text.EndBlock();
            }
        }
        else
        {
            text.Line($"{target} = {ReadExpression(type, source)};");
        }
    }

    private string ReadExpression(AstType type, string source)
    {
        if (mapper.IsRecord(type))
        {
            return $"new(in {source})";
        }

        return type.Kind is NativeTypeKind.Pointer && mapper.Type(type) is not "nint" ? "(nint)" + source : source;
    }

    private static void WriteConstructorValidation(CodeWriter text, string name)
    {
        if (name is "NVSDK_NGX_LoggingInfo")
        {
            WriteGuard(text, "value.DisableOtherLoggingSinks && value.LoggingCallback is null", "throw new ArgumentException(\"A logging callback is required when disabling other logging sinks.\", nameof(value));");
        }

        if (name is "NVSDK_NGX_Application_Identifier")
        {
            WriteGuard(text, "(value.IdentifierType is NGXApplicationIdentifierType.ProjectId) != value.V.ProjectDesc.HasValue", "throw new ArgumentException(\"Application identifier and active union member disagree.\", nameof(value));");
            WriteGuard(text, "value.IdentifierType is not (NGXApplicationIdentifierType.ProjectId or NGXApplicationIdentifierType.ApplicationId)", "throw new ArgumentOutOfRangeException(nameof(value));");
        }

        if (name is "NVSDK_NGX_Resource_VK")
        {
            WriteGuard(text, "(value.Type is NGXResourceVKType.VkImageView && value.Resource.BufferInfo.HasValue) || (value.Type is NGXResourceVKType.VkBuffer && value.Resource.ImageViewInfo.HasValue)", "throw new ArgumentException(\"Resource type and union member disagree.\", nameof(value));");
        }
    }

    private static void WriteDefaults(CodeWriter text, string record, string name, AstField[] fields)
    {
        List<(string Field, string Value)> defaults = [];
        foreach (AstField field in fields)
        {
            if (field.Declaration is not string declaration || !declaration.Contains('='))
            {
                continue;
            }

            string value = declaration.Split('=', 2)[1].Trim();
            if (numericInitializer.IsMatch(value))
            {
                if (field.Type.Kind is NativeTypeKind.Float)
                {
                    value = float.Parse(value.TrimEnd('f', 'F'), CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);

                    if (!value.Contains('.') && !value.Contains('E'))
                    {
                        value += ".0";
                    }

                    value += "f";
                }

                defaults.Add((PublicFieldName(record, field.Name), value));
            }
            else if (!zeroInitializer.IsMatch(value))
            {
                throw new InvalidOperationException($"Unsupported initializer: {value}.");
            }
        }

        if (defaults.Count is 0)
        {
            return;
        }

        text.BeginBlock($"public {name}()");
        text.Line("this = default;");

        foreach ((string field, string value) in defaults)
        {
            text.Line($"{field} = {value};");
        }

        text.EndBlock();
    }
}
