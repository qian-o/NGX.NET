namespace Generation;

internal static class Program
{
    private static void Main(string[] args)
    {
        string root = Path.GetFullPath(args[0]);
        string repository = args.Length > 1 ? Path.GetFullPath(args[1]) : root;
        using JsonDocument ast = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "NGX.NET.Generator/ast.json")));
        Dictionary<string, string> files = GenerationChecks.Run(ast.RootElement, root);

        foreach ((string path, string source) in files)
        {
            SourceStyleChecks.Run(path, source);
        }

        ResultTests.Run(ast.RootElement, files, repository);
        IdentityChecks.Run(files, repository);
        HandwrittenStyleChecks.Run(repository);
        Console.WriteLine($"PASS {files.Count} direct emitter outputs: syntax, member order and whitespace.");
    }
}
