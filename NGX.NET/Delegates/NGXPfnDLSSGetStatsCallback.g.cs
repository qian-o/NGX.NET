#nullable enable

namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate NGXResult NGXPfnDLSSGetStatsCallback(NGXParameter parameters);
