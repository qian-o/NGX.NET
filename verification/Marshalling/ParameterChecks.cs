namespace Marshalling;

internal static class ParameterChecks
{
    internal static void Run()
    {
        NGXParameter parameters = new(123);
        TrackingScope first = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "eval", first, NGXResult.Success);
        TrackingScope failed = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "eval", failed, NGXResult.Fail);
        Assert(!first.IsDisposed && !failed.IsDisposed, "Failed helper preserves both old and newly written data");
        TrackingScope replacement = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "eval", replacement, NGXResult.Success);
        Assert(first.IsDisposed && failed.IsDisposed && !replacement.IsDisposed, "Successful helper replacement releases previous snapshots");
        TrackingScope cancelled = Storage();
        cancelled.Dispose();
        Assert(cancelled.IsDisposed && !replacement.IsDisposed, "Pre-entry conversion failure does not replace retained data");

        TrackingScope otherSlot = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "other", otherSlot, NGXResult.Success);
        Assert(!replacement.IsDisposed && !otherSlot.IsDisposed, "Independent parameter slots coexist");
        NativeLifetime.Release(parameters);
        NativeLifetime.Release(parameters);
        Assert(replacement.IsDisposed && otherSlot.IsDisposed, "Reset or destroy releases every parameter slot once");

        Console.WriteLine("PASS parameter snapshots preserve failure data and release on replacement, reset or destroy");
    }
}
