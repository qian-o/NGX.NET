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
        AstField[] fields = [.. record.Fields];
        bool union = record.IsUnion;
        List<(string Name, string Element, int Count)> buffers = [];
        foreach (AstField field in fields)
        {
            AstType type = field.Type;
            if (type.Kind is not NativeTypeKind.ConstantArray || MathFieldType(name, field) is not null)
            {
                continue;
            }

            string element = mapper.Type(type.Element!);
            if (element is "sbyte" or "byte" or "float" or "uint" or "int")
            {
                continue;
            }

            if (element.EndsWith('*'))
            {
                element = $"NGXPointer<{element[..^1]}>";
            }

            buffers.Add((Name(field.Name) + "Buffer", element, type.Count));
        }

        CodeWriter publicText = CreateFile();
        publicText.BeginBlock($"public struct {managed}");
        CodeWriter nativeText = CreateFile();
        nativeText.Line($"[StructLayout(LayoutKind.Explicit, Size = {record.Size})]");
        nativeText.BeginBlock($"internal unsafe struct {managed}Native : IDisposable");

        foreach (AstField field in fields)
        {
            string fieldName = field.Name;
            AstType type = field.Type;

            if (!(name is "NVSDK_NGX_PathListInfo" && fieldName is "Length"))
            {
                publicText.Line($"public {mapper.PublicFieldType(name, field)} {PublicFieldName(name, fieldName)};");
                publicText.BlankLine();
            }

            nativeText.Line($"[FieldOffset({field.Offset / 8})]");

            if (MathFieldType(name, field) is string mathType)
            {
                nativeText.Line($"public {mathType} {Name(fieldName)};");
            }
            else if (type.Kind is NativeTypeKind.ConstantArray)
            {
                string element = mapper.Type(type.Element!);
                if (element is "sbyte" or "byte" or "float" or "uint" or "int")
                {
                    nativeText.Line($"public fixed {element} {Name(fieldName)}[{type.Count}];");
                }
                else
                {
                    nativeText.Line($"public {Name(fieldName)}Buffer {Name(fieldName)};");
                }
            }
            else
            {
                nativeText.Line($"public {mapper.Type(type)} {Name(fieldName)};");
            }

            nativeText.BlankLine();
        }

        WriteDefaults(publicText, name, managed, fields);
        nativeText.BeginBlock($"public {managed}Native(in {managed} value)");
        if (NeedsDefaultInitialization(record))
        {
            nativeText.Line("this = default;");
            nativeText.BlankLine();
        }

        nativeText.BeginBlock("try");

        if (union)
        {
            AstField[] branches = [.. fields.Where(field => mapper.IsRecord(field.Type))];
            if (branches.Length > 1)
            {
                WriteGuard(nativeText, $"value.{PublicFieldName(name, branches[0].Name)}.HasValue && value.{PublicFieldName(name, branches[1].Name)}.HasValue", "throw new ArgumentException(\"Only one union member may be specified.\", nameof(value));");
            }

            for (int i = 0; i < branches.Length; i++)
            {
                string field = Name(branches[i].Name);
                string source = PublicFieldName(name, branches[i].Name);
                string local = ParameterName(branches[i].Name);
                string type = mapper.PublicType(branches[i].Type);
                nativeText.BeginBlock($"{(i is 0 ? "if" : "else if")} (value.{source} is {type} {local})", continuation: i is not 0);
                nativeText.Line($"{field} = new(in {local});");
                nativeText.EndBlock();
            }

            foreach (AstField field in fields.Where(field => !mapper.IsRecord(field.Type)))
            {
                nativeText.BeginBlock("else", continuation: true);
                nativeText.Line($"{Name(field.Name)} = value.{PublicFieldName(name, field.Name)};");
                nativeText.EndBlock();
            }
        }
        else
        {
            if (name is "NVSDK_NGX_LoggingInfo")
            {
                WriteGuard(nativeText, "value.DisableOtherLoggingSinks && value.LoggingCallback is null", "throw new ArgumentException(\"A logging callback is required when disabling other logging sinks.\", nameof(value));");
            }

            if (name is "NVSDK_NGX_Application_Identifier")
            {
                WriteGuard(nativeText, "(value.IdentifierType is NGXApplicationIdentifierType.ProjectId) != value.V.ProjectDesc.HasValue", "throw new ArgumentException(\"Application identifier and active union member disagree.\", nameof(value));");
                WriteGuard(nativeText, "value.IdentifierType is not (NGXApplicationIdentifierType.ProjectId or NGXApplicationIdentifierType.ApplicationId)", "throw new ArgumentOutOfRangeException(nameof(value));");
            }

            if (name is "NVSDK_NGX_Resource_VK")
            {
                WriteGuard(nativeText, "(value.Type is NGXResourceVKType.VkImageView && value.Resource.BufferInfo.HasValue) || (value.Type is NGXResourceVKType.VkBuffer && value.Resource.ImageViewInfo.HasValue)", "throw new ArgumentException(\"Resource type and union member disagree.\", nameof(value));");
            }

            foreach (AstField field in fields)
            {
                EmitFieldConstruction(nativeText, name, field);
            }
        }

        nativeText.EndBlock();
        nativeText.BeginBlock("catch", continuation: true);
        nativeText.Line("Dispose();");
        nativeText.BlankLine();
        nativeText.Line("throw;");
        nativeText.EndBlock();
        nativeText.EndBlock();
        nativeText.BeginBlock("public void Dispose()");

        if (!union)
        {
            foreach (AstField field in fields.Reverse())
            {
                EmitFieldDisposal(nativeText, name, field);
            }
        }

        nativeText.Line("this = default;");
        nativeText.EndBlock();

        foreach ((string bufferName, string element, int count) in buffers)
        {
            nativeText.Line($"[InlineArray({count})]");
            nativeText.BeginBlock($"internal struct {bufferName}");
            nativeText.Line($"private {element} element;");
            nativeText.EndBlock();
        }

        nativeText.EndBlock();

        if (!union)
        {
            publicText.BeginBlock($"internal unsafe {managed}(in {managed}Native native)");

            foreach (AstField field in fields)
            {
                EmitFieldRead(publicText, name, field);
            }

            publicText.EndBlock();
        }

        publicText.EndBlock();
        files[$"Structs/{managed}.g.cs"] = publicText.ToString();
        files[$"Structs/Native/{managed}Native.g.cs"] = nativeText.ToString();
    }

    private bool NeedsDefaultInitialization(AstRecord record)
    {
        if (record.IsUnion || record.Name is "NVSDK_NGX_PathListInfo")
        {
            return true;
        }

        return record.Fields.Any(field => (field.Type.Kind is NativeTypeKind.ConstantArray && MathFieldType(record.Name, field) is null) || (field.Type is { Kind: NativeTypeKind.Pointer, Element: not null } type && mapper.IsRecord(type.Element)));
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
        text.BeginBlock("public void Deconstruct(out nint value)");
        text.Line("value = Value;");
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

    private void EmitFieldConstruction(CodeWriter text, string record, AstField field)
    {
        string name = field.Name;
        string target = Name(name);
        string source = "value." + PublicFieldName(record, name);
        AstType type = field.Type;

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Length")
            {
                return;
            }

            text.BlankLine();
            text.BeginBlock("if (value.Paths is string[] paths && paths.Length > 0)");
            text.Line("Path = (void**)NativeMemory.AllocZeroed(checked((nuint)paths.Length * (nuint)sizeof(void*)));");
            text.Line("Length = checked((uint)paths.Length);");
            text.BlankLine();
            text.BeginBlock("for (int i = 0; i < paths.Length; i++)");
            text.Line("ArgumentNullException.ThrowIfNull(paths[i]);");
            text.Line("Path[i] = NGXMarshal.TextToPtr(paths[i], NGXEncoding.NativeWide);");
            text.EndBlock();
            text.EndBlock();

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            text.Line(math.EndsWith('*') ? $"{target} = {source}.HasValue ? NGXMarshal.AllocValue({source}.Value) : null;" : $"{target} = {source};");

            return;
        }

        if (type.Kind is NativeTypeKind.ConstantArray)
        {
            AstType element = type.Element!;
            int count = type.Count;
            text.BlankLine();

            if (element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU)
            {
                text.BeginBlock($"fixed (sbyte* buffer = {target})");
                text.Line($"NGXMarshal.WriteUtf8({source}, new Span<byte>(buffer, {count}));");
                text.EndBlock();
            }
            else
            {
                string input = ParameterName(name);
                text.BeginBlock($"if ({source} is {mapper.PublicFieldType(record, field).TrimEnd('?')} {input})");
                WriteGuard(text, $"{input}.Length > {count}", $"throw new ArgumentException(\"{target} accepts at most {count} elements.\", nameof(value));");
                text.BeginBlock($"for (int i = 0; i < {input}.Length; i++)");
                EmitAssignment(text, element, $"{target}[i]", $"{input}[i]");
                text.EndBlock();
                text.EndBlock();
            }

            return;
        }

        EmitAssignment(text, type, target, source);
    }

    private void EmitAssignment(CodeWriter text, AstType type, string target, string source)
    {
        string expression;
        NativeTypeKind kind = type.Kind;
        if (kind is NativeTypeKind.Record)
        {
            expression = $"new(in {source})";
        }
        else if (kind is NativeTypeKind.Pointer)
        {
            AstType element = type.Element!;
            NativeTypeKind elementKind = element.Kind;
            if (mapper.CallbackName(type) is not null)
            {
                expression = $"NgxCallbacks.Acquire({source})";
            }
            else if (elementKind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                expression = $"{(mapper.Type(type) is "void*" ? string.Empty : $"({mapper.Type(type)})")}NGXMarshal.TextToPtr({source}, NGXEncoding.{(elementKind is NativeTypeKind.WChar ? "NativeWide" : "Utf8")})";
            }
            else if (mapper.IsRecord(element))
            {
                string local = ParameterName(Regex.Replace(target, @"\[.*\]", string.Empty)) + (target.Contains('[') ? "Element" : string.Empty);
                WriteGuard(text, $"{source} is {mapper.PublicType(element)} {local}", $"{target} = NGXMarshal.AllocNative<{mapper.Type(element)}>(new(in {local}));");

                return;
            }
            else if (elementKind is NativeTypeKind.ULongLong or NativeTypeKind.ULong)
            {
                expression = $"{source}.HasValue ? NGXMarshal.AllocValue({source}.GetValueOrDefault()) : null";
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

        text.Line($"{target} = {expression};");
    }

    private void EmitFieldDisposal(CodeWriter text, string record, AstField field)
    {
        string name = field.Name;
        string target = Name(name);
        AstType type = field.Type;

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Path")
            {
                text.BeginBlock("if (Path is not null)");
                text.BeginBlock("for (int i = checked((int)Length) - 1; i >= 0; i--)");
                text.Line("NGXMarshal.Free(Path[i]);");
                text.EndBlock();
                text.Line("NativeMemory.Free(Path);");
                text.EndBlock();
            }

            return;
        }

        if (record is "NVSDK_NGX_Application_Identifier" && name is "v")
        {
            WriteGuard(text, "IdentifierType is NGXApplicationIdentifierType.ProjectId", "V.ProjectDesc.Dispose();");

            return;
        }

        if (MathFieldType(record, field) is string math)
        {
            if (math.EndsWith('*'))
            {
                text.Line($"NGXMarshal.Free({target});");
            }

            return;
        }

        if (type.Kind is NativeTypeKind.ConstantArray)
        {
            string? release = ReleaseExpression(type.Element!, $"{target}[i]", mapper.Type(type.Element!).EndsWith('*'));
            if (release is not null)
            {
                text.BlankLine();
                text.BeginBlock($"for (int i = {type.Count - 1}; i >= 0; i--)");
                text.Line(release);
                text.EndBlock();
            }
        }
        else if (ReleaseExpression(type, target) is string release)
        {
            text.Line(release);
        }
    }

    private string? ReleaseExpression(AstType type, string value, bool pointerWrapper = false)
    {
        if (mapper.IsRecord(type))
        {
            return value + ".Dispose();";
        }

        if (type.Kind is not NativeTypeKind.Pointer)
        {
            return null;
        }

        if (mapper.CallbackName(type) is not null)
        {
            return $"NgxCallbacks.Release({value});";
        }

        AstType element = type.Element!;
        if (mapper.IsRecord(element))
        {
            return $"NGXMarshal.FreeNative{(pointerWrapper ? $"<{mapper.Type(element)}>" : string.Empty)}({value});";
        }

        return element.Kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar or NativeTypeKind.ULongLong or NativeTypeKind.ULong ? $"NGXMarshal.Free({value});" : null;
    }

    private void EmitFieldRead(CodeWriter text, string record, AstField field)
    {
        string name = field.Name;
        string target = PublicFieldName(record, name);
        string source = "native." + Name(name);
        AstType type = field.Type;

        if (record is "NVSDK_NGX_PathListInfo")
        {
            if (name is "Path")
            {
                text.Line("Paths = new string[checked((int)native.Length)];");
                text.BeginBlock("for (int i = 0; i < Paths.Length; i++)");
                text.Line("Paths[i] = NGXMarshal.PtrToString(native.Path[i], NGXEncoding.NativeWide)!;");
                text.EndBlock();
            }

            return;
        }

        if (record is "NVSDK_NGX_Application_Identifier" && name is "v")
        {
            text.Line("V = native.IdentifierType is NGXApplicationIdentifierType.ProjectId ? new() { ProjectDesc = new NGXProjectIdDescription(in native.V.ProjectDesc) } : new() { ApplicationId = native.V.ApplicationId };");

            return;
        }

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
                text.BeginBlock($"fixed (sbyte* buffer = {source})");
                text.Line($"{target} = NGXMarshal.ReadUtf8(new ReadOnlySpan<byte>(buffer, {type.Count}));");
                text.EndBlock();
            }
            else
            {
                text.Line($"{target} = new {mapper.PublicType(element)}[{type.Count}];");
                text.BeginBlock($"for (int i = 0; i < {target}.Length; i++)");
                text.Line($"{target}[i] = {ReadExpression(element, source + "[i]", mapper.Type(element).EndsWith('*'))};");
                text.EndBlock();
            }
        }
        else
        {
            text.Line($"{target} = {ReadExpression(type, source)};");
        }
    }

    private string ReadExpression(AstType type, string source, bool pointerWrapper = false)
    {
        if (mapper.IsRecord(type))
        {
            return $"new(in {source})";
        }

        if (type.Kind is NativeTypeKind.Pointer)
        {
            if (mapper.CallbackName(type) is string cb)
            {
                return $"{source} is 0 ? null : Marshal.GetDelegateForFunctionPointer<{cb}>({source})";
            }

            AstType element = type.Element!;
            NativeTypeKind kind = element.Kind;
            if (kind is NativeTypeKind.CharS or NativeTypeKind.CharU or NativeTypeKind.WChar)
            {
                return $"NGXMarshal.PtrToString({source}, NGXEncoding.{(kind is NativeTypeKind.WChar ? "NativeWide" : "Utf8")})";
            }

            if (mapper.IsRecord(element))
            {
                string pointer = pointerWrapper ? $"({mapper.Type(type)}){source}" : source;

                return $"{(pointerWrapper ? $"({pointer})" : pointer)} is null ? null : new {mapper.PublicType(element)}(in *{pointer})";
            }

            if (kind is NativeTypeKind.ULongLong or NativeTypeKind.ULong)
            {
                string pointer = pointerWrapper ? $"({mapper.Type(type)}){source}" : source;

                return $"{(pointerWrapper ? $"({pointer})" : pointer)} is null ? null : *{pointer}";
            }

            return mapper.Type(type) is "nint" ? source : "(nint)" + source;
        }

        return source;
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
