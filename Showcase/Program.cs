using System.Runtime.InteropServices;
using Showcase.DirectX12;
using Showcase.Handlers;
using Showcase.Vulkan;
using Streamline.NET;

namespace Showcase;

internal static class Program
{
    private static async Task<int> Main()
    {
        try
        {
            if (!OperatingSystem.IsWindows() || RuntimeInformation.ProcessArchitecture != Architecture.X64)
            {
                throw new PlatformNotSupportedException("Showcase requires Windows x64.");
            }

            Console.WriteLine("Streamline.NET Showcase\n1. DirectX 12\n2. Vulkan");
            Console.Write("Select backend [1]: ");
            string? choice = Console.ReadLine();
            bool vulkan = choice?.Trim() == "2";
            RuntimeOptions runtime = new()
            {
                RenderAPI = vulkan ? RenderAPI.Vulkan : RenderAPI.D3D12,
                DLSS = true,
                DLSSD = true,
                DLSSG = true,
                Reflex = true,
                PCL = true
            };

            Console.WriteLine("Preparing Streamline runtime...");
            await SL.EnsureRuntimeAsync(Path.Combine(AppContext.BaseDirectory, "Assets", "Streamline"), runtime);

            using UserInterface ui = new();
            using Window window = new();
            using RHI rhi = vulkan ? new VulkanRHI(window, ui) : new DirectX12RHI(window, ui);
            rhi.Initialize(runtime);

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
}
