namespace Marshalling;

internal static class LayoutChecks
{
    private static readonly string[] UnusedTypes = [nameof(NGXCoordinatesVK), nameof(NGXDLDenoiseCreateParams)];
    private static readonly string[] OutputTypes = [nameof(NGXFeatureRequirement), nameof(NGXVkExtensionProperties), nameof(NGXResourceVK), nameof(NGXImageViewInfoVK), nameof(NGXBufferInfoVK), nameof(NGXVkImageSubresourceRange)];
    private static readonly string[] OutputOnlyTypes = [nameof(NGXFeatureRequirement), nameof(NGXVkExtensionProperties)];

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
                Type managed = typeof(Ngx).Assembly.GetType("NGX.NET." + managedName, true)!;
                bool unused = UnusedTypes.Contains(managedName);
                Assert(mapper.HasNativeRecord(name) != unused, name + " native type use");

                if (unused)
                {
                    Assert(!nativeSources.Contains(managedName + "Native") && typeof(Ngx).Assembly.GetType("NGX.NET." + managedName + "Native") is null, name + " unused native type removed");

                    continue;
                }

                Assert(nativeSources.Contains(managedName + "Native"), name + " generated native source");
                Type type = typeof(Ngx).Assembly.GetType("NGX.NET." + managedName + "Native", true)!;
                Assert(type.IsNotPublic && !typeof(IDisposable).IsAssignableFrom(type), name + " native scope ownership");
                bool input = !OutputOnlyTypes.Contains(managedName);
                bool output = OutputTypes.Contains(managedName);
                Assert(mapper.HasInputConversion(name) == input && mapper.HasOutputConversion(name) == output, name + " conversion direction");
                Type[] arguments = mapper.Allocates(name) ? [managed.MakeByRefType(), typeof(NativeScope)] : [managed.MakeByRefType()];
                Assert((type.GetConstructor(arguments) is not null) == input, name + " input conversion constructor");
                ConstructorInfo? reverse = managed.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [type.MakeByRefType()], null);
                Assert((reverse is not null) == output, name + " output conversion constructor");
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

        Assert(nativeSources.Count == models.Records.Values.Count(static record => !record.IsOpaque) - UnusedTypes.Length, "Only documented unused native types are removed");

        Console.WriteLine($"INFO {nativeSources.Count} native types; {layouts} AST layout comparisons; {offsets} AST field offset comparisons.");

        Console.WriteLine("PASS all required native layouts match the four-target AST and conversions match their used directions");
    }

    private static bool ContainsReferences<T>()
    {
        return RuntimeHelpers.IsReferenceOrContainsReferences<T>();
    }
}
