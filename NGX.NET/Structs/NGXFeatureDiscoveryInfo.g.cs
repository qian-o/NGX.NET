#nullable enable

namespace NGX.NET;

public struct NGXFeatureDiscoveryInfo
{
    public NGXVersion SDKVersion;

    public NGXFeature FeatureID;

    public NGXApplicationIdentifier Identifier;

    public string? ApplicationDataPath;

    public NGXFeatureCommonInfo? FeatureInfo;

    internal unsafe NGXFeatureDiscoveryInfo(in NGXFeatureDiscoveryInfoNative native)
    {
        SDKVersion = native.SDKVersion;
        FeatureID = native.FeatureID;
        Identifier = new(in native.Identifier);
        ApplicationDataPath = NGXMarshal.PtrToString(native.ApplicationDataPath, NGXEncoding.NativeWide);
        FeatureInfo = native.FeatureInfo is null ? null : new NGXFeatureCommonInfo(in *native.FeatureInfo);
    }
}
