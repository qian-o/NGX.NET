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
        NativeLifetime.Release(new NGXParameter(321));
        Console.WriteLine($"INFO {Iterations} retained RR calls; {(double)bytes / Iterations} managed bytes per call; {Unsafe.SizeOf<NGXVKDLSSDEvalParams>()} managed description bytes.");

        Console.WriteLine("PASS retained RR conversion allocation measurement");
    }

    private static void RetainedFrame(NGXVKDLSSDEvalParams value)
    {
        NativeScope scope = new();
        scope.Alloc(new NGXVKDLSSDEvalParamsNative(in value, scope));
        NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, new NGXParameter(321), "measure", scope, NGXResult.Success);
    }
}
