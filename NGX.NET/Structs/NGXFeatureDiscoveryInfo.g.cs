#nullable enable

namespace NGX.NET;

public struct NGXFeatureDiscoveryInfo
{
    public NGXVersion SDKVersion;

    public NGXFeature FeatureID;

    public NGXApplicationIdentifier Identifier;

    public string? ApplicationDataPath;

    public NGXFeatureCommonInfo? FeatureInfo;
}
