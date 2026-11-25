namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXCUDADevice
{
    public void* CudaContext;

    public void* CudaStream;
}
