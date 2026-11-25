namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXFeatureDiscoveryInfo
{
    public NGXVersion SDKVersion;

    public NGXFeature FeatureID;

    public NGXApplicationIdentifier Identifier;

    public void* ApplicationDataPath;

    public NGXFeatureCommonInfo* FeatureInfo;
}
