#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 56)]
internal unsafe struct NGXFeatureDiscoveryInfoNative : IDisposable
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

    public NGXFeatureDiscoveryInfoNative(in NGXFeatureDiscoveryInfo value)
    {
        this = default;

        try
        {
            SDKVersion = value.SDKVersion;
            FeatureID = value.FeatureID;
            Identifier = new(in value.Identifier);
            ApplicationDataPath = NGXMarshal.TextToPtr(value.ApplicationDataPath, NGXEncoding.NativeWide);

            if (value.FeatureInfo is NGXFeatureCommonInfo featureInfo)
            {
                FeatureInfo = NGXMarshal.AllocNative<NGXFeatureCommonInfoNative>(new(in featureInfo));
            }
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NGXMarshal.FreeNative(FeatureInfo);
        NGXMarshal.Free(ApplicationDataPath);
        Identifier.Dispose();
        this = default;
    }
}
