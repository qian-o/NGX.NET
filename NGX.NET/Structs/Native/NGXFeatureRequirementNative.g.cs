#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 264)]
internal unsafe struct NGXFeatureRequirementNative
{
    [FieldOffset(0)]
    public NGXFeatureSupportResult FeatureSupported;

    [FieldOffset(4)]
    public uint MinHWArchitecture;

    [FieldOffset(8)]
    public fixed byte MinOSVersion[255];
}
