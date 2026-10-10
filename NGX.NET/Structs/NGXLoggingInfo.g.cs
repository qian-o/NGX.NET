#nullable enable

namespace NGX.NET;

public struct NGXLoggingInfo
{
    public NGXAppLogCallback? LoggingCallback;

    public NGXLoggingLevel MinimumLoggingLevel;

    public bool DisableOtherLoggingSinks;

    internal unsafe NGXLoggingInfo(in NGXLoggingInfoNative native)
    {
        LoggingCallback = native.LoggingCallback is 0 ? null : Marshal.GetDelegateForFunctionPointer<NGXAppLogCallback>(native.LoggingCallback);
        MinimumLoggingLevel = native.MinimumLoggingLevel;
        DisableOtherLoggingSinks = native.DisableOtherLoggingSinks;
    }
}
