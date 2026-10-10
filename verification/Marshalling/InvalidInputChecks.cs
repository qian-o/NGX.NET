namespace Marshalling;

internal static unsafe class InvalidInputChecks
{
    private static readonly string[] AcceptedPaths = ["bad\0path", "\uD800"];

    internal static void Run()
    {
        using NativeScope scope = new();
        NGXFeatureDiscoveryInfo value = Discovery();
        value.FeatureInfo = new()
        {
            PathListInfo = new()
            {
                Paths = ["first", null!]
            }
        };
        Throws<ArgumentNullException>(() => new NGXFeatureDiscoveryInfoNative(in value, scope));
        value.Identifier.IdentifierType = NGXApplicationIdentifierType.ApplicationId;
        Throws<ArgumentException>(() => new NGXFeatureDiscoveryInfoNative(in value, scope));
        NGXVKGBuffer buffer = new()
        {
            Attributes = new NGXResourceVK?[18]
        };
        Throws<ArgumentException>(() => new NGXVKGBufferNative(in buffer, scope));
        NGXResourceVKUnion union = new()
        {
            ImageViewInfo = new(),
            BufferInfo = new()
        };
        Throws<ArgumentException>(() => new NGXResourceVKUnionNative(in union));
        NGXLoggingInfo logging = new()
        {
            DisableOtherLoggingSinks = true
        };
        Throws<ArgumentException>(() => new NGXLoggingInfoNative(in logging, scope));
        Throws<ArgumentNullException>(static () => Ngx.Parameter.Reset(default));

        foreach (string path in AcceptedPaths)
        {
            NGXFeatureDiscoveryInfo accepted = Discovery();
            accepted.FeatureInfo = new()
            {
                PathListInfo = new()
                {
                    Paths = ["first", path]
                }
            };
            NGXFeatureDiscoveryInfoNative native = new(in accepted, scope);
            string expected = path.Contains('\0') ? "bad" : "\uFFFD";
            Assert(NativeTextHelper.ReadWide(native.FeatureInfo->PathListInfo.Path[1]) == expected, "Accepted NUL and invalid Unicode path semantics");
        }

        scope.Dispose();
        Assert(scope.IsDisposed, "Scope cleanup after partial constructor failure");

        Console.WriteLine("PASS bounded arrays and union guards reject invalid shapes; native text accepts NUL and invalid Unicode");
    }
}
