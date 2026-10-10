namespace Generation;

internal static class SyntaxStyleChecks
{
    internal static void Run(string path, CompilationUnitSyntax syntax)
    {
        Require(!syntax.ContainsDiagnostics, $"Invalid C# syntax: {path}.");

        foreach (SyntaxNode node in syntax.DescendantNodes())
        {
            Require(node is not RelationalPatternSyntax, $"Use comparison operators instead of relational patterns: {path}:{node.GetLocation().GetLineSpan().StartLinePosition.Line + 1}.");
            Require(node is not LockStatementSyntax, $"Use a Lock.Scope declaration: {path}.");

            if (node is InitializerExpressionSyntax initializer && initializer.Ancestors().OfType<TypeDeclarationSyntax>().Any())
            {
                Require(initializer.OpenBraceToken.GetLocation().GetLineSpan().StartLinePosition.Character > 0, $"Unindented initializer: {path}:{initializer.GetLocation().GetLineSpan().StartLinePosition.Line + 1}.");
            }

            if (node is ConditionalExpressionSyntax conditional)
            {
                Require(conditional.WhenTrue is not ConditionalExpressionSyntax && conditional.WhenFalse is not ConditionalExpressionSyntax, $"Nested conditional expression: {path}.");
            }

            if (node is ParenthesizedLambdaExpressionSyntax { ParameterList.Parameters: [ParameterSyntax { Type: null, Modifiers.Count: 0, AttributeLists.Count: 0 }] })
            {
                Require(false, $"A single untyped lambda parameter does not need parentheses: {path}.");
            }

            if (node is not TypeDeclarationSyntax type)
            {
                continue;
            }

            int previousRank = -1;
            foreach (MemberDeclarationSyntax member in type.Members)
            {
                int rank = MemberRank(member);
                Require(rank >= previousRank, $"Member order: {path}:{member.GetLocation().GetLineSpan().StartLinePosition.Line + 1}.");
                previousRank = rank;
            }
        }
    }

    internal static int MemberRank(MemberDeclarationSyntax member)
    {
        SyntaxTokenList modifiers = member switch
        {
            BaseFieldDeclarationSyntax field => field.Modifiers,
            BaseMethodDeclarationSyntax method => method.Modifiers,
            BasePropertyDeclarationSyntax property => property.Modifiers,
            BaseTypeDeclarationSyntax type => type.Modifiers,
            _ => default
        };
        int visibility = modifiers switch
        {
            _ when modifiers.Any(SyntaxKind.PublicKeyword) => 0,
            _ when modifiers.Any(SyntaxKind.InternalKeyword) => 1,
            _ when modifiers.Any(SyntaxKind.ProtectedKeyword) => 2,
            _ => 4
        };

        return member switch
        {
            FieldDeclarationSyntax when modifiers.Any(SyntaxKind.ConstKeyword) => 0,
            MethodDeclarationSyntax method when method.AttributeLists.SelectMany(static list => list.Attributes).Any(static attribute => attribute.Name.ToString() is "LibraryImport" or "DllImport") => 1,
            FieldDeclarationSyntax when modifiers.Any(SyntaxKind.StaticKeyword) && modifiers.Any(SyntaxKind.ReadOnlyKeyword) => 2,
            FieldDeclarationSyntax when modifiers.Any(SyntaxKind.StaticKeyword) => 3,
            FieldDeclarationSyntax when modifiers.Any(SyntaxKind.ReadOnlyKeyword) => 4,
            FieldDeclarationSyntax when modifiers.Any(SyntaxKind.PublicKeyword) => 5,
            FieldDeclarationSyntax => 6,
            ConstructorDeclarationSyntax when modifiers.Any(SyntaxKind.StaticKeyword) => 7,
            ConstructorDeclarationSyntax or DestructorDeclarationSyntax => 8,
            PropertyDeclarationSyntax or IndexerDeclarationSyntax when modifiers.Any(SyntaxKind.StaticKeyword) => 9,
            PropertyDeclarationSyntax or IndexerDeclarationSyntax => 10,
            EventDeclarationSyntax or EventFieldDeclarationSyntax => 11,
            MethodDeclarationSyntax method when modifiers.Any(SyntaxKind.StaticKeyword) => 17 + visibility,
            MethodDeclarationSyntax { ExplicitInterfaceSpecifier: not null } => 15,
            MethodDeclarationSyntax => 12 + visibility,
            OperatorDeclarationSyntax or ConversionOperatorDeclarationSyntax => 22,
            _ => 23
        };
    }
}
