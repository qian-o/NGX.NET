#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXCUDAGBufferNative : IDisposable
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXCUDAGBufferNative(in NGXCUDAGBuffer value)
    {
        this = default;

        try
        {
            if (value.Attributes is ulong?[] attributes)
            {
                if (attributes.Length > 17)
                {
                    throw new ArgumentException("PInAttrib accepts at most 17 elements.", nameof(value));
                }

                for (int i = 0; i < attributes.Length; i++)
                {
                    PInAttrib[i] = attributes[i].HasValue ? NGXMarshal.AllocValue(attributes[i].GetValueOrDefault()) : null;
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
        for (int i = 16; i >= 0; i--)
        {
            NGXMarshal.Free(PInAttrib[i]);
        }

        this = default;
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private NGXPointer<ulong> element;
    }
}
