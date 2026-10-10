namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXCUDADLSSDCreateParamsNative(in NGXCUDADLSSDCreateParams value)
{
    [FieldOffset(0)]
    public NGXDLSSDCreateParamsNative Feature = new(in value.Feature);

    [FieldOffset(40)]
    public void* InCUContext = (void*)value.CUContext;

    [FieldOffset(48)]
    public void* InCUStream = (void*)value.CUStream;
}
