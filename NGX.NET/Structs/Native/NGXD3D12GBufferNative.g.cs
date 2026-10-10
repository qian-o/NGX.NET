#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXD3D12GBufferNative
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXD3D12GBufferNative(in NGXD3D12GBuffer value)
    {
        this = default;

        if (value.Attributes is nint[] attributes)
        {
            if (attributes.Length > 17)
            {
                throw new ArgumentException("PInAttrib accepts at most 17 elements.", nameof(value));
            }

            for (int i = 0; i < attributes.Length; i++)
            {
                PInAttrib[i] = attributes[i];
            }
        }
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private nint element;
    }
}
