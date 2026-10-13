namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXD3D11GBuffer
{
    public AttributesBuffer Attributes;

    [InlineArray(17)]
    public struct AttributesBuffer
    {
        private nint element;
    }
}
