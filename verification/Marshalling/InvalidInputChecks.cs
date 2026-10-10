namespace Marshalling;

internal static class InvalidInputChecks
{
    private static readonly string[] InvalidPaths = ["bad\0path", "\uD800"];

    internal static void Run()
    {
        NGXFeatureDiscoveryInfo value = Discovery();
        value.FeatureInfo = new()
        {
            PathListInfo = new()
            {
                Paths = ["first", null!]
            }
        };
        Throws<ArgumentNullException>(() => new NGXFeatureDiscoveryInfoNative(in value));
        value.Identifier.IdentifierType = NGXApplicationIdentifierType.ApplicationId;
        Throws<ArgumentException>(() => new NGXFeatureDiscoveryInfoNative(in value));
        NGXVKGBuffer buffer = new()
        {
            Attributes = new NGXResourceVK?[18]
        };
        Throws<ArgumentException>(() => new NGXVKGBufferNative(in buffer));
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
        Throws<ArgumentException>(() => new NGXLoggingInfoNative(in logging));
        Throws<ArgumentException>(static () => Ngx.Parameter.Reset(default));

        foreach (string bad in InvalidPaths)
        {
            NGXFeatureDiscoveryInfo invalid = Discovery();
            invalid.FeatureInfo = new()
            {
                PathListInfo = new()
                {
                    Paths = ["first", bad]
                }
            };
            Throws<ArgumentException>(() => new NGXFeatureDiscoveryInfoNative(in invalid));
        }

        Console.WriteLine("PASS constructor failure, union discriminator and bounded arrays reject invalid input");
    }
}
