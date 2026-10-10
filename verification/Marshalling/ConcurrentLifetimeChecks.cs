namespace Marshalling;

internal static class ConcurrentLifetimeChecks
{
    internal static void Run()
    {
        int disposals = 0;
        Parallel.For(0, 64, i =>
        {
            nint device = i + 1;
            NGXParameter parameters = new(i + 1);
            TrackingScope initialization = Storage(() => Interlocked.Increment(ref disposals));
            TrackingScope values = Storage(() =>
            {
                Interlocked.Increment(ref disposals);
                NativeLifetime.Release(parameters);
            });
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, device, initialization, NGXResult.Success);
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "concurrent", values, NGXResult.Success);
            NativeLifetime.Release(parameters);
            NativeLifetime.Release(NGXGraphicsAPI.D3D12, device);
            Assert(initialization.IsDisposed && values.IsDisposed, "Concurrent lifetime cleanup disposes every retained scope");
        });
        Assert(disposals is 128, "Concurrent and reentrant cleanup disposes each scope exactly once");

        Console.WriteLine("PASS scoped locks preserve concurrent ownership and reentrant parameter cleanup");
    }
}
