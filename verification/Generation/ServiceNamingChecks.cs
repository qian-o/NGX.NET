namespace Generation;

internal static class ServiceNamingChecks
{
    internal static void Run(string path, CompilationUnitSyntax syntax)
    {
        foreach (FieldDeclarationSyntax field in syntax.DescendantNodes().OfType<FieldDeclarationSyntax>())
        {
            if (!field.Modifiers.Any(SyntaxKind.PrivateKeyword) || !field.Modifiers.Any(SyntaxKind.StaticKeyword) || !field.Modifiers.Any(SyntaxKind.ReadOnlyKeyword))
            {
                continue;
            }

            string type = field.Declaration.Type.ToString();
            bool service = type.EndsWith("Encoding", StringComparison.Ordinal) || type is "Regex" or "Lock";
            bool collection = field.Declaration.Type is GenericNameSyntax generic && generic.Identifier.ValueText is "Dictionary" or "HashSet" or "List" or "Queue" or "Stack";

            foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
            {
                bool empty = variable.Initializer?.Value is CollectionExpressionSyntax { Elements.Count: 0 } or ObjectCreationExpressionSyntax { Initializer: null } or ImplicitObjectCreationExpressionSyntax { Initializer: null };
                if (service || (collection && empty))
                {
                    Require(char.IsLower(variable.Identifier.ValueText[0]), $"Service or state field must use camelCase: {path}::{variable.Identifier.ValueText}.");
                }
            }
        }
    }
}
