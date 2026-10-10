#nullable enable

namespace NGX.NET;

public struct NGXD3D12GBuffer
{
    public nint[]? Attributes;

    internal unsafe NGXD3D12GBuffer(in NGXD3D12GBufferNative native)
    {
        Attributes = new nint[17];
        for (int i = 0; i < Attributes.Length; i++)
        {
            Attributes[i] = native.PInAttrib[i];
        }
    }
}
