namespace Marshalling;

internal static unsafe class DiscoveryChecks
{
    internal static void Run()
    {
        using NativeScope scope = new();
        NGXFeatureDiscoveryInfo value = Discovery();
        NGXFeatureDiscoveryInfoNative native = new(in value, scope);
        GC.Collect(2, GCCollectionMode.Forced, true, true);
        Assert(Marshal.PtrToStringUTF8((nint)native.Identifier.V.ProjectDesc.EngineVersion) is "1.0\U0001F680", "UTF-8 nested identity");
        Assert(NativeTextHelper.ReadWide(native.ApplicationDataPath) == value.ApplicationDataPath, "Native-wide data path");
        Assert(native.FeatureInfo->PathListInfo.Length == value.FeatureInfo!.Value.PathListInfo.Paths!.Length, "Nested path count");

        for (int i = 0; i < native.FeatureInfo->PathListInfo.Length; i++)
        {
            Assert(NativeTextHelper.ReadWide(native.FeatureInfo->PathListInfo.Path[i]) == value.FeatureInfo.Value.PathListInfo.Paths[i], "Nested path contents");
        }

        string firstPath = NativeTextHelper.ReadWide(native.FeatureInfo->PathListInfo.Path[0])!;
        scope.Dispose();
        scope.Dispose();
        Assert(scope.IsDisposed && firstPath is "/runtime/\u5E93", "Scope cleanup and managed string independence");
        value.Identifier = new()
        {
            IdentifierType = NGXApplicationIdentifierType.ApplicationId,
            V = new()
            {
                ApplicationId = ulong.MaxValue
            }
        };
        using NativeScope identifierScope = new();
        NGXApplicationIdentifierNative identifier = new(in value.Identifier, identifierScope);
        Assert(identifier.V.ApplicationId is ulong.MaxValue, "Union integer arm");

        Console.WriteLine("PASS nested conversion and Unicode paths survive compacting GC and scope cleanup");
    }
}
