#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate NGXResult NGXPfnDLSSGGetCurrentSettingsCallback(NGXHandle handle, NGXParameter parameters);
