#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnProgressCallbackC(float progress, [MarshalAs(UnmanagedType.I1)] ref bool shouldCancel);
