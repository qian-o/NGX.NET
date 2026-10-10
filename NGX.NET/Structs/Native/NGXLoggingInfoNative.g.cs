namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXLoggingInfoNative
{
    [FieldOffset(0)]
    public nint LoggingCallback;

    [FieldOffset(8)]
    public NGXLoggingLevel MinimumLoggingLevel;

    [FieldOffset(12)]
    public Bool8 DisableOtherLoggingSinks;

    public NGXLoggingInfoNative(in NGXLoggingInfo value, NativeScope scope)
    {
        if (value.DisableOtherLoggingSinks && value.LoggingCallback is null)
        {
            throw new ArgumentException("A logging callback is required when disabling other logging sinks.", nameof(value));
        }

        NGXAppLogCallback? loggingCallback = CallbackGuard.Wrap(value.LoggingCallback);
        LoggingCallback = loggingCallback is null ? 0 : scope.Keep(loggingCallback);
        MinimumLoggingLevel = value.MinimumLoggingLevel;
        DisableOtherLoggingSinks = value.DisableOtherLoggingSinks;
    }
}
