namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXCUDADLSSDCreateParams
{
    public NGXDLSSDCreateParams Feature;

    public void* CUContext;

    public void* CUStream;
}
