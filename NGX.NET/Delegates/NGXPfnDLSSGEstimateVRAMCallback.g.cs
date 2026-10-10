namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public delegate NGXResult NGXPfnDLSSGEstimateVRAMCallback(uint motionDepthWidth, uint motionDepthHeight, uint colorWidth, uint colorHeight, uint colorFormat, uint motionFormat, uint depthFormat, uint hudlessFormat, uint uiFormat, out ulong estimatedBytes);
