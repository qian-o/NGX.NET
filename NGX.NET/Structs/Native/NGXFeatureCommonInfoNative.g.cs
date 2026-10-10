#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal unsafe struct NGXFeatureCommonInfoNative : IDisposable
{
    [FieldOffset(0)]
    public NGXPathListInfoNative PathListInfo;

    [FieldOffset(16)]
    public nint InternalData;

    [FieldOffset(24)]
    public NGXLoggingInfoNative LoggingInfo;

    public NGXFeatureCommonInfoNative(in NGXFeatureCommonInfo value)
    {
        try
        {
            PathListInfo = new(in value.PathListInfo);
            InternalData = value.InternalData;
            LoggingInfo = new(in value.LoggingInfo);
        }
        catch
        {
            Dispose();

            throw;
        }
    }

    public void Dispose()
    {
        LoggingInfo.Dispose();
        PathListInfo.Dispose();
        this = default;
    }
}
