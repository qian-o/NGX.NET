#nullable enable

namespace NGX.NET;

public struct NGXVkExtensionProperties
{
    public string? ExtensionName;

    public uint SpecVersion;

    internal unsafe NGXVkExtensionProperties(in NGXVkExtensionPropertiesNative native)
    {
        fixed (sbyte* buffer = native.ExtensionName)
        {
            ExtensionName = NGXMarshal.ReadUtf8(new ReadOnlySpan<byte>(buffer, 256));
        }

        SpecVersion = native.SpecVersion;
    }
}
