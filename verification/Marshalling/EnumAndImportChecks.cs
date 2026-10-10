namespace Marshalling;

internal static class EnumAndImportChecks
{
    internal static void Run(string root)
    {
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        HashSet<string> enums = [.. Directory.GetFiles(Path.Combine(root, "NGX.NET/Enums"), "*.g.cs").Select(static path => Path.GetFileName(path)[..^5])];
        HashSet<string> imports = [.. typeof(Ngx).GetNestedTypes(BindingFlags.Public).Append(typeof(Ngx)).SelectMany(static type => type.GetMethods(BindingFlags.Static | BindingFlags.NonPublic)).Select(static method => method.GetCustomAttribute<LibraryImportAttribute>()?.EntryPoint).OfType<string>()];
        int enumValues = 0;
        int functions = 0;
        foreach (JsonProperty platform in ast.RootElement.GetProperty("platforms").EnumerateObject())
        {
            foreach (JsonElement item in platform.Value.GetProperty("enums").EnumerateArray())
            {
                string name = item.GetProperty("name").GetString()!;
                string managedName = TypeMapper.TypeName(name);
                Assert(enums.Contains(managedName), name + " generated enum source");
                Type type = typeof(Ngx).Assembly.GetType("NGX.NET." + managedName, true)!;
                foreach (JsonElement entry in item.GetProperty("values").EnumerateArray())
                {
                    string member = TypeMapper.EnumMember(name, entry.GetProperty("name").GetString()!);
                    Assert(unchecked((uint)Convert.ToInt64(Enum.Parse(type, member))) == unchecked((uint)entry.GetProperty("value").GetInt64()), type.Name + "." + member);
                    enumValues++;
                }
            }

            foreach (JsonElement function in platform.Value.GetProperty("functions").EnumerateArray())
            {
                Assert(imports.Contains(function.GetProperty("export").GetString()!), "Missing native import");
                functions++;
            }
        }

        Assert(Enum.IsDefined(NGXEngineType.Custom) && Enum.IsDefined(NGXPerfQualityValue.Dlaa) && Enum.IsDefined(NGXResult.FailFeatureNotSupported), "PascalCase enums");
        Console.WriteLine($"INFO {enumValues} AST enum value comparisons; {functions} AST native import comparisons.");

        Console.WriteLine("PASS enum values and native import names match the SDK");
    }
}
