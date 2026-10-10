namespace Marshalling;

internal static unsafe class DiscoveryChecks
{
    internal static void Run()
    {
        NGXFeatureDiscoveryInfo value = Discovery();
        NGXFeatureDiscoveryInfoNative native = new(in value);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        NGXFeatureDiscoveryInfo restored = new(in native);
        Assert(restored.Identifier.V.ProjectDesc!.Value.EngineVersion is "1.0\U0001F680", "UTF-8 nested identity");
        Assert(restored.ApplicationDataPath == value.ApplicationDataPath, "Native-wide data path");
        Assert(restored.FeatureInfo!.Value.PathListInfo.Paths!.SequenceEqual(value.FeatureInfo!.Value.PathListInfo.Paths!), "Nested paths");
        native.Dispose();
        native.Dispose();
        Assert(native.FeatureInfo is null && native.ApplicationDataPath is null, "Native reset");
        Assert(restored.FeatureInfo.Value.PathListInfo.Paths![0] is "/runtime/\u5E93", "Managed result independence");
        value.Identifier = new()
        {
            IdentifierType = NGXApplicationIdentifierType.ApplicationId,
            V = new()
            {
                ApplicationId = ulong.MaxValue
            }
        };
        native = new(in value);
        Assert(native.Identifier.V.ApplicationId is ulong.MaxValue, "Union integer arm");
        native.Dispose();

        Console.WriteLine("PASS nested constructor, Unicode paths and union round trip survive compacting GC");
    }
}
