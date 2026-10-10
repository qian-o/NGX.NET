#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 264)]
internal unsafe struct NGXFeatureRequirementNative : IDisposable
{
    [FieldOffset(0)]
    public NGXFeatureSupportResult FeatureSupported;

    [FieldOffset(4)]
    public uint MinHWArchitecture;

    [FieldOffset(8)]
    public fixed sbyte MinOSVersion[255];

    public NGXFeatureRequirementNative(in NGXFeatureRequirement value)
    {
        this = default;

        try
        {
            FeatureSupported = value.FeatureSupported;
            MinHWArchitecture = value.MinHWArchitecture;

            fixed (sbyte* buffer = MinOSVersion)
            {
                NGXMarshal.WriteUtf8(value.MinOSVersion, new Span<byte>(buffer, 255));
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
        this = default;
    }
}
