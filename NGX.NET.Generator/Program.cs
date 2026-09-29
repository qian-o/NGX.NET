using System.Text;
using System.Text.Json;
using NGX.NET.Generator;

string root = Path.GetFullPath(args.Length > 0 ? args[0] : ".");
string input = args.Length > 1 ? args[1] : Path.Combine(root, "NGX.NET.Generator", "ast.json");
using JsonDocument document = JsonDocument.Parse(File.ReadAllText(input));
Emitter emitter = new(document.RootElement);
Dictionary<string, string> files = emitter.Generate();
string output = Path.Combine(root, "NGX.NET");
int changed = 0;

foreach ((string name, string content) in files)
{
    string path = Path.Combine(output, name);
    Directory.CreateDirectory(Path.GetDirectoryName(path)!);

    if (!File.Exists(path) || File.ReadAllText(path) != content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(true));
        changed++;
    }
}

foreach (string path in Directory.EnumerateFiles(output, "*.g.cs", SearchOption.AllDirectories))
{
    if (!files.ContainsKey(Path.GetRelativePath(output, path)))
    {
        File.Delete(path);
        changed++;
    }
}

Console.WriteLine($"Generated {files.Count} files; {changed} changed.");
