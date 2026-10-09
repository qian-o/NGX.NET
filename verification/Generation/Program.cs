using System.Text;
using System.Text.Json;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NGX.NET.Generator;

namespace Generation;

internal static class Program
{
    private static readonly UTF8Encoding UTF8 = new(true);

    private static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        Dictionary<string, string> first = new Emitter(ast.RootElement).Generate();
        Dictionary<string, string> second = new Emitter(ast.RootElement).Generate();
        Require(first.Count == second.Count, "Generation changed the number of files.");

        foreach ((string path, string source) in first)
        {
            Require(second.TryGetValue(path, out string? repeated) && repeated == source, $"Non-deterministic source: {path}.");
            byte[] expected = [.. UTF8.GetPreamble(), .. UTF8.GetBytes(source)];
            Require(File.ReadAllBytes(Path.Combine(root, "NGX.NET", path)).AsSpan().SequenceEqual(expected), $"The checked-in file differs from direct emitter output: {path}.");
            CheckSource(path, source);
        }

        string repository = args.Length > 1 ? Path.GetFullPath(args[1]) : root;
        int handwrittenCount = 0;
        string[] folders = ["NGX.NET", "NGX.NET.Generator", "Showcase", "verification"];
        foreach (string folder in folders)
        {
            foreach (string path in Directory.EnumerateFiles(Path.Combine(repository, folder), "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(repository, path);
                if (path.EndsWith(".g.cs", StringComparison.Ordinal) || relative.Split(Path.DirectorySeparatorChar).Any(static part => part is "obj" or "bin"))
                {
                    continue;
                }

                CheckRules(relative, CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot());
                handwrittenCount++;
            }
        }

        Console.WriteLine($"PASS {handwrittenCount} handwritten files: native declaration order, instance/static method order, comparison operators and scoped locking.");
        Console.WriteLine($"PASS {first.Count} direct emitter outputs: syntax, braces, explicit types, member order, whitespace, BOM, deterministic bytes and checked-in parity.");
    }

    private static void CheckSource(string path, string source)
    {
        Require(source.EndsWith('\n') && !source.EndsWith("\n\n", StringComparison.Ordinal), $"Final newline: {path}.");
        Require(!source.Contains('\r') && !source.Contains('\t') && !source.Contains("\n\n\n", StringComparison.Ordinal), $"Whitespace: {path}.");
        string[] lines = source.Split('\n');
        foreach (string line in lines)
        {
            Require(line.TrimEnd() == line, $"Trailing whitespace: {path}.");
            Require((line.Length - line.TrimStart().Length) % 4 is 0, $"Indentation: {path}.");
        }

        CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(source).GetCompilationUnitRoot();
        Require(!syntax.ContainsDiagnostics, $"Invalid C# syntax: {path}.");
        Require(syntax.Members is [FileScopedNamespaceDeclarationSyntax], $"File-scoped namespace: {path}.");
        FileScopedNamespaceDeclarationSyntax fileNamespace = (FileScopedNamespaceDeclarationSyntax)syntax.Members[0];
        Require(fileNamespace.Members.Count > 0, $"Missing main type: {path}.");
        foreach (MemberDeclarationSyntax helper in fileNamespace.Members.Skip(1))
        {
            Require(helper is TypeDeclarationSyntax type && (type.Modifiers.Any(SyntaxKind.InternalKeyword) || type.Modifiers.Any(SyntaxKind.FileKeyword)), $"Invalid auxiliary type: {path}.");
        }

        CheckRules(path, syntax);

        foreach (SyntaxNode node in syntax.DescendantNodes())
        {
            Require(node is not RecordDeclarationSyntax, $"Record declaration: {path}.");

            if (node is VariableDeclarationSyntax declaration)
            {
                Require(!declaration.Type.IsVar, $"Implicit variable type: {path}.");
                Require(declaration.Variables.Count is 1 || declaration.Parent is ForStatementSyntax, $"Multiple variable declarations: {path}.");
            }

            if (node is MethodDeclarationSyntax method)
            {
                Require(method.ExpressionBody is null, $"Expression-bodied method: {path}.");
            }

            if (node is BlockSyntax block)
            {
                CheckBrace(path, lines, block.OpenBraceToken);
                CheckBrace(path, lines, block.CloseBraceToken);
                CheckStatements(path, lines, block);
            }

            if (node is EnumDeclarationSyntax enumeration)
            {
                CheckBrace(path, lines, enumeration.OpenBraceToken);
                CheckBrace(path, lines, enumeration.CloseBraceToken);

                for (int i = 1; i < enumeration.Members.Count; i++)
                {
                    int previousLine = enumeration.Members[i - 1].GetLastToken().GetLocation().GetLineSpan().EndLinePosition.Line;
                    Require(lines[previousLine + 1].Length is 0, $"Enum member spacing: {path}:{previousLine + 1}.");
                }

                if (enumeration.AttributeLists.SelectMany(static list => list.Attributes).Any(static attribute => attribute.Name.ToString() is "Flags"))
                {
                    Require(enumeration.Members.Any(static member => member.Identifier.ValueText is "None" && member.EqualsValue?.Value is LiteralExpressionSyntax { Token.ValueText: "0" }), $"Flags without None: {path}.");
                }
            }

            if (node is TypeDeclarationSyntax type)
            {
                Require(!type.Modifiers.Any(SyntaxKind.SealedKeyword), $"Sealed type: {path}.");
                CheckBrace(path, lines, type.OpenBraceToken);
                CheckBrace(path, lines, type.CloseBraceToken);
                CheckMembers(path, lines, type.Members);
            }

            StatementSyntax? embedded = node switch
            {
                IfStatementSyntax value => value.Statement,
                ElseClauseSyntax { Statement: not IfStatementSyntax } value => value.Statement,
                ForStatementSyntax value => value.Statement,
                ForEachStatementSyntax value => value.Statement,
                WhileStatementSyntax value => value.Statement,
                DoStatementSyntax value => value.Statement,
                FixedStatementSyntax value => value.Statement,
                LockStatementSyntax value => value.Statement,
                UsingStatementSyntax value => value.Statement,
                _ => null
            };
            Require(embedded is null or BlockSyntax, $"Unbraced control statement: {path}.");
        }
    }

    private static void CheckBrace(string path, string[] lines, SyntaxToken token)
    {
        int line = token.GetLocation().GetLineSpan().StartLinePosition.Line;
        Require(lines[line].Trim() is "{" or "}" or "};", $"Allman brace at {path}:{line + 1}.");

        if (token.IsKind(SyntaxKind.OpenBraceToken))
        {
            Require(line + 1 < lines.Length && lines[line + 1].Length is not 0, $"Blank line after opening brace: {path}:{line + 1}.");
        }
        else
        {
            Require(line > 0 && lines[line - 1].Length is not 0, $"Blank line before closing brace: {path}:{line + 1}.");
        }
    }

    private static void CheckStatements(string path, string[] lines, BlockSyntax block)
    {
        for (int i = 1; i < block.Statements.Count; i++)
        {
            StatementSyntax statement = block.Statements[i];
            if (statement is ReturnStatementSyntax or BreakStatementSyntax or ContinueStatementSyntax)
            {
                int line = statement.GetLocation().GetLineSpan().StartLinePosition.Line;
                Require(lines[line - 1].Length is 0, $"Missing blank line before exit: {path}:{line + 1}.");
            }
        }
    }

    private static void CheckMembers(string path, string[] lines, SyntaxList<MemberDeclarationSyntax> members)
    {
        int previousRank = -1;
        for (int i = 0; i < members.Count; i++)
        {
            MemberDeclarationSyntax member = members[i];
            int rank = MemberRank(member);
            Require(rank >= previousRank, $"Member order: {path}.");
            previousRank = rank;

            if (i is 0)
            {
                continue;
            }

            int previousLine = members[i - 1].GetLastToken().GetLocation().GetLineSpan().EndLinePosition.Line;
            Require(lines[previousLine + 1].Length is 0, $"Member spacing: {path}:{previousLine + 1}.");
        }
    }

    private static void CheckRules(string path, CompilationUnitSyntax syntax)
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

    private static int MemberRank(MemberDeclarationSyntax member)
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

    private static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
