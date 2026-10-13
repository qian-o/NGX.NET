using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Slangc.NET;

namespace Showcase.Helpers;

internal static class ShaderCompiler
{
    private static readonly string ShaderDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders");
    private static readonly string CacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NGX.NET", "Showcase", "Shaders");
    private static readonly byte[] SourceHash = HashSources();

    private static readonly Dictionary<(string File, string Entry, string Stage, bool Vulkan), byte[]> cache = [];

    public static byte[] Compile(string file, string entry, string stage, bool vulkan)
    {
        (string, string, string, bool) key = (file, entry, stage, vulkan);
        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached;
        }

        string path = Path.Combine(ShaderDirectory, file);
        List<string> arguments = [path, "-entry", entry, "-stage", stage, "-target", vulkan ? "spirv" : "dxil", "-profile", "sm_6_6", "-matrix-layout-row-major", "-O3"];
        if (vulkan)
        {
            arguments.AddRange(["-fvk-use-entrypoint-name", "-fvk-use-dx-position-w", "-fvk-invert-y"]);

            if (OperatingSystem.IsWindows())
            {
                // Slang's downstream LoadLibrary does not use the managed resolver that loads slang-compiler from the NuGet RID folder.
                string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
                string optimizerDirectory = Path.Combine(AppContext.BaseDirectory, "runtimes", $"win-{architecture}", "native");
                if (!File.Exists(Path.Combine(optimizerDirectory, "slang-glslang.dll")))
                {
                    optimizerDirectory = AppContext.BaseDirectory;
                }

                arguments.AddRange(["-spirv-opt-path", optimizerDirectory]);
            }
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        hash.AppendData(SourceHash);
        hash.AppendData(Encoding.UTF8.GetBytes(string.Join('\0', arguments)));
        string cachePath = Path.Combine(CacheDirectory, $"{Convert.ToHexString(hash.GetHashAndReset())}.bin");
        byte[] compiled;
        if (File.Exists(cachePath))
        {
            compiled = File.ReadAllBytes(cachePath);
        }
        else
        {
            compiled = SlangCompiler.Compile([.. arguments]);
            Directory.CreateDirectory(CacheDirectory);
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

        foreach (string file in Directory.EnumerateFiles(ShaderDirectory, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            hash.AppendData(Encoding.UTF8.GetBytes($"{Path.GetRelativePath(ShaderDirectory, file)}\0"));
            hash.AppendData(SHA256.HashData(File.ReadAllBytes(file)));
        }

        return hash.GetHashAndReset();
    }
}
