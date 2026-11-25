namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetD(NGXParameter parameter, byte* name, double* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d11Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetD3d11Resource(NGXParameter parameter, byte* name, nint* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d12Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetD3d12Resource(NGXParameter parameter, byte* name, nint* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetF(NGXParameter parameter, byte* name, float* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetI(NGXParameter parameter, byte* name, int* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetUI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetUI(NGXParameter parameter, byte* name, uint* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetULL")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetULL(NGXParameter parameter, byte* name, ulong* value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetVoidPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetVoidPointer(NGXParameter parameter, byte* name, void** value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetD(NGXParameter parameter, byte* name, double value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d11Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetD3d11Resource(NGXParameter parameter, byte* name, nint value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d12Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetD3d12Resource(NGXParameter parameter, byte* name, nint value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetF(NGXParameter parameter, byte* name, float value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetI(NGXParameter parameter, byte* name, int value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetUI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetUI(NGXParameter parameter, byte* name, uint value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetULL")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetULL(NGXParameter parameter, byte* name, ulong value);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetVoidPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial void SetVoidPointer(NGXParameter parameter, byte* name, void* value);

        static Parameter()
        {
            NativeLoader.Register();
        }

        public static NGXResult GetD(NGXParameter parameter, string name, out double value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (double* pValue = &value)
            {
                return GetD(parameter, pName, pValue);
            }
        }

        public static NGXResult GetD3d11Resource(NGXParameter parameter, string name, out nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (nint* pValue = &value)
            {
                return GetD3d11Resource(parameter, pName, pValue);
            }
        }

        public static NGXResult GetD3d12Resource(NGXParameter parameter, string name, out nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (nint* pValue = &value)
            {
                return GetD3d12Resource(parameter, pName, pValue);
            }
        }

        public static NGXResult GetF(NGXParameter parameter, string name, out float value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (float* pValue = &value)
            {
                return GetF(parameter, pName, pValue);
            }
        }

        public static NGXResult GetI(NGXParameter parameter, string name, out int value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (int* pValue = &value)
            {
                return GetI(parameter, pName, pValue);
            }
        }

        public static NGXResult GetUI(NGXParameter parameter, string name, out uint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (uint* pValue = &value)
            {
                return GetUI(parameter, pName, pValue);
            }
        }

        public static NGXResult GetULL(NGXParameter parameter, string name, out ulong value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (ulong* pValue = &value)
            {
                return GetULL(parameter, pName, pValue);
            }
        }

        public static NGXResult GetVoidPointer(NGXParameter parameter, string name, out nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            fixed (nint* pValue = &value)
            {
                return GetVoidPointer(parameter, pName, (void**)pValue);
            }
        }

        public static void SetD(NGXParameter parameter, string name, double value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetD(parameter, pName, value);
        }

        public static void SetD3d11Resource(NGXParameter parameter, string name, nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetD3d11Resource(parameter, pName, value);
        }

        public static void SetD3d12Resource(NGXParameter parameter, string name, nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetD3d12Resource(parameter, pName, value);
        }

        public static void SetF(NGXParameter parameter, string name, float value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetF(parameter, pName, value);
        }

        public static void SetI(NGXParameter parameter, string name, int value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetI(parameter, pName, value);
        }

        public static void SetUI(NGXParameter parameter, string name, uint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetUI(parameter, pName, value);
        }

        public static void SetULL(NGXParameter parameter, string name, ulong value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetULL(parameter, pName, value);
        }

        public static void SetVoidPointer(NGXParameter parameter, string name, nint value)
        {
            using NGXStrings strings = new();
            byte* pName = strings.AllocUtf8(name);

            SetVoidPointer(parameter, pName, (void*)value);
        }
    }
}
