namespace Marshalling;

internal static unsafe class CudaChecks
{
    internal static void Run()
    {
        NGXCUDADevice device = new()
        {
            CudaContext = 123,
            CudaStream = 456
        };
        NGXCUDADeviceNative* first = NgxLifetime.CudaDevice(device);
        NGXCUDADevice copy = device;
        Assert(first == NgxLifetime.CudaDevice(copy), "Stable CUDA storage");
        NgxLifetime.FinishCudaDevice((nint)first, true);
        NgxLifetime.FinishCudaDevice((nint)first, false);
        Assert(first == NgxLifetime.CudaDevice(copy), "Later failure preserves initialized device");
        NgxLifetime.Shutdown("CUDA", (nint)first);

        Console.WriteLine("PASS CUDA device addresses remain stable across copied public descriptors");
    }
}
