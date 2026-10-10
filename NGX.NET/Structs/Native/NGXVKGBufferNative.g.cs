namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXVKGBufferNative
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXVKGBufferNative(in NGXVKGBuffer value, NativeScope scope)
    {
        this = default;

        if (value.Attributes is NGXResourceVK?[] attributes)
        {
            if (attributes.Length > 17)
            {
                throw new ArgumentException("PInAttrib accepts at most 17 elements.", nameof(value));
            }

            for (int i = 0; i < attributes.Length; i++)
            {
                PInAttrib[i] = attributes[i] is NGXResourceVK attributesElement ? (nint)scope.Alloc(new NGXResourceVKNative(in attributesElement)) : 0;
            }
        }
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private nint element;
    }
}
