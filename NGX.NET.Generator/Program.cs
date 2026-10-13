namespace NGX.NET.Generator;

internal static class Program
{
    private static readonly UTF8Encoding UTF8WithBom = new(true);
    private static readonly string[] OutputDirectories = ["API", "Enums", "Structs"];

    private static int Main(string[] args)
    {
        try
        {
            string root = args.Length > 0 ? Path.GetFullPath(args[0]) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            string input = Path.Combine(root, "NGX.NET.Generator", "ast.json");
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(input));
            GenerationPipeline emitter = new(AstReader.Read(document.RootElement));
            Dictionary<string, string> files = emitter.Generate();
            string output = Path.Combine(root, "NGX.NET");
            int changedFileCount = 0;
            foreach ((string name, string source) in files)
            {
                string path = Path.Combine(output, name);
                byte[] content = [.. UTF8WithBom.GetPreamble(), .. UTF8WithBom.GetBytes(source)];
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                if (!File.Exists(path) || !File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
                {
                    File.WriteAllBytes(path, content);
                    changedFileCount++;
                }
            }

            foreach (string directory in OutputDirectories)
            {
                string folder = Path.Combine(output, directory);
                if (!Directory.Exists(folder))
                {
                    continue;
                }

                foreach (string path in Directory.EnumerateFiles(folder, "*.g.cs", SearchOption.AllDirectories))
                {
                    string name = Path.GetRelativePath(output, path).Replace(Path.DirectorySeparatorChar, '/');
                    if (!files.ContainsKey(name))
                    {
                        File.Delete(path);
                        changedFileCount++;
                    }
                }
            }

            Console.WriteLine($"Generated {files.Count} files; {changedFileCount} changed.");

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);

            return 1;
        }
    }
}
