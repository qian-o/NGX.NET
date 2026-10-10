#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXCUDADeviceNative : IDisposable
{
    [FieldOffset(0)]
    public void* CudaContext;

    [FieldOffset(8)]
    public void* CudaStream;

    public NGXCUDADeviceNative(in NGXCUDADevice value)
    {
        try
        {
            CudaContext = (void*)value.CudaContext;
            CudaStream = (void*)value.CudaStream;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
