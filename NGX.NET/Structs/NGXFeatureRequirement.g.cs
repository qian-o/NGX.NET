namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXFeatureRequirement
{
    public NGXFeatureSupportResult FeatureSupported;

    public uint MinHWArchitecture;

    public MinOSVersionBuffer MinOSVersion;

    [InlineArray(255)]
    public struct MinOSVersionBuffer
    {
        private byte element;
    }
}
