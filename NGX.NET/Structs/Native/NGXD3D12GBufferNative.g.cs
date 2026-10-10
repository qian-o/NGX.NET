#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXD3D12GBufferNative : IDisposable
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXD3D12GBufferNative(in NGXD3D12GBuffer value)
    {
        this = default;

        try
        {
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
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private nint element;
    }
}
