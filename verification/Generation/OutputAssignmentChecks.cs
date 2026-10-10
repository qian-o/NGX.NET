namespace Generation;

internal static class OutputAssignmentChecks
{
    internal static void Run(CSharpCompilation compilation, IEnumerable<SyntaxTree> generated)
    {
        int count = 0;
        foreach (SyntaxTree tree in generated)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (MethodDeclarationSyntax method in tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Where(static method => method.Modifiers.Any(SyntaxKind.PublicKeyword)))
            {
                IParameterSymbol[] outputs = [.. model.GetDeclaredSymbol(method)!.Parameters.Where(static parameter => parameter.RefKind is RefKind.Out)];
                if (outputs.Length is 0)
                {
                    continue;
                }

                InvocationExpressionSyntax? nativeCall = method.DescendantNodes().OfType<InvocationExpressionSyntax>().FirstOrDefault(call => model.GetSymbolInfo(call).Symbol is IMethodSymbol target && target.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name is "LibraryImportAttribute"));
                if (nativeCall is null)
                {
                    continue;
                }

                foreach (AssignmentExpressionSyntax assignment in method.DescendantNodes().OfType<AssignmentExpressionSyntax>().Where(assignment => assignment.SpanStart < nativeCall.SpanStart))
                {
                    foreach (IdentifierNameSyntax target in assignment.Left.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>())
                    {
                        ISymbol? symbol = model.GetSymbolInfo(target).Symbol;
                        Require(!outputs.Any(output => SymbolEqualityComparer.Default.Equals(output, symbol)), $"Output assigned before native call: {tree.FilePath}::{method.Identifier}::{target.Identifier}.");
                    }
                }

                count++;
            }
        }

        Require(count > 0, "No generated native calls with public output parameters were checked.");
        Console.WriteLine($"PASS {count} generated public methods: no output assignments before native calls.");
    }
}
