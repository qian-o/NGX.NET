using Slangc.NET;

namespace Showcase.Helpers;

internal static class ShaderCompiler
{
    private static readonly Dictionary<(string File, string Entry, string Stage, bool Vulkan, bool RayQuery), byte[]> cache = [];

    public static byte[] Compile(string file, string entry, string stage, bool vulkan, bool rayQuery)
    {
        (string, string, string, bool, bool) key = (file, entry, stage, vulkan, rayQuery);

        if (cache.TryGetValue(key, out byte[]? cached))
        {
            return cached;
        }

        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", file);
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

        byte[] compiled = SlangCompiler.Compile([.. arguments]);
        cache.Add(key, compiled);

        return compiled;
    }
}
