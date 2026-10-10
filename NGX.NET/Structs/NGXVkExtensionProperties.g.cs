namespace NGX.NET;

public struct NGXVkExtensionProperties
{
    public string? ExtensionName;

    public uint SpecVersion;

    internal unsafe NGXVkExtensionProperties(in NGXVkExtensionPropertiesNative native)
    {
        fixed (byte* buffer = native.ExtensionName)
        {
            ExtensionName = NativeTextHelper.ReadUtf8(new ReadOnlySpan<byte>(buffer, 256));
        }

        SpecVersion = native.SpecVersion;
    }
}
