namespace Marshalling;

internal static class LayoutChecks
{
    internal static void Run(string root)
    {
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        Models models = AstReader.Read(ast.RootElement);
        TypeMapper mapper = new(models);
        HashSet<string> nativeSources = [.. Directory.GetFiles(Path.Combine(root, "NGX.NET/Structs/Native"), "*.g.cs").Select(static path => Path.GetFileName(path)[..^5])];
        int layouts = 0;
        int offsets = 0;
        foreach (JsonProperty platform in ast.RootElement.GetProperty("platforms").EnumerateObject())
        {
            foreach (JsonElement record in platform.Value.GetProperty("records").EnumerateArray())
            {
                if (record.GetProperty("opaque").GetBoolean())
                {
                    continue;
                }

                string name = record.GetProperty("name").GetString()!;
                string managedName = mapper.ManagedRecord(name);
                Assert(nativeSources.Contains(managedName + "Native"), name + " generated native source");
                Type type = typeof(Ngx).Assembly.GetType("NGX.NET." + managedName + "Native", true)!;
                Type managed = typeof(Ngx).Assembly.GetType("NGX.NET." + managedName, true)!;
                Assert(type.IsNotPublic && typeof(IDisposable).IsAssignableFrom(type), name + " visibility/disposal");
                Assert(type.GetConstructor([managed.MakeByRefType()]) is not null, name + " public-struct constructor");
                Assert(!(bool)typeof(LayoutChecks).GetMethod(nameof(ContainsReferences), BindingFlags.NonPublic | BindingFlags.Static)!.MakeGenericMethod(type).Invoke(null, null)!, name + " managed native field");
                Assert(Marshal.SizeOf(type) == record.GetProperty("size").GetInt32(), platform.Name + " size " + name);
                layouts++;

                foreach (JsonElement field in record.GetProperty("fields").EnumerateArray())
                {
                    string fieldName = field.GetProperty("name").GetString()!;
                    string managedField = TypeMapper.NativeFieldName(fieldName);
                    Assert((long)Marshal.OffsetOf(type, managedField) == field.GetProperty("offset").GetInt64() / 8, name + "::" + fieldName);
                    offsets++;
                }
            }
        }

        Console.WriteLine($"INFO {nativeSources.Count} native types; {layouts} AST layout comparisons; {offsets} AST field offset comparisons.");

        Console.WriteLine("PASS all generated Native layouts and constructors match the four-target AST");
    }

    private static bool ContainsReferences<T>()
    {
        return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
    }
}
