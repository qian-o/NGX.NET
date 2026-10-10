namespace Generation;

internal static class HandwrittenStyleChecks
{
    private static readonly string[] SourceDirectories = ["NGX.NET", "NGX.NET.Generator", "Showcase", "verification"];

    internal static void Run(string repository)
    {
        int count = 0;
        foreach (string folder in SourceDirectories)
        {
            foreach (string path in Directory.EnumerateFiles(Path.Combine(repository, folder), "*.cs", SearchOption.AllDirectories))
            {
                string relative = Path.GetRelativePath(repository, path);
                if (path.EndsWith(".g.cs", StringComparison.Ordinal) || relative.Split(Path.DirectorySeparatorChar).Any(static part => part is "obj" or "bin"))
                {
                    continue;
                }

                CompilationUnitSyntax syntax = CSharpSyntaxTree.ParseText(File.ReadAllText(path)).GetCompilationUnitRoot();
                SyntaxStyleChecks.Run(relative, syntax);

                if (folder is not "Showcase")
                {
                    ServiceNamingChecks.Run(relative, syntax);
                }

                count++;
            }
        }

        Console.WriteLine($"PASS {count} handwritten files: native declaration order, instance/static method order, comparison operators, scoped locking and service/state field names.");
    }
}
