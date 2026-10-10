#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXVKGBufferNative : IDisposable
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXVKGBufferNative(in NGXVKGBuffer value)
    {
        this = default;

        try
        {
            if (value.Attributes is NGXResourceVK?[] attributes)
            {
                if (attributes.Length > 17)
                {
                    throw new ArgumentException("PInAttrib accepts at most 17 elements.", nameof(value));
                }

                for (int i = 0; i < attributes.Length; i++)
                {
                    if (attributes[i] is NGXResourceVK attributesElement)
                    {
                        PInAttrib[i] = NGXMarshal.AllocNative<NGXResourceVKNative>(new(in attributesElement));
                    }
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
            NGXMarshal.FreeNative<NGXResourceVKNative>(PInAttrib[i]);
        }

        this = default;
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private NGXPointer<NGXResourceVKNative> element;
    }
}
