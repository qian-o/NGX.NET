#nullable enable

namespace NGX.NET;

public struct NGXD3D11GBuffer
{
    public nint[]? Attributes;

    internal unsafe NGXD3D11GBuffer(in NGXD3D11GBufferNative native)
    {
        Attributes = new nint[17];
        for (int i = 0; i < Attributes.Length; i++)
        {
            Attributes[i] = native.PInAttrib[i];
        }
    }
}
