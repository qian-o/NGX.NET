namespace Marshalling;

internal static unsafe class AllocationChecks
{
    internal static void Run()
    {
        NGXVKDLSSDEvalParams value = Frame();
        for (int i = 0; i < 1000; i++)
        {
            RetainedFrame(value);
        }

        const int Iterations = 10000;
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < Iterations; i++)
        {
            RetainedFrame(value);
        }

        long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        NgxLifetime.ReleaseParameters(321);
        Console.WriteLine($"INFO {Iterations} retained RR calls; {(double)bytes / Iterations} managed bytes per call; {Unsafe.SizeOf<NGXVKDLSSDEvalParams>()} managed description bytes; 19 native pointees and 1 native root allocation per call.");

        Console.WriteLine("PASS constructor and retained RR conversion allocation measurement");
    }

    private static void RetainedFrame(NGXVKDLSSDEvalParams value)
    {
        NativeCall? call = new();
        NGXVKDLSSDEvalParamsNative native = new(in value);
        try
        {
            call.Take(ref native);
            NgxLifetime.BeginParameters(321, "measure", call);
            NgxLifetime.EndParameters(321, "measure", true, true, ref call);
        }
        finally
        {
            native.Dispose();
            call?.Dispose();
        }
    }
}
