#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXFeatureDiscoveryInfoNative
{
    [FieldOffset(0)]
    public NGXVersion SDKVersion;

    [FieldOffset(4)]
    public NGXFeature FeatureID;

    [FieldOffset(8)]
    public NGXApplicationIdentifierNative Identifier;

    [FieldOffset(40)]
    public void* ApplicationDataPath;

    [FieldOffset(48)]
    public NGXFeatureCommonInfoNative* FeatureInfo;

    public NGXFeatureDiscoveryInfoNative(in NGXFeatureDiscoveryInfo value, NativeScope scope)
    {
        SDKVersion = value.SDKVersion;
        FeatureID = value.FeatureID;
        Identifier = new(in value.Identifier, scope);
        ApplicationDataPath = scope.AllocWide(value.ApplicationDataPath);
        FeatureInfo = value.FeatureInfo is NGXFeatureCommonInfo featureInfo ? scope.Alloc(new NGXFeatureCommonInfoNative(in featureInfo, scope)) : null;
    }
}
