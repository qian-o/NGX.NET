#nullable enable

namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal unsafe struct NGXFeatureCommonInfoNative
{
    [FieldOffset(0)]
    public NGXPathListInfoNative PathListInfo;

    [FieldOffset(16)]
    public nint InternalData;

    [FieldOffset(24)]
    public NGXLoggingInfoNative LoggingInfo;

    public NGXFeatureCommonInfoNative(in NGXFeatureCommonInfo value, NativeScope scope)
    {
        PathListInfo = new(in value.PathListInfo, scope);
        InternalData = value.InternalData;
        LoggingInfo = new(in value.LoggingInfo, scope);
    }
}
