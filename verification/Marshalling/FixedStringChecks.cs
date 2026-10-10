namespace Marshalling;

internal static unsafe class FixedStringChecks
{
    internal static void Run()
    {
        NGXVkExtensionProperties value = new()
        {
            ExtensionName = "VK_\u6D4B\u8BD5\U0001F680",
            SpecVersion = 123
        };
        NGXVkExtensionPropertiesNative native = new(in value);
        NGXVkExtensionProperties read = new(in native);
        Assert(read.ExtensionName == value.ExtensionName && read.SpecVersion is 123, "Fixed buffer round trip");
        new Span<byte>(native.ExtensionName, 256).Fill((byte)'x');
        read = new(in native);
        Assert(read.ExtensionName!.Length is 256, "Bounded missing terminator");
        value.ExtensionName = new string('x', 256);
        Throws<ArgumentException>(() => new NGXVkExtensionPropertiesNative(in value));
        native.Dispose();
        void* key = NGXMarshal.StringToPtr(Ngx.EParameterReserved00, NGXEncoding.Utf8);
        try
        {
            Assert(new ReadOnlySpan<byte>(key, 3).SequenceEqual(new byte[] { 35, 0, 0 }), "SDK binary key");
        }
        finally
        {
            NGXMarshal.Free(key);
        }

        Console.WriteLine("PASS fixed-buffer strings respect capacity and read without a terminator");
    }
}
