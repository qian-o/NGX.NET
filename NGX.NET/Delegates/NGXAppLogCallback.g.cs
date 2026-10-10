#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXAppLogCallback([MarshalAs(UnmanagedType.LPUTF8Str)] string? message, NGXLoggingLevel loggingLevel, NGXFeature sourceComponent);
