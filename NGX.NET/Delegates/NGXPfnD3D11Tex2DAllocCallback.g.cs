#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnD3D11Tex2DAllocCallback(nint description, out nint texture);
