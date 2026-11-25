namespace NGX.NET;

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXAppLogCallback(byte* arg0, NGXLoggingLevel arg1, NGXFeature arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXD3D11BufferAllocCallback(nint arg0, nint* arg1);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXD3D11Tex2DAllocCallback(nint arg0, nint* arg1);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXD3D12ResourceAllocCallback(nint arg0, int arg1, nint arg2, nint* arg3);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXDLSSGEstimateVRAMCallback(uint arg0, uint arg1, uint arg2, uint arg3, uint arg4, uint arg5, uint arg6, uint arg7, uint arg8, ulong* arg9);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXDLSSGGetCurrentSettingsCallback(NGXHandle arg0, NGXParameter arg1);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXDLSSGetOptimalSettingsCallback(NGXParameter arg0);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXDLSSGetStatsCallback(NGXParameter arg0);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetD(NGXParameter arg0, byte* arg1, double* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetD3D11Resource(NGXParameter arg0, byte* arg1, nint* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetD3D12Resource(NGXParameter arg0, byte* arg1, nint* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetF(NGXParameter arg0, byte* arg1, float* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetI(NGXParameter arg0, byte* arg1, int* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetUI(NGXParameter arg0, byte* arg1, uint* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetULL(NGXParameter arg0, byte* arg1, ulong* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate NGXResult NGXParameterGetVoidPointer(NGXParameter arg0, byte* arg1, void** arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetD(NGXParameter arg0, byte* arg1, double arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetD3D11Resource(NGXParameter arg0, byte* arg1, nint arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetD3D12Resource(NGXParameter arg0, byte* arg1, nint arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetF(NGXParameter arg0, byte* arg1, float arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetI(NGXParameter arg0, byte* arg1, int arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetUI(NGXParameter arg0, byte* arg1, uint arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetULL(NGXParameter arg0, byte* arg1, ulong arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXParameterSetVoidPointer(NGXParameter arg0, byte* arg1, void* arg2);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXProgressCallback(float arg0, byte* arg1);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXProgressCallbackC(float arg0, byte* arg1);

[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void NGXResourceReleaseCallback(nint arg0);
