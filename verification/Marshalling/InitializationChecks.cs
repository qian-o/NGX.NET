namespace Marshalling;

internal static class InitializationChecks
{
    internal static void Run()
    {
        NativeCall? call = Storage();
        NgxLifetime.BeginInitialization("test", 1, call);
        NgxLifetime.EndInitialization("test", 1, true, ref call);
        Assert(call is null && NgxCallbacks.Count is 1, "Init commit ownership");
        call = Storage();
        NgxLifetime.BeginInitialization("test", 2, call);
        NgxLifetime.EndInitialization("test", 2, false, ref call);
        call!.Dispose();
        Assert(NgxCallbacks.Count is 1, "Init rollback");
        NgxLifetime.Shutdown("test", 2);
        Assert(NgxCallbacks.Count is 1, "Different device retained");
        NgxLifetime.Shutdown("test", 1);
        Assert(NgxCallbacks.Count is 0, "Shutdown cleanup");

        Console.WriteLine("PASS Init ownership is reserved before native entry, committed or rolled back internally");
    }
}
