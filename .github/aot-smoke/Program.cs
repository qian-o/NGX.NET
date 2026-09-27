using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Streamline.NET;

internal static unsafe class Program
{
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static Resource Allocate(ResourceAllocationDesc* description, void* device)
    {
        return new(description->Type, device, description->State);
    }

    private static int Main(string[] args)
    {
        DLSSOptions options = new() { Mode = DLSSMode.MaxQuality, OutputWidth = 1920, OutputHeight = 1080 };
        DLSSOptimalSettings settings = new();
        if (args.Length > 0)
        {
            // Keep the native-call path reachable for trimming and AOT analysis.
            // CI runs without this argument and never loads an NVIDIA runtime.
            SL.SetLibraryPath(args[0]);
            DLSSOptimalSettings returned = SL.DLSS.GetOptimalSettings(in options);
            return (int)returned.OptimalRenderWidth;
        }
        ResourceAllocationDesc description = new(ResourceType.Buffer, null, 7, null);
        delegate* unmanaged[Cdecl]<ResourceAllocationDesc*, void*, Resource> callback = &Allocate;
        Resource resource = callback(&description, (void*)123);
        if (resource.Native != (void*)123 || resource.State != 7 || resource.StructType != Resource.TypeId)
        {
            return 1;
        }
        VkPhysicalDeviceVulkan13Features features = SL.GetVkPhysicalDeviceVulkan13Features(["dynamicRendering"]);
        if (features.DynamicRendering != 1 || options.StructVersion == 0 || settings.StructVersion == 0)
        {
            return 2;
        }
        if (SL.IsSignedByNVIDIA("nonexistent-file") || SL.VerifyEmbeddedSignature("nonexistent-file"))
        {
            return 3;
        }
        if (SL.FindStruct<DLSSOptions>(&options) != &options)
        {
            return 4;
        }
        try
        {
            SL.Shutdown();
            return 5;
        }
        catch (InvalidOperationException)
        {
            // The resolver must run in AOT before any SDK library is loaded.
        }
        Console.WriteLine("AOT/trim smoke passed, including a managed unmanaged-callable struct-return callback. No Streamline runtime was loaded.");
        return 0;
    }
}
