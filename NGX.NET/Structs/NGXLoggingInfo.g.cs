namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct NGXLoggingInfo
{
    public delegate* unmanaged[Cdecl]<byte*, NGXLoggingLevel, NGXFeature, void> LoggingCallback;

    public NGXLoggingLevel MinimumLoggingLevel;

    public Bool8 DisableOtherLoggingSinks;
}
