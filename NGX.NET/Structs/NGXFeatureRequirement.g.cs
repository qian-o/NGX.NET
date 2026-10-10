#nullable enable

namespace NGX.NET;

public struct NGXFeatureRequirement
{
    public NGXFeatureSupportResult FeatureSupported;

    public uint MinHWArchitecture;

    public string? MinOSVersion;

    internal unsafe NGXFeatureRequirement(in NGXFeatureRequirementNative native)
    {
        FeatureSupported = native.FeatureSupported;
        MinHWArchitecture = native.MinHWArchitecture;

        fixed (byte* buffer = native.MinOSVersion)
        {
            MinOSVersion = NativeTextHelper.ReadUtf8(new ReadOnlySpan<byte>(buffer, 255));
        }
    }
}
