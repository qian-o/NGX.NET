#nullable enable

namespace NGX.NET;

public struct NGXCUDADLSSDCreateParams
{
    public NGXDLSSDCreateParams Feature;

    public nint CUContext;

    public nint CUStream;

    internal unsafe NGXCUDADLSSDCreateParams(in NGXCUDADLSSDCreateParamsNative native)
    {
        Feature = new(in native.Feature);
        CUContext = (nint)native.InCUContext;
        CUStream = (nint)native.InCUStream;
    }
}
