namespace Marshalling;

internal static class BackendCleanupChecks
{
    internal static void Run()
    {
        NGXParameter parameters = new(789);
        TrackingScope d3dParameters = Storage();
        TrackingScope vulkanParameters = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "d3d", d3dParameters, NGXResult.Success);
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "vulkan", vulkanParameters, NGXResult.Success);
        TrackingScope firstDevice = Storage();
        TrackingScope lastDevice = Storage();
        TrackingScope vulkanDevice = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, 1, firstDevice, NGXResult.Success);
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, 2, lastDevice, NGXResult.Success);
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, 3, vulkanDevice, NGXResult.Success);
        NativeLifetime.Release(NGXGraphicsAPI.D3D12, 1);
        Assert(firstDevice.IsDisposed && !lastDevice.IsDisposed && !d3dParameters.IsDisposed, "Another device still uses backend data");
        NativeLifetime.Release(NGXGraphicsAPI.D3D12, 2);
        Assert(lastDevice.IsDisposed && d3dParameters.IsDisposed, "Last device cleanup");
        Assert(!vulkanDevice.IsDisposed && !vulkanParameters.IsDisposed, "Backend cleanup preserves another API's scopes on the same parameter handle");
        NativeLifetime.Release(NGXGraphicsAPI.Vulkan, 0);
        Assert(vulkanDevice.IsDisposed && vulkanParameters.IsDisposed, "Whole backend shutdown releases its device and parameter scopes");

        Console.WriteLine("PASS last-device and whole-backend cleanup release only the owning API's scopes");
    }
}
