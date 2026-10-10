#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class Parameter
    {
        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetDNative(NGXParameter inParameter, string inName, out double outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d11Resource", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetD3d11ResourceNative(NGXParameter inParameter, string inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetD3d12Resource", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetD3d12ResourceNative(NGXParameter inParameter, string inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetF", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFNative(NGXParameter inParameter, string inName, out float outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetI", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetINative(NGXParameter inParameter, string inName, out int outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetUI", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetUINative(NGXParameter inParameter, string inName, out uint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetULL", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetULLNative(NGXParameter inParameter, string inName, out ulong outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_GetVoidPointer", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetVoidPointerNative(NGXParameter inParameter, string inName, out nint outValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetDNative(NGXParameter inParameter, string inName, double inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d11Resource", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetD3d11ResourceNative(NGXParameter inParameter, string inName, nint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetD3d12Resource", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetD3d12ResourceNative(NGXParameter inParameter, string inName, nint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetF", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetFNative(NGXParameter inParameter, string inName, float inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetI", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetINative(NGXParameter inParameter, string inName, int inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetUI", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetUINative(NGXParameter inParameter, string inName, uint inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetULL", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetULLNative(NGXParameter inParameter, string inName, ulong inValue);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_Parameter_SetVoidPointer", StringMarshalling = StringMarshalling.Utf8)]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial void SetVoidPointerNative(NGXParameter inParameter, string inName, void* inValue);

        static Parameter()
        {
            NativeLoader.Register();
        }

        public static NGXResult GetD(NGXParameter parameter, string name, out double value)
        {
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetDNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetD3d11ResourceNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetD3d12ResourceNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetFNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetINative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetUINative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetULLNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            value = default;

            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            NGXResult result = GetVoidPointerNative(parameter, name, out value);
            if (result is not NGXResult.Success)
            {
                value = default;
            }

            return result;
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
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetDNative(parameter, name, value);
        }

        public static void SetD3d11Resource(NGXParameter parameter, string name, nint value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetD3d11ResourceNative(parameter, name, value);
        }

        public static void SetD3d12Resource(NGXParameter parameter, string name, nint value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetD3d12ResourceNative(parameter, name, value);
        }

        public static void SetF(NGXParameter parameter, string name, float value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetFNative(parameter, name, value);
        }

        public static void SetI(NGXParameter parameter, string name, int value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetINative(parameter, name, value);
        }

        public static void SetUI(NGXParameter parameter, string name, uint value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetUINative(parameter, name, value);
        }

        public static void SetULL(NGXParameter parameter, string name, ulong value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetULLNative(parameter, name, value);
        }

        public static void SetVoidPointer(NGXParameter parameter, string name, nint value)
        {
            ArgumentNullException.ThrowIfNull((void*)parameter.Value, nameof(parameter));
            ArgumentNullException.ThrowIfNull(name);

            SetVoidPointerNative(parameter, name, (void*)value);
        }
    }
}
