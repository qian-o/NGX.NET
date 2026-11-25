namespace NGX.NET;

[StructLayout(LayoutKind.Sequential)]
public struct NGXFeatureCommonInfo
{
    public NGXPathListInfo PathListInfo;

    public nint InternalData;

    public NGXLoggingInfo LoggingInfo;
}
