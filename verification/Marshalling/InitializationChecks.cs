namespace Marshalling;

internal static class InitializationChecks
{
    internal static void Run()
    {
        TrackingScope initialized = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, 1, initialized, NGXResult.Success);
        Assert(!initialized.IsDisposed, "Init commit ownership");
        TrackingScope failed = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, 2, failed, NGXResult.Fail);
        Assert(failed.IsDisposed && !initialized.IsDisposed, "Init rollback");
        NativeLifetime.Release(NGXGraphicsAPI.D3D12, 2);
        Assert(!initialized.IsDisposed, "Different device retained");
        TrackingScope additional = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.D3D12, 1, additional, NGXResult.Success);
        NativeLifetime.Release(NGXGraphicsAPI.D3D12, 1);
        Assert(initialized.IsDisposed && additional.IsDisposed, "Shutdown releases every initialized scope for its device");

        Console.WriteLine("PASS initialization scopes commit after success, release failures and survive until shutdown");
    }
}
