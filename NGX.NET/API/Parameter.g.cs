#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetDNative(NGXParameter inParameter, sbyte* inName, out double outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d11Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetD3d11ResourceNative(NGXParameter inParameter, sbyte* inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d12Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetD3d12ResourceNative(NGXParameter inParameter, sbyte* inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFNative(NGXParameter inParameter, sbyte* inName, out float outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetINative(NGXParameter inParameter, sbyte* inName, out int outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetUI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetUINative(NGXParameter inParameter, sbyte* inName, out uint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetULL")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetULLNative(NGXParameter inParameter, sbyte* inName, out ulong outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetVoidPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetVoidPointerNative(NGXParameter inParameter, sbyte* inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetDNative(NGXParameter inParameter, sbyte* inName, double inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d11Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetD3d11ResourceNative(NGXParameter inParameter, sbyte* inName, nint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d12Resource")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetD3d12ResourceNative(NGXParameter inParameter, sbyte* inName, nint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetF")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetFNative(NGXParameter inParameter, sbyte* inName, float inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetINative(NGXParameter inParameter, sbyte* inName, int inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetUI")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetUINative(NGXParameter inParameter, sbyte* inName, uint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetULL")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetULLNative(NGXParameter inParameter, sbyte* inName, ulong inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetVoidPointer")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetVoidPointerNative(NGXParameter inParameter, sbyte* inName, void* inValue);

        static Parameter()
        {
            NativeLoader.Register();
        }

        public static NGXResult GetD(NGXParameter parameter, string name, out double value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetDNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static double GetD(NGXParameter parameter, string name)
        {
            NGXResult result = GetD(parameter, name, out double value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetD");
            }

            return value;
        }

        public static NGXResult GetD3d11Resource(NGXParameter parameter, string name, out nint value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetD3d11ResourceNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static nint GetD3d11Resource(NGXParameter parameter, string name)
        {
            NGXResult result = GetD3d11Resource(parameter, name, out nint value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetD3d11Resource");
            }

            return value;
        }

        public static NGXResult GetD3d12Resource(NGXParameter parameter, string name, out nint value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetD3d12ResourceNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static nint GetD3d12Resource(NGXParameter parameter, string name)
        {
            NGXResult result = GetD3d12Resource(parameter, name, out nint value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetD3d12Resource");
            }

            return value;
        }

        public static NGXResult GetF(NGXParameter parameter, string name, out float value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetFNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static float GetF(NGXParameter parameter, string name)
        {
            NGXResult result = GetF(parameter, name, out float value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetF");
            }

            return value;
        }

        public static NGXResult GetI(NGXParameter parameter, string name, out int value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetINative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static int GetI(NGXParameter parameter, string name)
        {
            NGXResult result = GetI(parameter, name, out int value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetI");
            }

            return value;
        }

        public static NGXResult GetUI(NGXParameter parameter, string name, out uint value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetUINative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static uint GetUI(NGXParameter parameter, string name)
        {
            NGXResult result = GetUI(parameter, name, out uint value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetUI");
            }

            return value;
        }

        public static NGXResult GetULL(NGXParameter parameter, string name, out ulong value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetULLNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static ulong GetULL(NGXParameter parameter, string name)
        {
            NGXResult result = GetULL(parameter, name, out ulong value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetULL");
            }

            return value;
        }

        public static NGXResult GetVoidPointer(NGXParameter parameter, string name, out nint value)
        {
            sbyte* pName = null;
            value = default;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                NGXResult result = GetVoidPointerNative(parameter, pName, out value);
                if (result is not NGXResult.Success)
                {
                    value = default;
                }

                return result;
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static nint GetVoidPointer(NGXParameter parameter, string name)
        {
            NGXResult result = GetVoidPointer(parameter, name, out nint value);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Parameter.GetVoidPointer");
            }

            return value;
        }

        public static void SetD(NGXParameter parameter, string name, double value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetDNative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetD3d11Resource(NGXParameter parameter, string name, nint value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetD3d11ResourceNative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetD3d12Resource(NGXParameter parameter, string name, nint value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetD3d12ResourceNative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetF(NGXParameter parameter, string name, float value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetFNative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetI(NGXParameter parameter, string name, int value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetINative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetUI(NGXParameter parameter, string name, uint value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetUINative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetULL(NGXParameter parameter, string name, ulong value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetULLNative(parameter, pName, value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }

        public static void SetVoidPointer(NGXParameter parameter, string name, nint value)
        {
            sbyte* pName = null;

            try
            {
                if (parameter.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameter));
                }

                ArgumentNullException.ThrowIfNull(name);
                pName = (sbyte*)NGXMarshal.StringToPtr(name, NGXEncoding.Utf8);
                SetVoidPointerNative(parameter, pName, (void*)value);
            }
            finally
            {
                NGXMarshal.Free(pName);
            }
        }
    }
}
