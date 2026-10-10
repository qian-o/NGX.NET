namespace Marshalling;

internal static unsafe class FixedStringChecks
{
    internal static void Run()
    {
        NGXVkExtensionPropertiesNative native = default;
        ReadOnlySpan<byte> extension = "VK_\u6D4B\u8BD5\U0001F680"u8;
        extension.CopyTo(new Span<byte>(native.ExtensionName, 256));
        native.SpecVersion = 123;
        NGXVkExtensionProperties read = new(in native);
        Assert(read.ExtensionName is "VK_\u6D4B\u8BD5\U0001F680" && read.SpecVersion is 123, "Fixed buffer output conversion");
        new Span<byte>(native.ExtensionName, 256).Fill((byte)'x');
        read = new(in native);
        Assert(read.ExtensionName!.Length is 256, "Bounded missing terminator");
        NGXFeatureRequirementNative requirement = default;
        new Span<byte>(requirement.MinOSVersion, 255).Fill((byte)'y');
        NGXFeatureRequirement required = new(in requirement);
        Assert(required.MinOSVersion!.Length is 255, "Feature requirement bounded missing terminator");
        using NativeScope scope = new();
        byte* key = scope.AllocUtf8(Ngx.EParameterReserved00);
        Assert(new ReadOnlySpan<byte>(key, 3).SequenceEqual(new byte[] { 35, 0, 0 }), "SDK binary key");
        Assert(NativeTextHelper.ReadUtf8([65, 0, 66]) is "A" && NativeTextHelper.ReadUtf8([65, 66]) is "AB", "Bounded UTF-8 helper");

        Console.WriteLine("PASS output-only fixed buffers read within capacity and retain SDK binary key bytes");
    }
}
