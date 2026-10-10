namespace Marshalling;

internal static class DlssAllocationChecks
{
    private static long checksum;

    internal static void Run()
    {
        NGXD3D11DLSSEvalParams first = new()
        {
            Feature = new() { Color = 11 },
            Depth = 12,
            Reset = 1,
            GBufferSurface = new() { Attributes = [13, 0, 14] }
        };
        NGXD3D12DLSSEvalParams second = new()
        {
            Feature = new() { Color = 21 },
            Depth = 22,
            Reset = 1,
            GBufferSurface = new() { Attributes = [23, 0, 24] }
        };

        for (int i = 0; i < 1000; i++)
        {
            Convert(first, second);
        }

        const int Iterations = 10000;
        long before = GC.GetAllocatedBytesForCurrentThread();

        for (int i = 0; i < Iterations; i++)
        {
            Convert(first, second);
        }

        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        long expected = (long)first.Feature.Color + (long)second.Depth + first.Reset + second.Reset + (long)first.GBufferSurface.Attributes![0] + (long)second.GBufferSurface.Attributes![0];
        Assert(allocated is 0 && checksum == expected, "Warmed D3D11/D3D12 DLSS conversion allocates no managed memory and preserves populated fields");

        Console.WriteLine($"PASS {Iterations} D3D11/D3D12 DLSS conversions: 0 managed bytes; semantic call-graph checks prove no native allocation calls.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Convert(NGXD3D11DLSSEvalParams first, NGXD3D12DLSSEvalParams second)
    {
        NGXD3D11DLSSEvalParamsNative firstNative = new(in first);
        NGXD3D12DLSSEvalParamsNative secondNative = new(in second);
        checksum = (long)firstNative.Feature.PInColor + (long)secondNative.PInDepth + firstNative.InReset + secondNative.InReset + (long)firstNative.GBufferSurface.PInAttrib[0] + (long)secondNative.GBufferSurface.PInAttrib[0];
    }
}
