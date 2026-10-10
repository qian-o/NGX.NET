namespace Marshalling;

internal static class ParameterChecks
{
    internal static void Run()
    {
        for (int i = 0; i < 3; i++)
        {
            NativeCall? call = Storage();
            NgxLifetime.BeginParameters(123, "eval", call);
            NgxLifetime.EndParameters(123, "eval", true, i is 2, ref call);
            Assert(call is null && NgxCallbacks.Count == (i is 2 ? 1 : i + 1), "Failure retention / success replacement");
        }

        NativeCall? cancelled = Storage();
        NgxLifetime.BeginParameters(123, "eval", cancelled);
        NgxLifetime.EndParameters(123, "eval", false, false, ref cancelled);
        cancelled!.Dispose();
        Assert(NgxCallbacks.Count is 1, "Pre-entry failure retains old data");
        NgxLifetime.ReleaseParameters(123);
        Assert(NgxCallbacks.Count is 0, "Reset cleanup");

        Console.WriteLine("PASS parameter snapshots preserve failed-call data and release on success or Reset");
    }
}
