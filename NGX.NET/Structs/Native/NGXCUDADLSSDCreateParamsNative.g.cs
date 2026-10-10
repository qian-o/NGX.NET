#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXCUDADLSSDCreateParamsNative : IDisposable
{
    [FieldOffset(0)]
    public NGXDLSSDCreateParamsNative Feature;

    [FieldOffset(40)]
    public void* InCUContext;

    [FieldOffset(48)]
    public void* InCUStream;

    public NGXCUDADLSSDCreateParamsNative(in NGXCUDADLSSDCreateParams value)
    {
        try
        {
            Feature = new(in value.Feature);
            InCUContext = (void*)value.CUContext;
            InCUStream = (void*)value.CUStream;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        Feature.Dispose();
        this = default;
    }
}
