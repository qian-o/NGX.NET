namespace NGX.NET;

public struct NGXCUDADLSSDCreateParams
{
    public NGXDLSSDCreateParams Feature;

    public nint CUContext;

    public nint CUStream;
}
