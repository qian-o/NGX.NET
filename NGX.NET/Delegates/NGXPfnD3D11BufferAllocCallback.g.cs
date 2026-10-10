namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate void NGXPfnD3D11BufferAllocCallback(nint description, out nint buffer);
