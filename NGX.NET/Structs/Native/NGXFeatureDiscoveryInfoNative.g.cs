namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXFeatureDiscoveryInfoNative(in NGXFeatureDiscoveryInfo value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXVersion SDKVersion = value.SDKVersion;

    [FieldOffset(4)]
    public NGXFeature FeatureID = value.FeatureID;

    [FieldOffset(8)]
    public NGXApplicationIdentifierNative Identifier = new(in value.Identifier, scope);

    [FieldOffset(40)]
    public void* ApplicationDataPath = scope.AllocWide(value.ApplicationDataPath);

    [FieldOffset(48)]
    public NGXFeatureCommonInfoNative* FeatureInfo = value.FeatureInfo is NGXFeatureCommonInfo featureInfo ? scope.Alloc(new NGXFeatureCommonInfoNative(in featureInfo, scope)) : null;
}
