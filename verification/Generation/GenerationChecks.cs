namespace Generation;

internal static class GenerationChecks
{
    private static readonly UTF8Encoding utf8 = new(true);

    internal static Dictionary<string, string> Run(JsonElement ast, string root)
    {
        Dictionary<string, string> first = new GenerationPipeline(AstReader.Read(ast)).Generate();
        Dictionary<string, string> second = new GenerationPipeline(AstReader.Read(ast)).Generate();
        Require(first.Count == second.Count, "Generation changed the number of files.");

        foreach ((string path, string source) in first)
        {
            Require(second.TryGetValue(path, out string? repeated) && repeated == source, $"Non-deterministic source: {path}.");
            byte[] expected = [.. utf8.GetPreamble(), .. utf8.GetBytes(source)];
            Require(File.ReadAllBytes(Path.Combine(root, "NGX.NET", path)).AsSpan().SequenceEqual(expected), $"The checked-in file differs from direct emitter output: {path}.");
        }

        Console.WriteLine($"PASS {first.Count} direct emitter outputs: BOM, deterministic bytes and checked-in parity.");

        return first;
    }
}
