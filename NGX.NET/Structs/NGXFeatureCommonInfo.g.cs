#nullable enable

namespace NGX.NET;

public struct NGXFeatureCommonInfo
{
    public NGXPathListInfo PathListInfo;

    public nint InternalData;

    public NGXLoggingInfo LoggingInfo;

    internal unsafe NGXFeatureCommonInfo(in NGXFeatureCommonInfoNative native)
    {
        PathListInfo = new(in native.PathListInfo);
        InternalData = native.InternalData;
        LoggingInfo = new(in native.LoggingInfo);
    }
}
