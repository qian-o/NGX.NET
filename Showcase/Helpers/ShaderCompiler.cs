using System.Security.Cryptography;
using System.Text;
using Slangc.NET;

namespace Showcase.Helpers;

internal static class ShaderCompiler
{
    private static readonly Dictionary<(string File, string Entry, string Stage, bool Vulkan, bool RayQuery), byte[]> cache = [];
    private static readonly string shaderDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
    private static readonly string cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGX.NET", "Showcase", "Shaders");
    private static readonly byte[] sourceHash = HashSources();

    public static byte[] Compile(string file, string entry, string stage, bool vulkan, bool rayQuery)
    {
        (string, string, string, bool, bool) key = (file, entry, stage, vulkan, rayQuery);

        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached;
        }

        string path = Path.Combine(shaderDirectory, file);
        List<string> arguments =
        [
            path,
            "-entry",
            entry,
            "-stage",
            stage,
            "-target",
            vulkan ? "spirv" : "dxil",
            "-profile",
            "sm_6_6",
            "-matrix-layout-row-major",
            "-O3",
            "-D",
            $"HAS_RAY_QUERY={(rayQuery ? 1 : 0)}"
        ];

        if (vulkan)
        {
            arguments.AddRange(["-fvk-use-entrypoint-name", "-fvk-use-dx-position-w", "-fvk-invert-y"]);
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(sourceHash);
        hash.AppendData(Encoding.UTF8.GetBytes(string.Join('\0', arguments)));
        string cachePath = Path.Combine(cacheDirectory, $"{Convert.ToHexString(hash.GetHashAndReset())}.bin");
        byte[] compiled;

        if (File.Exists(cachePath))
        {
            compiled = File.ReadAllBytes(cachePath);
        }
        else
        {
            compiled = SlangCompiler.Compile([.. arguments]);
            Directory.CreateDirectory(cacheDirectory);
            string temporary = $"{cachePath}.{Guid.NewGuid():N}.tmp";
            File.WriteAllBytes(temporary, compiled);
            File.Move(temporary, cachePath, true);
        }

        cache.Add(key, compiled);

        return compiled;
    }

    private static byte[] HashSources()
    {
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(typeof(SlangCompiler).Module.ModuleVersionId.ToByteArray());

        foreach (string file in Directory.EnumerateFiles(shaderDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes(Path.GetRelativePath(shaderDirectory, file) + '\0'));
            hash.AppendData(SHA256.HashData(File.ReadAllBytes(file)));
        }

        return hash.GetHashAndReset();
    }
}
