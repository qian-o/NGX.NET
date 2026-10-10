namespace Generation;

internal static class ResultTests
{
    internal static void Run(JsonElement ast, Dictionary<string, string> files, string root)
    {
        int count = CheckOverloads(files);
        CheckParsing(ast);
        CheckExecution(files, root);
        Console.WriteLine($"PASS {count} single-output result overloads: complete out coverage, forwarding, collisions, SDK-mask success and failed partial-output reset.");
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
                string[] inputs = [.. original.ParameterList.Parameters.Except(outputs).Select(static parameter => parameter.ToString())];
                MethodDeclarationSyntax[] overloads = [.. methods.Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword) && method.ReturnType.ToString() is not "NGXResult" && method.Identifier.ValueText == original.Identifier.ValueText && method.ParameterList.Parameters.Select(static parameter => parameter.ToString()).SequenceEqual(inputs))];
                Require(overloads.Length == (outputs.Length is 1 ? 1 : 0), $"Incorrect value-returning overload count: {path}::{original.Identifier}.");

                if (outputs.Length is not 1)
                {
                    continue;
                }

                MethodDeclarationSyntax overload = overloads[0];
                Require(overload.ReturnType.ToString() == outputs[0].Type!.ToString(), $"Incorrect single-output type: {original.Identifier}.");
                InvocationExpressionSyntax[] calls = [.. overload.DescendantNodes().OfType<InvocationExpressionSyntax>()];
                Require(calls.Length is 2 && calls.Count(call => call.Expression is IdentifierNameSyntax name && name.Identifier.ValueText == original.Identifier.ValueText) is 1 && calls.Count(static call => call.Expression is MemberAccessExpressionSyntax { Name.Identifier.ValueText: "CheckError" }) is 1, $"Value wrapper must forward once and check its result: {original.Identifier}.");
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
        MethodDeclarationSyntax[] methods = PublicMethods(generated["API/D3D12.g.cs"], "QueryFrameMetrics3");
        Require(methods.Length is 1 && methods[0].ParameterList.Parameters.Select(static parameter => parameter.Identifier.ValueText).SequenceEqual(["userSelectedWidth", "customWidth", "userSelectedHeight", "frameTime"]), "Interleaved multi-output parameters changed order or gained a value wrapper.");
        Require(methods[0].ReturnType.ToString() is "NGXResult" && methods[0].ParameterList.Parameters.Count(static parameter => parameter.Modifiers.Any(SyntaxKind.OutKeyword)) is 2, "Multi-output functions must retain their complete status/out signature.");
        Require(generated.Keys.Where(static path => path.StartsWith("Structs/", StringComparison.Ordinal)).All(static path => Path.GetFileName(path).StartsWith("NGX", StringComparison.Ordinal)), "Generation introduced an aggregate result type.");

        prototype["parameters"] = new JsonArray(parameters[1]!.DeepClone(), first.DeepClone(), parameters[2]!.DeepClone());
        prototype["name"] = "NVSDK_NGX_D3D12_QueryCustomWidth";
        generated = Generate(fixture);
        methods = PublicMethods(generated["API/D3D12.g.cs"], "QueryCustomWidth");
        Require(methods.Length is 2 && methods.Single(static method => method.ReturnType.ToString() is "uint").ParameterList.Parameters.Select(static parameter => parameter.Identifier.ValueText).SequenceEqual(["userSelectedWidth", "userSelectedHeight"]), "Single-output wrapper removal shifted interleaved inputs.");
        JsonObject collision = (JsonObject)prototype.DeepClone();
        collision["name"] = "NGX_D3D12_QUERY_CUSTOM_WIDTH";
        collision["parameters"] = new JsonArray(parameters[1]!.DeepClone(), parameters[2]!.DeepClone());
        functions.Add(collision);
        Reject(fixture, "Conflicting managed overload");
        functions.Remove(collision);
        prototype["result"] = parameters[1]!["type"]!.DeepClone();
        Require(PublicMethods(Generate(fixture)["API/D3D12.g.cs"], "QueryCustomWidth").Length is 1, "A non-status return value gained a convenience wrapper.");
    }

    private static void CheckExecution(Dictionary<string, string> files, string root)
    {
        MethodDeclarationSyntax[] parameterMethods = Methods(files["API/Parameter.g.cs"]);
        string scalar = parameterMethods.Single(static method => method.Identifier.ValueText is "GetI" && method.ReturnType.ToString() is "int").ToString();
        string scalarOut = parameterMethods.Single(static method => method.Identifier.ValueText is "GetI" && method.ReturnType.ToString() is "NGXResult").ToString();
        MethodDeclarationSyntax scalarNative = parameterMethods.Single(static method => method.Identifier.ValueText is "GetINative");
        string parameter = scalarNative.ParameterList.Parameters[0].Identifier.ValueText;
        string name = scalarNative.ParameterList.Parameters[1].Identifier.ValueText;
        string value = scalarNative.ParameterList.Parameters[2].Identifier.ValueText;
        string scalarStub = Stub(scalarNative, $"Calls++; {value} = checked((int){parameter}.Value) + {name}.Length; return TestResult;");
        MethodDeclarationSyntax[] dlssMethods = Methods(files["API/DLSS.g.cs"]);
        string statsOut = dlssMethods.Single(static method => method.Identifier.ValueText is "GetStats1" && method.ReturnType.ToString() is "NGXResult").ToString();
        MethodDeclarationSyntax statsNative = dlssMethods.Single(static method => method.Identifier.ValueText is "GetStats1Native");
        string statsStub = Stub(statsNative, $"Calls++; {statsNative.ParameterList.Parameters[1].Identifier.ValueText} = 123UL; {statsNative.ParameterList.Parameters[2].Identifier.ValueText} = 9u; return TestResult;");
        MethodDeclarationSyntax[] vulkanMethods = Methods(files["API/Vulkan.g.cs"]);
        string arraysOut = vulkanMethods.Single(static method => method.Identifier.ValueText is "RequiredExtensions" && method.ReturnType.ToString() is "NGXResult").ToString();
        MethodDeclarationSyntax arraysNative = vulkanMethods.Single(static method => method.Identifier.ValueText is "RequiredExtensionsNative");
        string arraysStub = Stub(arraysNative, $"Calls++; {arraysNative.ParameterList.Parameters[0].Identifier.ValueText} = TestResult.IsSuccess ? 0u : 1u; {arraysNative.ParameterList.Parameters[1].Identifier.ValueText} = (byte**)1; {arraysNative.ParameterList.Parameters[2].Identifier.ValueText} = TestResult.IsSuccess ? 0u : 1u; {arraysNative.ParameterList.Parameters[3].Identifier.ValueText} = (byte**)1; return TestResult;");
        string source = $$"""
            namespace NGX.NET;
            public static unsafe partial class Ngx
            {
                public static NGXResult TestResult;
                public static int Calls;
                public static class Parameter
                {
                    {{scalar}}
                    {{scalarOut}}
                    {{scalarStub}}
                }
                public static class DLSS
                {
                    {{statsOut}}
                    {{statsStub}}
                }
                public static class Vulkan
                {
                    {{arraysOut}}
                    {{arraysStub}}
                }
            }
            public static class ResultProbe
            {
                public static void Run()
                {
                    NGXParameter parameters = new(12);
                    NGXResult[] successes = [NGXResult.Success, (NGXResult)0, (NGXResult)2, (NGXResult)0xBAE00000u];
                    foreach (NGXResult result in successes)
                    {
                        Ngx.TestResult = result;
                        int before = Ngx.Calls;
                        Require(Ngx.Parameter.GetI(parameters, "test", out int value) == result && value is 16, "Successful scalar output forwarding");
                        Require(Ngx.Parameter.GetI(parameters, "test") is 16, "Successful value wrapper forwarding");
                        Require(Ngx.DLSS.GetStats1(parameters, out ulong bytes, out uint level) == result && bytes is 123UL && level is 9u, "Successful multiple-output forwarding");
                        Require(Ngx.Vulkan.RequiredExtensions(out string[] instance, out string[] device) == result && instance.Length is 0 && device.Length is 0, "Successful array output conversion");
                        Require(Ngx.Calls == before + 4, "Underlying calls duplicated");
                    }

                    NGXResult[] failures = [NGXResult.Fail, NGXResult.FailInvalidParameter, (NGXResult)0xBADABCDEu];
                    foreach (NGXResult result in failures)
                    {
                        Ngx.TestResult = result;
                        Require(Ngx.Parameter.GetI(parameters, "test", out int value) == result && value is 0, "Failed scalar partial output was not reset");
                        Require(Ngx.DLSS.GetStats1(parameters, out ulong bytes, out uint level) == result && bytes is 0UL && level is 0u, "Failed multiple partial outputs were not reset");
                        Require(Ngx.Vulkan.RequiredExtensions(out string[] instance, out string[] device) == result && instance.Length is 0 && device.Length is 0, "Failed native array partial writes were consumed");
                        CheckFailure(() => Ngx.Parameter.GetI(parameters, "test"), "Ngx.Parameter.GetI");
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
                        Require(exception.Result == Ngx.TestResult && exception.Message.StartsWith($"{operation}:", StringComparison.Ordinal) && Ngx.Calls == before + 1, "Failure details or call count changed");

                        return;
                    }

                    throw new Exception("NGX failure did not throw.");
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
        List<SyntaxTree> trees = [CSharpSyntaxTree.ParseText("global using System; global using System.Runtime.CompilerServices; global using System.Runtime.InteropServices;"), CSharpSyntaxTree.ParseText(source), CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "NGX.NET/NGXException.cs"))), CSharpSyntaxTree.ParseText(File.ReadAllText(Path.Combine(root, "NGX.NET/Extensions.cs"))), CSharpSyntaxTree.ParseText(files["Structs/NGXParameter.g.cs"]), CSharpSyntaxTree.ParseText(files["Enums/NGXResult.g.cs"])];
        string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        CSharpCompilation compilation = CSharpCompilation.Create("ResultOverloadProbe", trees, assemblies.Select(static path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
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

    private static string Stub(MethodDeclarationSyntax method, string statements)
    {
        MethodDeclarationSyntax stub = method.WithAttributeLists(default).WithModifiers(SyntaxFactory.TokenList(method.Modifiers.Where(static modifier => !modifier.IsKind(SyntaxKind.PartialKeyword)))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement($"{{{statements}}}"))).WithSemicolonToken(default);

        return stub.ToString();
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

        throw new InvalidOperationException($"Expected generation diagnostic: {expected}.");
    }

    private static MethodDeclarationSyntax[] PublicMethods(string source, string name)
    {
        return [.. Methods(source).Where(method => method.Modifiers.Any(SyntaxKind.PublicKeyword) && method.Identifier.ValueText == name)];
    }

    private static MethodDeclarationSyntax[] Methods(string source)
    {
        return [.. CSharpSyntaxTree.ParseText(source).GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>()];
    }
}
