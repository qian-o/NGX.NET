namespace Marshalling;

internal static class BackendCleanupChecks
{
    private static readonly nint[] BackendDevices = [1, 2];

    internal static void Run()
    {
        NgxLifetime.PrepareParameters();
        NgxLifetime.RegisterParameters("test", 789);
        NativeCall? parameters = Storage();
        NgxLifetime.BeginParameters(789, "eval", parameters);
        NgxLifetime.EndParameters(789, "eval", true, true, ref parameters);

        foreach (nint device in BackendDevices)
        {
            NativeCall? call = Storage();
            NgxLifetime.BeginInitialization("test", device, call);
            NgxLifetime.EndInitialization("test", device, true, ref call);
        }

        Assert(NgxCallbacks.Count is 3, "Backend ownership setup");
        NgxLifetime.Shutdown("test", 1);
        Assert(NgxCallbacks.Count is 2, "Another device still uses backend data");
        NgxLifetime.Shutdown("test", 2);
        Assert(NgxCallbacks.Count is 0, "Last device cleanup");

        Console.WriteLine("PASS shutdown of the last tracked device releases its backend's parameter snapshots");
    }
}
