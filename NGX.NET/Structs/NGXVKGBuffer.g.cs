#nullable enable

namespace NGX.NET;

public struct NGXVKGBuffer
{
    public NGXResourceVK?[]? Attributes;

    internal unsafe NGXVKGBuffer(in NGXVKGBufferNative native)
    {
        Attributes = new NGXResourceVK?[17];
        for (int i = 0; i < Attributes.Length; i++)
        {
            Attributes[i] = ((NGXResourceVKNative*)native.PInAttrib[i]) is null ? null : new NGXResourceVK(in *(NGXResourceVKNative*)native.PInAttrib[i]);
        }
    }
}
