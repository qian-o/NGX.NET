using System.Text.Json;

namespace Streamline.NET.Generator;

internal static class AstJsonParser
{
    public static InterfaceSnapshot Parse(string path)
    {
        using FileStream stream = File.OpenRead(path);
        InterfaceSnapshot snapshot = JsonSerializer.Deserialize<InterfaceSnapshot>(stream, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("The interface snapshot is empty.");

        if (snapshot.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported snapshot schema {snapshot.SchemaVersion}.");
        }

        if (snapshot.Source.Commit.Length != 40 || snapshot.Source.Release.Length == 0 || snapshot.Declarations.Count == 0)
        {
            throw new InvalidDataException("The snapshot is missing its source identity or declarations.");
        }

        HashSet<string> identities = new(StringComparer.Ordinal);

        foreach (NativeDeclaration declaration in snapshot.Declarations)
        {
            if (!identities.Add(declaration.Id))
            {
                throw new InvalidDataException($"Duplicate declaration identity: {declaration.Id}");
            }

            if (declaration.Classification is not ("application" or "dependency" or "test" or "plugin-template" or "implementation"))
            {
                throw new InvalidDataException($"Unclassified declaration: {declaration.QualifiedName}");
            }
        }

        OverloadContracts.Apply(snapshot);

        return snapshot;
    }
}
