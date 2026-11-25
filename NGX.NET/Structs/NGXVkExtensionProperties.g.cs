namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXVkExtensionProperties
{
    public ExtensionNameBuffer ExtensionName;

    public uint SpecVersion;

    [InlineArray(256)]
    public struct ExtensionNameBuffer
    {
        private byte element;
    }
}
