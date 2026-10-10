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
        NGXCUDADeviceNative* first = NativeLifetime.GetCudaDevice(device);
        NGXCUDADevice copy = device;
        Assert(first == NativeLifetime.GetCudaDevice(copy), "Stable CUDA storage across copied descriptors");
        TrackingScope failed = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Cuda, (nint)first, failed, NGXResult.Fail);
        Assert(failed.IsDisposed && first == NativeLifetime.GetCudaDevice(copy), "Failed initialization preserves cached CUDA device storage");
        TrackingScope initialized = Storage();
        NativeLifetime.Retain(NGXGraphicsAPI.Cuda, (nint)first, initialized, NGXResult.Success);
        NativeLifetime.Release(NGXGraphicsAPI.Cuda, (nint)first);
        Assert(initialized.IsDisposed, "CUDA shutdown releases initialization scope");
        long before = GC.GetAllocatedBytesForCurrentThread();
        NGXCUDADeviceNative* recreated = NativeLifetime.GetCudaDevice(device);
        long recreatedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert(recreatedBytes > 0 && (nint)recreated->CudaContext is 123 && (nint)recreated->CudaStream is 456, "Shutdown clears the CUDA descriptor cache and recreates owned storage");
        before = GC.GetAllocatedBytesForCurrentThread();
        NGXCUDADeviceNative* cached = NativeLifetime.GetCudaDevice(copy);
        long cachedBytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert(cached == recreated && cachedBytes is 0, "Cached CUDA descriptor lookup does not allocate");
        NativeLifetime.Release(NGXGraphicsAPI.Cuda, 0);

        Console.WriteLine("PASS CUDA device copies stay stable across failures and are recreated only after shutdown");
    }
}
