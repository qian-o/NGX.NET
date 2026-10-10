namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXCUDADeviceNative(in NGXCUDADevice value)
{
    [FieldOffset(0)]
    public void* CudaContext = (void*)value.CudaContext;

    [FieldOffset(8)]
    public void* CudaStream = (void*)value.CudaStream;
}
