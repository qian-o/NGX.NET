namespace Marshalling;

internal static class FrameGenerationRetentionChecks
{
    private static readonly (bool Options, int ExpectedRoots)[] FrameGenerationCases = [(true, 1), (false, 2), (false, 2), (true, 1)];

    internal static void Run()
    {
        foreach ((bool options, int expected) in FrameGenerationCases)
        {
            NativeCall? call = Storage();
            call.HasFrameGenerationOptions = options;
            NgxLifetime.BeginParameters(456, "fg", call);
            NgxLifetime.EndParameters(456, "fg", true, true, ref call);
            Assert(NgxCallbacks.Count == expected, "Conditional pointer writes discarded old options");
        }

        NgxLifetime.ReleaseParameters(456);
        Assert(NgxCallbacks.Count is 0, "FG pointer cleanup");

        Console.WriteLine("PASS omitted DLSSG options preserve previously registered matrix storage");
    }
}
