namespace NGX.NET.Generator;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            bool verify = args.Contains("--verify", StringComparer.Ordinal);
            string? directoryArgument = args.FirstOrDefault(static argument => !argument.StartsWith("--", StringComparison.Ordinal));
            string root = directoryArgument is not null ? Path.GetFullPath(directoryArgument) : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../"));
            string input = Path.Combine(root, "NGX.NET.Generator", "ast.json");
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(input));
            GenerationPipeline emitter = new(AstReader.Read(document.RootElement));
            Dictionary<string, string> files = emitter.Generate();
            string output = Path.Combine(root, "NGX.NET");
            int changedFileCount = GeneratedFileWriter.Synchronize(output, files, verify);

            Console.WriteLine(verify ? $"Verified {files.Count} generated files." : $"Generated {files.Count} files; {changedFileCount} changed.");

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);

            return 1;
        }
    }
}
