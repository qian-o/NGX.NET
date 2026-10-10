namespace NGX.NET;

[Flags]
public enum NGXFeatureSupportResult : int
{
    Supported = 0,

    CheckNotPresent = 1 << 0,

    DriverVersionUnsupported = 1 << 1,

    AdapterUnsupported = 1 << 2,

    OsVersionBelowMinimumSupported = 1 << 3,

    NotImplemented = 1 << 4,

    None = 0
}
