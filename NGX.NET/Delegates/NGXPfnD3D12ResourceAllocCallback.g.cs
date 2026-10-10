#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnD3D12ResourceAllocCallback(nint description, int state, nint heap, out nint resource);
