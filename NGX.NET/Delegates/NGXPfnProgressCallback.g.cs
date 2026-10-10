#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnProgressCallback(float progress, [MarshalAs(UnmanagedType.I1)] ref bool shouldCancel);
