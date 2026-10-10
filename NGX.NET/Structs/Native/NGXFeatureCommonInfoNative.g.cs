namespace NGX.NET;

[StructLayout(LayoutKind.Explicit, Size = 40)]
internal unsafe struct NGXFeatureCommonInfoNative(in NGXFeatureCommonInfo value, NativeScope scope)
{
    [FieldOffset(0)]
    public NGXPathListInfoNative PathListInfo = new(in value.PathListInfo, scope);

    [FieldOffset(16)]
    public nint InternalData = value.InternalData;

    [FieldOffset(24)]
    public NGXLoggingInfoNative LoggingInfo = new(in value.LoggingInfo, scope);
}
