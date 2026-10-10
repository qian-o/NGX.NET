#nullable enable

namespace NGX.NET;

public struct NGXCUDADevice
{
    public nint CudaContext;

    public nint CudaStream;

    internal unsafe NGXCUDADevice(in NGXCUDADeviceNative native)
    {
        CudaContext = (nint)native.CudaContext;
        CudaStream = (nint)native.CudaStream;
    }
}
