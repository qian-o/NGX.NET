namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXCUDAGBuffer
{
    public AttributesBuffer Attributes;

    [InlineArray(17)]
    public struct AttributesBuffer
    {
        private nint element;
    }
}
