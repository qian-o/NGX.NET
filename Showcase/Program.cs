using System.Runtime.InteropServices;

namespace Showcase;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            if (args.Contains("--check-shaders"))
            {
                foreach (bool shaderVulkan in OperatingSystem.IsWindows() ? new[] { false, true } : new[] { true })
                {
                    foreach ((string entry, string stage) in ShaderEntries())
                    {
                        Console.WriteLine($"{(shaderVulkan ? "SPIR-V" : "DXIL")} {entry}: {ShaderCompiler.Compile("Scene.slang", entry, stage, shaderVulkan).Length:N0} bytes");
                    }
                }

                if (!OperatingSystem.IsWindows())
                {
                    Console.WriteLine("DXIL compilation must also be checked on Windows with the deployed DXC runtime.");
                }

                return 0;
            }
            if (args.Contains("--check-scene"))
            {
                Scene.Load(Path.Combine(AppContext.BaseDirectory, "Assets", "Scenes", "Sponza.gltf"));
                return 0;
            }
            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                throw new PlatformNotSupportedException("Showcase requires Windows x64. Shader/scene checks can run separately on other supported compiler platforms.");
            }

            Console.WriteLine("Streamline.NET Showcase\n1. DirectX 12\n2. Vulkan");
            Console.Write("Select backend [1]: ");
            string? choice = Console.ReadLine();
            bool vulkan = choice?.Trim() == "2";
            using UserInterface ui = new();
            using Window window = new();
            using RHI rhi = vulkan ? new VulkanRHI(window, ui) : new DirectX12RHI(window, ui);
            rhi.Initialize();
            while (!window.Closed)
            {
                rhi.RenderFrame();
            }

            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            string logs = Path.Combine(AppContext.BaseDirectory, "Logs");
            Directory.CreateDirectory(logs);
            string path = Path.Combine(logs, $"showcase-{DateTime.Now:yyyyMMdd-HHmmss}.log");
            File.WriteAllText(path, exception.ToString());
            Console.Error.WriteLine($"Failure log: {path}");
            if (OperatingSystem.IsWindows() && !Console.IsInputRedirected)
            {
                Console.WriteLine("Press Enter to close.");
                Console.ReadLine();
            }
            return 1;
        }
    }

    internal static IEnumerable<(string Entry, string Stage)> ShaderEntries()
    {
        yield return ("SceneVS", "vertex");
        yield return ("ScenePS", "fragment");
        yield return ("UiVS", "vertex");
        yield return ("UiPS", "fragment");
        foreach (ComputePass pass in Enum.GetValues<ComputePass>())
        {
            yield return (pass.ToString(), "compute");
        }
    }
}
