namespace Generation;

internal static class IdentityChecks
{
    internal static void Run(Dictionary<string, string> files, string repository)
    {
        List<SyntaxTree> trees = [];
        List<SyntaxTree> generated = [];
        NativeDeclarations declarations = new();

        foreach ((string path, string source) in files)
        {
            SyntaxTree tree = CSharpSyntaxTree.ParseText(source, path: path);
            CompilationUnitSyntax syntax = (CompilationUnitSyntax)declarations.Visit(tree.GetRoot())!;
            SyntaxTree rewritten = CSharpSyntaxTree.Create(syntax, path: path);
            trees.Add(rewritten);
            generated.Add(rewritten);
        }

        foreach (string path in Directory.EnumerateFiles(Path.Combine(repository, "NGX.NET"), "*.cs", SearchOption.AllDirectories))
        {
            string relative = Path.GetRelativePath(repository, path);
            if (path.EndsWith(".g.cs", StringComparison.Ordinal) || relative.Split(Path.DirectorySeparatorChar).Any(static part => part is "obj" or "bin"))
            {
                continue;
            }

            CompilationUnitSyntax syntax = (CompilationUnitSyntax)declarations.Visit(CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetRoot())!;
            trees.Add(CSharpSyntaxTree.Create(syntax, path: path));
        }

        trees.Add(CSharpSyntaxTree.ParseText("global using System; global using System.Collections.Generic; global using System.IO; global using System.Linq; global using System.Threading; global using System.Threading.Tasks;"));
        string[] assemblies = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        CSharpCompilation compilation = CSharpCompilation.Create("GeneratedConversionChecks", trees, assemblies.Select(static path => MetadataReference.CreateFromFile(path)), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true, nullableContextOptions: NullableContextOptions.Enable));
        Diagnostic[] errors = [.. compilation.GetDiagnostics().Where(static diagnostic => diagnostic.Severity is DiagnosticSeverity.Error)];
        if (errors.Length is not 0)
        {
            throw new InvalidOperationException(string.Join('\n', errors.Select(static diagnostic => diagnostic.ToString())));
        }

        int checkedCount = 0;
        foreach (SyntaxTree tree in generated)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (CastExpressionSyntax cast in tree.GetRoot().DescendantNodes().OfType<CastExpressionSyntax>())
            {
                ITypeSymbol target = model.GetTypeInfo(cast.Type).Type!;
                if (model.ClassifyConversion(cast.Expression, target, isExplicitInSource: true).IsIdentity)
                {
                    FileLinePositionSpan location = cast.GetLocation().GetLineSpan();

                    throw new InvalidOperationException($"Identity conversion: {tree.FilePath}:{location.StartLinePosition.Line + 1}: {cast}.");
                }

                checkedCount++;
            }
        }

        OutputAssignmentChecks.Run(compilation, generated);
        DlssAllocationChecks.Run(compilation, generated);
        Console.WriteLine($"PASS {checkedCount} generated casts: 0 identity conversions; semantic compilation has no errors.");
    }

    private class NativeDeclarations : CSharpSyntaxRewriter
    {
        public override SyntaxNode? VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            if (!node.Modifiers.Any(SyntaxKind.PartialKeyword) || !node.AttributeLists.SelectMany(static list => list.Attributes).Any(static attribute => attribute.Name.ToString() is "LibraryImport"))
            {
                return base.VisitMethodDeclaration(node);
            }

            return node.WithModifiers(SyntaxFactory.TokenList(node.Modifiers.Where(static token => !token.IsKind(SyntaxKind.PartialKeyword)))).WithBody(SyntaxFactory.Block(SyntaxFactory.ParseStatement("throw new InvalidOperationException();"))).WithSemicolonToken(default);
        }
    }
}
