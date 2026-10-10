#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnResourceReleaseCallback(nint resource);
