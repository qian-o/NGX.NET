#nullable enable

namespace NGX.NET;

public struct NGXLoggingInfo
{
    public NGXAppLogCallback? LoggingCallback;

    public NGXLoggingLevel MinimumLoggingLevel;

    public bool DisableOtherLoggingSinks;
}
