namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 260)]
internal unsafe struct NGXVkExtensionPropertiesNative
{
    [FieldOffset(0)]
    public fixed byte ExtensionName[256];

    [FieldOffset(256)]
    public uint SpecVersion;
}
