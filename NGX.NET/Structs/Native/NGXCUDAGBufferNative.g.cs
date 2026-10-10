namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 136)]
internal unsafe struct NGXCUDAGBufferNative
{
    [FieldOffset(0)]
    public PInAttribBuffer PInAttrib;

    public NGXCUDAGBufferNative(in NGXCUDAGBuffer value, NativeScope scope)
    {
        this = default;

        if (value.Attributes is ulong?[] attributes)
        {
            if (attributes.Length > 17)
            {
                throw new ArgumentException("PInAttrib accepts at most 17 elements.", nameof(value));
            }

            for (int i = 0; i < attributes.Length; i++)
            {
                PInAttrib[i] = attributes[i].HasValue ? (nint)scope.Alloc(attributes[i].GetValueOrDefault()) : 0;
            }
        }
    }

    [InlineArray(17)]
    internal struct PInAttribBuffer
    {
        private nint element;
    }
}
