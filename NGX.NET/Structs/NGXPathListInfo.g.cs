#nullable enable

namespace NGX.NET;

public struct NGXPathListInfo
{
    public string[]? Paths;

    internal unsafe NGXPathListInfo(in NGXPathListInfoNative native)
    {
        Paths = new string[checked((int)native.Length)];
        for (int i = 0; i < Paths.Length; i++)
        {
            Paths[i] = NGXMarshal.PtrToString(native.Path[i], NGXEncoding.NativeWide)!;
        }
    }
}
