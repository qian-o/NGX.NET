namespace Generation;

internal static class DlssAllocationChecks
{
    internal static void Run(CSharpCompilation compilation, IReadOnlyCollection<SyntaxTree> generated)
    {
        HashSet<IMethodSymbol> visited = new(SymbolEqualityComparer.Default);
        foreach (string path in new[] { "API/D3D11.g.cs", "API/D3D12.g.cs" })
        {
            SyntaxTree tree = generated.Single(tree => tree.FilePath == path);
            SemanticModel model = compilation.GetSemanticModel(tree);
            MethodDeclarationSyntax method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single(static method => method.Identifier.ValueText is "EvaluateDLSSExt" && method.Modifiers.Any(SyntaxKind.PublicKeyword) && method.ReturnType.ToString() is "NGXResult");
            Visit(compilation, generated, (IMethodSymbol)model.GetDeclaredSymbol(method)!, visited);
        }

        Require(visited.Count is 10, $"DLSS call graph coverage changed: {visited.Count} generated methods/constructors.");
        Console.WriteLine($"PASS D3D11/D3D12 DLSS call graphs: {visited.Count} generated methods/constructors have no managed or native allocation before SDK entry.");
    }

    private static void Visit(CSharpCompilation compilation, IReadOnlyCollection<SyntaxTree> generated, IMethodSymbol method, HashSet<IMethodSymbol> visited)
    {
        if (!visited.Add(method))
        {
            return;
        }

        foreach (SyntaxReference reference in method.DeclaringSyntaxReferences)
        {
            SyntaxNode declaration = reference.GetSyntax();
            SemanticModel model = compilation.GetSemanticModel(reference.SyntaxTree);

            foreach (SyntaxNode node in declaration.DescendantNodes())
            {
                if (node is ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax or AnonymousObjectCreationExpressionSyntax or LambdaExpressionSyntax or AnonymousMethodExpressionSyntax)
                {
                    throw new InvalidOperationException($"DLSS conversion allocates managed storage: {method.Name}: {node}.");
                }

                if (node is BaseObjectCreationExpressionSyntax creation)
                {
                    ITypeSymbol type = model.GetTypeInfo(creation).Type!;
                    if (creation.Ancestors().Any(static ancestor => ancestor is ThrowStatementSyntax or ThrowExpressionSyntax))
                    {
                        continue;
                    }

                    Require(type.IsValueType, $"DLSS conversion creates a managed object: {method.Name}: {creation}.");
                    IMethodSymbol constructor = (IMethodSymbol)model.GetSymbolInfo(creation).Symbol!;
                    Visit(compilation, generated, constructor, visited);
                }

                if (node is not InvocationExpressionSyntax invocation || invocation.Expression is IdentifierNameSyntax { Identifier.ValueText: "nameof" })
                {
                    continue;
                }

                IMethodSymbol target = (IMethodSymbol)model.GetSymbolInfo(invocation).Symbol!;
                if (target.GetAttributes().Any(static attribute => attribute.AttributeClass?.Name is "LibraryImportAttribute") || (target.ContainingType.ToDisplayString() is "System.ArgumentNullException" && target.Name is "ThrowIfNull"))
                {
                    continue;
                }

                Require(target.DeclaringSyntaxReferences.Length > 0 && target.DeclaringSyntaxReferences.All(reference => generated.Contains(reference.SyntaxTree)), $"DLSS conversion calls an allocation-capable external helper: {target.ToDisplayString()}.");
                Visit(compilation, generated, target, visited);
            }
        }
    }
}
