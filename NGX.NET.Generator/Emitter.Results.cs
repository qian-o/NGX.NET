using System.Text.RegularExpressions;

namespace NGX.NET.Generator;

internal partial class Emitter
{
    private readonly HashSet<string> signatures = [];
    private readonly Dictionary<string, (string Operation, (string Type, string Name)[] Fields)> resultTypes = [];

    private void RegisterFunction(string group, string method, List<string> declarations)
    {
        string parameters = string.Join(", ", declarations.Select(static declaration => Regex.Replace(declaration[..declaration.LastIndexOf(' ')], @"^(?:in|out|ref) ", "ref ")));
        string signature = $"{group}.{method}({parameters})";
        if (!signatures.Add(signature))
        {
            throw new InvalidOperationException($"Conflicting managed overload: {signature}.");
        }
    }

    private void WriteResultFunction(CodeWriter text, string group, string method, List<string> declarations, List<string> forward, List<(int Index, string Type, string Name)> outputs)
    {
        HashSet<int> indices = [.. outputs.Select(static output => output.Index)];
        List<string> inputs = [.. declarations.Where((_, index) => !indices.Contains(index))];
        List<string> arguments = [.. forward];
        foreach ((int index, string type, string name) in outputs)
        {
            arguments[index] = $"out {type} {name}";
        }

        string operation = group.Length is 0 ? $"Ngx.{method}" : $"Ngx.{group}.{method}";
        string returned = outputs[0].Type;
        string value = outputs[0].Name;
        if (outputs.Count > 1)
        {
            returned = ResultTypeName(method);
            (string Type, string Name)[] fields = [.. outputs.Select(static output => (output.Type, ResultPropertyName(output.Name)))];
            if (fields.Any(field => field.Name == returned || !Regex.IsMatch(field.Name, @"^[A-Z][A-Za-z0-9_]*$")) || fields.Select(static field => field.Name).Distinct(StringComparer.Ordinal).Count() != fields.Length)
            {
                throw new InvalidOperationException($"Conflicting or invalid result properties: {operation} -> {returned}.");
            }

            if (resultTypes.TryGetValue(returned, out (string Operation, (string Type, string Name)[] Fields) existing))
            {
                if (!existing.Fields.SequenceEqual(fields))
                {
                    throw new InvalidOperationException($"Conflicting result type {returned}: {existing.Operation} and {operation} have different outputs.");
                }
            }
            else
            {
                resultTypes.Add(returned, (operation, fields));
            }

            value = $"new({string.Join(", ", outputs.Select(static output => output.Name))})";
        }

        RegisterFunction(group, method, inputs);
        WriteSummary(text, $"Returns the outputs of {operation} after checking the NGX result.");
        text.Line("/// <exception cref=\"NGXException\">The NGX operation failed.</exception>");
        text.BeginBlock($"public static {returned} {method}({string.Join(", ", inputs)})");
        text.Line($"ThrowIfFailed({method}({string.Join(", ", arguments)}), \"{operation}\");");
        text.BlankLine();
        text.Line($"return {value};");
        text.EndBlock();
    }

    private void WriteResultTypes()
    {
        foreach ((string name, (string operation, (string Type, string Name)[] fields)) in resultTypes.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
        {
            if (name is "Ngx" || files.Keys.Any(path => path.EndsWith($"/{name}.g.cs", StringComparison.Ordinal)))
            {
                throw new InvalidOperationException($"Result type {name} conflicts with an existing generated type: {operation}.");
            }

            CodeWriter text = CreateFile();
            WriteSummary(text, $"Managed outputs of {operation[(operation.LastIndexOf('.') + 1)..]}.");
            string parameters = string.Join(", ", fields.Select(static field => $"{field.Type} {ParameterName(field.Name)}"));
            text.BeginBlock($"public readonly struct {name}({parameters})");
            foreach ((string type, string property) in fields)
            {
                WriteSummary(text, $"The {property} output.");
                text.Line($"public {type} {property} {{ get; }} = {ParameterName(property)};");
                text.BlankLine();
            }

            text.EndBlock();
            files[$"Types/{name}.g.cs"] = text.ToString();
        }
    }

    private static string ResultTypeName(string method)
    {
        // Strip an action word only at a PascalCase word boundary; preserve version suffixes.
        string name = Regex.Replace(method, @"^(?:Get|Query|Enumerate|Create|Allocate|Estimate|Calculate|Required)(?=[A-Z])", "");

        return name == method ? method + "Result" : name;
    }

    private static string ResultPropertyName(string parameter)
    {
        // Remove native pointer/direction prefixes, then normalize managed output names.
        string name = Regex.Replace(parameter, @"^p+(?=[A-Z_])_?", "");
        name = Regex.Replace(name, @"^(?:Out|out)(?=[A-Z_]|$)_?", "");
        name = Name(name);

        return Regex.Replace(name, @"Exts(?=[A-Z0-9]|$)", "Extensions");
    }
}
