namespace Generation;

internal static class ResultTests
{
    internal static void Run(JsonElement ast, Dictionary<string, string> files, string root)
    {
        int count = CheckOverloads(files);
        CheckParsing(ast);
        CheckExecution(files, root);
        Console.WriteLine($"PASS {count} result overloads: complete coverage, parsing, shared types, collision diagnostics and managed success/failure execution.");
    }

    private static int CheckOverloads(Dictionary<string, string> files)
    {
        int count = 0;
        foreach ((string path, string source) in files.Where(static file => file.Key.StartsWith("API/", StringComparison.Ordinal)))
        {
            MethodDeclarationSyntax[] methods = Methods(source);
            foreach (MethodDeclarationSyntax original in methods.Where(static method => method.Modifiers.Any(SyntaxKind.PublicKeyword) && method.ReturnType.ToString() is "NGXResult"))
            {
                ParameterSyntax[] outputs = [.. original.ParameterList.Parameters.Where(static parameter => parameter.Modifiers.Any(SyntaxKind.OutKeyword))];
                if (outputs.Length is 0)
                {
                    continue;
                }

                string[] inputs = [.. original.ParameterList.Parameters.Except(outputs).Select(static parameter => parameter.ToString())];
                MethodDeclarationSyntax[] overloads = [.. methods.Where(method => method.Identifier.ValueText == original.Identifier.ValueText && method.ParameterList.Parameters.Select(static parameter => parameter.ToString()).SequenceEqual(inputs))];
                Require(overloads.Length is 1, $"Missing or ambiguous result overload: {path}::{original.Identifier}.");
                MethodDeclarationSyntax overload = overloads[0];
                if (outputs.Length is 1)
                {
                    Require(overload.ReturnType.ToString() == outputs[0].Type!.ToString(), $"Incorrect single-output type: {original.Identifier}.");
                }
                else
                {
                    StructDeclarationSyntax structure = CSharpSyntaxTree.ParseText(files[$"Structs/{overload.ReturnType}.g.cs"]).GetRoot().DescendantNodes().OfType<StructDeclarationSyntax>().Single();
                    Require(structure.Modifiers.Any(SyntaxKind.ReadOnlyKeyword), $"Mutable result: {structure.Identifier}.");
                    PropertyDeclarationSyntax[] properties = [.. structure.Members.OfType<PropertyDeclarationSyntax>()];
                    Require(properties.Select(static property => property.Type.ToString()).SequenceEqual(outputs.Select(static parameter => parameter.Type!.ToString())), $"Output order/types changed: {original.Identifier}.");
                    Require(properties.All(static property => property.AccessorList?.Accessors is [AccessorDeclarationSyntax accessor] && accessor.IsKind(SyntaxKind.GetAccessorDeclaration)), $"Writable result properties: {structure.Identifier}.");
                }

                InvocationExpressionSyntax[] calls = [.. overload.DescendantNodes().OfType<InvocationExpressionSyntax>()];
                Require(calls.Length is 1 && calls[0].Expression.ToString() == original.Identifier.ValueText, $"Result wrapper bypassed the original method: {original.Identifier}.");
                count++;
            }
        }

        return count;
    }

    private static void CheckParsing(JsonElement ast)
    {
        JsonNode template = JsonNode.Parse(ast.GetRawText())!["platforms"]!.AsObject().First().Value!;
        JsonObject platform = (JsonObject)template.DeepClone();
        JsonObject prototype = (JsonObject)platform["functions"]!.AsArray().Single(static function => function!["name"]!.GetValue<string>() is "NGX_DLSS_GET_OPTIMAL_SETTINGS")!.DeepClone();
        JsonArray parameters = prototype["parameters"]!.AsArray();
        JsonObject first = (JsonObject)parameters[4]!.DeepClone();
        JsonObject second = (JsonObject)parameters[10]!.DeepClone();
        first["name"] = "ppOutCustomWidth";
        second["name"] = "Out_FrameTime";
        // Interleaved outputs ensure removal does not shift input/optional parameter indices.
        prototype["parameters"] = new JsonArray(parameters[1]!.DeepClone(), first, parameters[2]!.DeepClone(), second);
        prototype["name"] = "NVSDK_NGX_D3D12_QueryFrameMetrics3";
        prototype["export"] = "TestQueryFrameMetrics3";
        JsonArray functions = [prototype];
        platform["functions"] = functions;
        JsonObject fixture = new()
        {
            ["platforms"] = new JsonObject { ["test"] = platform }
        };
        Dictionary<string, string> generated = Generate(fixture);
        Require(generated["Structs/FrameMetrics3.g.cs"].Contains("uint customWidth, float frameTime", StringComparison.Ordinal), "Result names or field order depend on known SDK names.");
        Require(generated["API/D3D12.g.cs"].Contains("public static FrameMetrics3 QueryFrameMetrics3(uint userSelectedWidth, uint userSelectedHeight)", StringComparison.Ordinal), "Interleaved output removal changed input order.");

        JsonObject shared = (JsonObject)prototype.DeepClone();
        shared["name"] = "NVSDK_NGX_VULKAN_GetFrameMetrics3";
        functions.Add(shared);
        generated = Generate(fixture);
        Require(generated.Keys.Count(static path => path.EndsWith("/FrameMetrics3.g.cs", StringComparison.Ordinal)) is 1, "Equivalent results were not shared across methods/backends.");

        shared["parameters"]![3]!["name"] = "pOutDifferent";
        Reject(fixture, "Conflicting result type FrameMetrics3");
        functions.Remove(shared);

        second["name"] = "OutCustomWidth";
        Reject(fixture, "Conflicting or invalid result properties");
        second["name"] = "Out_FrameTime";

        prototype["name"] = "NVSDK_NGX_D3D12_QueryNGXFeatureRequirement";
        Reject(fixture, "conflicts with an existing generated type");
        prototype["name"] = "NVSDK_NGX_D3D12_InspectMetrics";
        Require(Generate(fixture).ContainsKey("Structs/InspectMetricsResult.g.cs"), "Unknown action words have no deterministic fallback.");
        prototype["name"] = "NVSDK_NGX_D3D12_GetterMetrics";
        Require(Generate(fixture).ContainsKey("Structs/GetterMetricsResult.g.cs"), "Action removal ignored the PascalCase word boundary.");

        prototype["name"] = "NVSDK_NGX_D3D12_QueryFrameMetrics3";
        JsonObject collision = (JsonObject)prototype.DeepClone();
        collision["name"] = "NGX_D3D12_QUERY_FRAME_METRICS3";
        collision["parameters"] = new JsonArray(parameters[1]!.DeepClone(), parameters[2]!.DeepClone());
        functions.Add(collision);
        Reject(fixture, "Conflicting managed overload");
        functions.Remove(collision);
        prototype["result"] = parameters[1]!["type"]!.DeepClone();
        Require(!Generate(fixture).ContainsKey("Structs/FrameMetrics3.g.cs"), "A non-status return value was replaced by outputs.");
    }

    private static void CheckExecution(Dictionary<string, string> files, string root)
    {
        string scalar = Methods(files["API/Parameter.g.cs"]).Single(static method => method.Identifier.ValueText is "GetI" && method.ReturnType.ToString() is "int").ToString();
        string arrays = Methods(files["API/Vulkan.g.cs"]).Single(static method => method.Identifier.ValueText is "RequiredExtensions" && method.ReturnType.ToString() is "Extensions").ToString();
        string settings = Methods(files["API/DLSS.g.cs"]).Single(static method => method.Identifier.ValueText is "GetOptimalSettings" && method.ReturnType.ToString() is "OptimalSettings").ToString();
        string source = $$"""
            namespace NGX.NET;
            public static partial class Ngx
            {
                public static NGXResult TestResult;
                public static int Calls;
                public static class Parameter
                {
                    {{scalar}}
                    public static NGXResult GetI(NGXParameter parameters, string name, out int value)
                    {
                        Calls++;
                        value = checked((int)parameters.Value) + name.Length;
                        return TestResult;
                    }
                }
                public static class Vulkan
                {
                    {{arrays}}
                    public static NGXResult RequiredExtensions(out string[] instance, out string[] device)
                    {
                        Calls++;
                        instance = ["instance"];
                        device = ["device"];
                        return TestResult;
                    }
                }
                public static class DLSS
                {
                    {{settings}}
                    public static NGXResult GetOptimalSettings(NGXParameter parameters, uint width, uint height, NGXPerfQualityValue quality, out uint optimalWidth, out uint optimalHeight, out uint maxWidth, out uint maxHeight, out uint minWidth, out uint minHeight, out float sharpness)
                    {
                        Calls++;
                        optimalWidth = width;
                        optimalHeight = height;
                        maxWidth = width + 1;
                        maxHeight = height + 2;
                        minWidth = width - 3;
                        minHeight = height - 4;
                        sharpness = 0.25f;
                        return TestResult;
                    }
                }
            }
            public static class ResultProbe
            {
                public static void Run()
                {
                    NGXParameter parameters = new(12);
                    Ngx.TestResult = NGXResult.Success;
                    Require(Ngx.Parameter.GetI(parameters, "test") == 16, "Scalar forwarding");
                    Extensions extensions = Ngx.Vulkan.RequiredExtensions();
                    Require(extensions.InstanceExtensions[0] == "instance" && extensions.DeviceExtensions[0] == "device", "Array forwarding");
                    OptimalSettings settings = Ngx.DLSS.GetOptimalSettings(parameters, 1920, 1080, NGXPerfQualityValue.MaxQuality);
                    Require(settings.RenderOptimalWidth == 1920 && settings.RenderOptimalHeight == 1080 && settings.RenderMaxWidth == 1921 && settings.RenderMaxHeight == 1082 && settings.RenderMinWidth == 1917 && settings.RenderMinHeight == 1076 && settings.Sharpness == 0.25f, "Multiple-output order");
                    Require(Ngx.Calls == 3, "Underlying call duplicated");
                    // Only the explicit Success value permits returning outputs.
                    foreach (NGXResult result in new[] { NGXResult.Fail, NGXResult.FailInvalidParameter, (NGXResult)0, (NGXResult)2 })
                    {
                        Ngx.TestResult = result;
                        CheckFailure(() => Ngx.Parameter.GetI(parameters, "test"), "Ngx.Parameter.GetI");
                        CheckFailure(() => Ngx.Vulkan.RequiredExtensions(), "Ngx.Vulkan.RequiredExtensions");
                        CheckFailure(() => Ngx.DLSS.GetOptimalSettings(parameters, 1920, 1080, NGXPerfQualityValue.MaxQuality), "Ngx.DLSS.GetOptimalSettings");
                    }
                }

                private static void CheckFailure(Action call, string operation)
                {
                    int before = Ngx.Calls;
                    try
                    {
                        call();
                    }
                    catch (NGXException exception)
                    {
                        Require(exception.Result == Ngx.TestResult && exception.Message.StartsWith(operation + ":", StringComparison.Ordinal) && Ngx.Calls == before + 1, "Failure details/call count");

                        return;
                    }

                    throw new Exception("NGX failure did not throw");
                }

                private static void Require(bool condition, string message)
                {
                    if (!condition)
                    {
                        throw new Exception(message);
                    }
                }
            }
            """;
        List<SyntaxTree> trees = [CSharpSyntaxTree.ParseText("global using System; global using System.Runtime.CompilerServices; global using System.Runtime.InteropServices;"), CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "NGX.NET/NGXException.cs")))];
        foreach (string name in new[] { "NGXParameter", "NGXResult", "NGXPerfQualityValue", "Extensions", "OptimalSettings" })
        {
            string directory = name is "NGXResult" or "NGXPerfQualityValue" ? "Enums" : "Structs";

            trees.Add(CSharpSyntaxTree.ParseText(files[$"{directory}/{name}.g.cs"]));
        }

        string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        CSharpCompilation compilation = CSharpCompilation.Create("ResultOverloadProbe", trees, assemblies.Select(static path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
        Require(result.Success, string.Join('\n', result.Diagnostics));
        stream.Position = 0;
        AssemblyLoadContext context = new("ResultOverloadProbe", isCollectible: true);
        try
        {
            Assembly assembly = context.LoadFromStream(stream);
            assembly.GetType("NGX.NET.ResultProbe", throwOnError: true)!.GetMethod("Run")!.Invoke(null, null);
        }
        finally
        {
            context.Unload();
        }
    }

    private static Dictionary<string, string> Generate(JsonObject fixture)
    {
        using JsonDocument ast = JsonDocument.Parse(fixture.ToJsonString());

        return new GenerationPipeline(AstReader.Read(ast.RootElement)).Generate();
    }

    private static void Reject(JsonObject fixture, string expected)
    {
        try
        {
            Generate(fixture);
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains(expected, StringComparison.Ordinal))
        {
            return;
        }

        throw new InvalidOperationException("Expected generation diagnostic: " + expected);
    }

    private static MethodDeclarationSyntax[] Methods(string source)
    {
        return [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()];
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
