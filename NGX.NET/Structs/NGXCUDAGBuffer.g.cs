#nullable enable

namespace NGX.NET;

public struct NGXCUDAGBuffer
{
    public ulong?[]? Attributes;

    internal unsafe NGXCUDAGBuffer(in NGXCUDAGBufferNative native)
    {
        Attributes = new ulong?[17];
        for (int i = 0; i < Attributes.Length; i++)
        {
            Attributes[i] = ((ulong*)native.PInAttrib[i]) is null ? null : *(ulong*)native.PInAttrib[i];
        }
    }
}
