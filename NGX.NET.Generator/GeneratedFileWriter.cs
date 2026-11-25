namespace NGX.NET.Generator;

internal static class GeneratedFileWriter
{
    private static readonly UTF8Encoding UTF8WithBom = new(true);
    private static readonly string[] OutputDirectories = ["API", "Enums", "Structs"];

    internal static int Synchronize(string output, IReadOnlyDictionary<string, string> files, bool verify)
    {
        int changedFileCount = 0;
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
                if (files.ContainsKey(name))
                {
                    continue;
                }

                if (verify)
                {
                    throw new InvalidOperationException($"Unexpected generated file: {name}.");
                }

                File.Delete(path);
                changedFileCount++;
            }
        }

        foreach ((string name, string source) in files)
        {
            string path = Path.Combine(output, name.Replace('/', Path.DirectorySeparatorChar));
            byte[] content = [.. UTF8WithBom.GetPreamble(), .. UTF8WithBom.GetBytes(source)];
            if (File.Exists(path) && File.ReadAllBytes(path).AsSpan().SequenceEqual(content))
            {
                continue;
            }

            if (verify)
            {
                throw new InvalidOperationException($"Generated file is stale: {name}.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, content);
            changedFileCount++;
        }

        return changedFileCount;
    }
}
