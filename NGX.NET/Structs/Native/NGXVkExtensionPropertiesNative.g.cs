#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 260)]
internal unsafe struct NGXVkExtensionPropertiesNative : IDisposable
{
    [FieldOffset(0)]
    public fixed sbyte ExtensionName[256];

    [FieldOffset(256)]
    public uint SpecVersion;

    public NGXVkExtensionPropertiesNative(in NGXVkExtensionProperties value)
    {
        this = default;

        try
        {
            fixed (sbyte* buffer = ExtensionName)
            {
                NGXMarshal.WriteUtf8(value.ExtensionName, new Span<byte>(buffer, 256));
            }

            SpecVersion = value.SpecVersion;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        this = default;
    }
}
