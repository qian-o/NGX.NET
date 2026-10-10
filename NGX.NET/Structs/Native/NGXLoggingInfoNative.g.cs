#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 16)]
internal unsafe struct NGXLoggingInfoNative : IDisposable
{
    [FieldOffset(0)]
    public nint LoggingCallback;

    [FieldOffset(8)]
    public NGXLoggingLevel MinimumLoggingLevel;

    [FieldOffset(12)]
    public Bool8 DisableOtherLoggingSinks;

    public NGXLoggingInfoNative(in NGXLoggingInfo value)
    {
        try
        {
            if (value.DisableOtherLoggingSinks && value.LoggingCallback is null)
            {
                throw new ArgumentException("A logging callback is required when disabling other logging sinks.", nameof(value));
            }

            LoggingCallback = NgxCallbacks.Acquire(value.LoggingCallback);
            MinimumLoggingLevel = value.MinimumLoggingLevel;
            DisableOtherLoggingSinks = value.DisableOtherLoggingSinks;
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        NgxCallbacks.Release(LoggingCallback);
        this = default;
    }
}
