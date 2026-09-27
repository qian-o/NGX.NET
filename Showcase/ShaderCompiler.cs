using Slangc.NET;

namespace Showcase;

internal static class ShaderCompiler
{
    public static byte[] Compile(string file, string entry, string stage, bool vulkan, bool rayQuery = true)
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", file);
        List<string> arguments = [path, "-entry", entry, "-stage", stage, "-target", vulkan ? "spirv" : "dxil", "-profile", "sm_6_6", "-matrix-layout-row-major", "-O3", "-D", $"HAS_RAY_QUERY={(rayQuery ? 1 : 0)}"];
        if (vulkan)
        {
            arguments.AddRange(["-fvk-use-entrypoint-name", "-fvk-use-dx-position-w", "-fvk-invert-y"]);
        }
        return SlangCompiler.Compile([.. arguments]);
    }
}
