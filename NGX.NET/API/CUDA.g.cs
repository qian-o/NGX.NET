#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class CUDA
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLISPExtNative(out NGXHandle ppOutHandle, NGXParameter pInParams, NGXFeatureCreateParamsNative* pDlispCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSDExtNative(out NGXHandle ppOutHandle, NGXParameter pInParams, NGXCUDADLSSDCreateParamsNative* pInDlssDCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSDExt1Native(NGXCUDADeviceNative* inDevice, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXCUDADLSSDCreateParamsNative* pInDlssDCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLISPExtNative(NGXHandle pInHandle, NGXParameter pInParams, NGXCUDADLISPEvalParamsNative* pDlispEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSDExtNative(NGXHandle pInHandle, NGXParameter pInParams, NGXCUDADLSSDEvalParamsNative* pInDlssDEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult AllocateParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeatureNative(NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_CreateFeature1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeature1Native(NGXCUDADeviceNative* inDevice, NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult DestroyParametersNative(NGXParameter inParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureNative(NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureCNative(NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetCapabilityParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureRequirementsNative(int cudaDevice, NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out NGXFeatureRequirementNative outSupported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetScratchBufferSizeNative(NGXFeature inFeatureId, NGXParameter inParameters, out nuint outSizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitNative(ulong inApplicationId, void* inApplicationDataPath, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult Init1Native(ulong inApplicationId, void* inApplicationDataPath, NGXCUDADeviceNative* inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitWithProjectIDNative(sbyte* inProjectId, NGXEngineType inEngineType, sbyte* inEngineVersion, void* inApplicationDataPath, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ReleaseFeatureNative(NGXHandle inHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ShutdownNative();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult Shutdown1Native(NGXCUDADeviceNative* inDevice);

        static CUDA()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLISPExt(out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            handle = default;
            NGXFeatureCreateParamsNative dlispCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlispCreateParametersNative = new(in dlispCreateParameters);
                NGXResult result = CreateDLISPExtNative(out handle, parameters, &dlispCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                return result;
            }
            finally
            {
                dlispCreateParametersNative.Dispose();
            }
        }

        public static NGXHandle CreateDLISPExt(NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            NGXResult result = CreateDLISPExt(out NGXHandle handle, parameters, in dlispCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateDLISPExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt(out NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            handle = default;
            NGXCUDADLSSDCreateParamsNative dlssDCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssDCreateParametersNative = new(in dlssDCreateParameters);
                NGXResult result = CreateDLSSDExtNative(out handle, parameters, &dlssDCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                return result;
            }
            finally
            {
                dlssDCreateParametersNative.Dispose();
            }
        }

        public static NGXHandle CreateDLSSDExt(NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt(out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateDLSSDExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt1(NGXCUDADevice? device, out NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            NGXCUDADeviceNative* pDevice = null;
            handle = default;
            NGXCUDADLSSDCreateParamsNative dlssDCreateParametersNative = default;
            bool cudaSucceeded = false;

            try
            {
                if (device is NGXCUDADevice deviceValue)
                {
                    pDevice = NgxLifetime.CudaDevice(deviceValue);
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssDCreateParametersNative = new(in dlssDCreateParameters);
                NGXResult result = CreateDLSSDExt1Native(pDevice, out handle, parameters, &dlssDCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                cudaSucceeded = result is NGXResult.Success;

                return result;
            }
            finally
            {
                dlssDCreateParametersNative.Dispose();
                NgxLifetime.FinishCudaDevice((nint)pDevice, cudaSucceeded);
            }
        }

        public static NGXHandle CreateDLSSDExt1(NGXCUDADevice? device, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt1(device, out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateDLSSDExt1");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt1(in NGXCUDADevice device, out NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            return CreateDLSSDExt1((NGXCUDADevice?)device, out handle, parameters, in dlssDCreateParameters);
        }

        public static NGXHandle CreateDLSSDExt1(in NGXCUDADevice device, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt1((NGXCUDADevice?)device, out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateDLSSDExt1");
            }

            return handle;
        }

        public static NGXResult EvaluateDLISPExt(NGXHandle handle, NGXParameter parameters, in NGXCUDADLISPEvalParams dlispEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXCUDADLISPEvalParamsNative dlispEvalParametersNative = default;
            NGXCUDADLISPEvalParamsNative* pDlispEvalParameters = null;

            try
            {
                if (handle.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(handle));
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlispEvalParametersNative = new(in dlispEvalParameters);
                pDlispEvalParameters = storage!.Take(ref dlispEvalParametersNative);
                NgxLifetime.BeginParameters(parameters.Value, "CUDA.EvaluateDLISPExt", storage!);
                attached = true;
                result = EvaluateDLISPExtNative(handle, parameters, pDlispEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "CUDA.EvaluateDLISPExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlispEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSDExt(NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDEvalParams dlssDEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXCUDADLSSDEvalParamsNative dlssDEvalParametersNative = default;
            NGXCUDADLSSDEvalParamsNative* pDlssDEvalParameters = null;

            try
            {
                if (handle.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(handle));
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssDEvalParametersNative = new(in dlssDEvalParameters);
                pDlssDEvalParameters = storage!.Take(ref dlssDEvalParametersNative);
                NgxLifetime.BeginParameters(parameters.Value, "CUDA.EvaluateDLSSDExt", storage!);
                attached = true;
                result = EvaluateDLSSDExtNative(handle, parameters, pDlssDEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "CUDA.EvaluateDLSSDExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssDEvalParametersNative.Dispose();
            }
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            parameters = default;
            NgxLifetime.PrepareParameters();
            NGXResult result = AllocateParametersNative(out parameters);
            if (result is NGXResult.Success)
            {
                NgxLifetime.RegisterParameters("CUDA", parameters.Value);
            }
            else
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter AllocateParameters()
        {
            NGXResult result = AllocateParameters(out NGXParameter parameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.AllocateParameters");
            }

            return parameters;
        }

        public static NGXResult CreateFeature(NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = CreateFeatureNative(featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature(featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateFeature");
            }

            return handle;
        }

        public static NGXResult CreateFeature1(NGXCUDADevice? device, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            NGXCUDADeviceNative* pDevice = null;
            handle = default;
            bool cudaSucceeded = false;

            try
            {
                if (device is NGXCUDADevice deviceValue)
                {
                    pDevice = NgxLifetime.CudaDevice(deviceValue);
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                NGXResult result = CreateFeature1Native(pDevice, featureID, parameters, out handle);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                cudaSucceeded = result is NGXResult.Success;

                return result;
            }
            finally
            {
                NgxLifetime.FinishCudaDevice((nint)pDevice, cudaSucceeded);
            }
        }

        public static NGXHandle CreateFeature1(NGXCUDADevice? device, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature1(device, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateFeature1");
            }

            return handle;
        }

        public static NGXResult CreateFeature1(in NGXCUDADevice device, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            return CreateFeature1((NGXCUDADevice?)device, featureID, parameters, out handle);
        }

        public static NGXHandle CreateFeature1(in NGXCUDADevice device, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature1((NGXCUDADevice?)device, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.CreateFeature1");
            }

            return handle;
        }

        public static NGXResult DestroyParameters(NGXParameter parameters)
        {
            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = DestroyParametersNative(parameters);

            if (result is NGXResult.Success)
            {
                NgxLifetime.ReleaseParameters(parameters.Value, destroyed: true);
            }

            return result;
        }

        public static NGXResult EvaluateFeature(NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallback? callback)
        {
            nint callbackNative = 0;

            try
            {
                if (featureHandle.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(featureHandle));
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                callbackNative = NgxCallbacks.Acquire(callback);
                NGXResult result = EvaluateFeatureNative(featureHandle, parameters, callbackNative);

                return result;
            }
            finally
            {
                NgxCallbacks.Release(callbackNative);
            }
        }

        public static NGXResult EvaluateFeatureC(NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallbackC? callback)
        {
            nint callbackNative = 0;

            try
            {
                if (featureHandle.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(featureHandle));
                }

                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                callbackNative = NgxCallbacks.Acquire(callback);
                NGXResult result = EvaluateFeatureCNative(featureHandle, parameters, callbackNative);

                return result;
            }
            finally
            {
                NgxCallbacks.Release(callbackNative);
            }
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            parameters = default;
            NgxLifetime.PrepareParameters();
            NGXResult result = GetCapabilityParametersNative(out parameters);
            if (result is NGXResult.Success)
            {
                NgxLifetime.RegisterParameters("CUDA", parameters.Value);
            }
            else
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetCapabilityParameters()
        {
            NGXResult result = GetCapabilityParameters(out NGXParameter parameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.GetCapabilityParameters");
            }

            return parameters;
        }

        public static NGXResult GetFeatureRequirements(int cudaDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = default;
            NGXFeatureRequirementNative supportedNative = default;
            supported = default;

            try
            {
                featureDiscoveryInfoNative = new(in featureDiscoveryInfo);
                NGXResult result = GetFeatureRequirementsNative(cudaDevice, &featureDiscoveryInfoNative, out supportedNative);
                if (result is NGXResult.Success)
                {
                    supported = new(in supportedNative);
                }
                else
                {
                    supported = default;
                }

                return result;
            }
            finally
            {
                featureDiscoveryInfoNative.Dispose();
            }
        }

        public static NGXFeatureRequirement GetFeatureRequirements(int cudaDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureRequirements(cudaDevice, in featureDiscoveryInfo, out NGXFeatureRequirement supported);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.GetFeatureRequirements");
            }

            return supported;
        }

        public static NGXResult GetParameters(out NGXParameter parameters)
        {
            parameters = default;
            NgxLifetime.PrepareParameters();
            NGXResult result = GetParametersNative(out parameters);
            if (result is NGXResult.Success)
            {
                NgxLifetime.RegisterParameters("CUDA", parameters.Value);
            }
            else
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetParameters()
        {
            NGXResult result = GetParameters(out NGXParameter parameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.GetParameters");
            }

            return parameters;
        }

        public static NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, out nuint sizeInBytes)
        {
            sizeInBytes = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = GetScratchBufferSizeNative(featureId, parameters, out sizeInBytes);
            if (result is not NGXResult.Success)
            {
                sizeInBytes = default;
            }

            return result;
        }

        public static nuint GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters)
        {
            NGXResult result = GetScratchBufferSize(featureId, parameters, out nuint sizeInBytes);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.CUDA.GetScratchBufferSize");
            }

            return sizeInBytes;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            void* pApplicationDataPath = null;
            NGXFeatureCommonInfoNative featureInfoNative = default;
            NGXFeatureCommonInfoNative* pFeatureInfo = null;

            try
            {
                pApplicationDataPath = storage!.String(applicationDataPath, NGXEncoding.NativeWide);

                if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
                {
                    featureInfoNative = new(in featureInfoValue);
                }

                pFeatureInfo = featureInfo.HasValue ? storage!.Take(ref featureInfoNative) : null;
                NgxLifetime.BeginInitialization("CUDA", 0, storage!);
                attached = true;
                result = InitNative(applicationId, pApplicationDataPath, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("CUDA", 0, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult Init1(ulong applicationId, string? applicationDataPath, NGXCUDADevice? device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            void* pApplicationDataPath = null;
            NGXCUDADeviceNative* pDevice = null;
            NGXFeatureCommonInfoNative featureInfoNative = default;
            NGXFeatureCommonInfoNative* pFeatureInfo = null;
            bool cudaSucceeded = false;

            try
            {
                pApplicationDataPath = storage!.String(applicationDataPath, NGXEncoding.NativeWide);

                if (device is NGXCUDADevice deviceValue)
                {
                    pDevice = NgxLifetime.CudaDevice(deviceValue);
                }

                if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
                {
                    featureInfoNative = new(in featureInfoValue);
                }

                pFeatureInfo = featureInfo.HasValue ? storage!.Take(ref featureInfoNative) : null;
                NgxLifetime.BeginInitialization("CUDA", (nint)pDevice, storage!);
                attached = true;
                result = Init1Native(applicationId, pApplicationDataPath, pDevice, pFeatureInfo, sdkVersion);
                returned = true;
                cudaSucceeded = result is NGXResult.Success;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("CUDA", (nint)pDevice, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
                NgxLifetime.FinishCudaDevice((nint)pDevice, cudaSucceeded);
            }
        }

        public static NGXResult Init1(ulong applicationId, string? applicationDataPath, in NGXCUDADevice device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init1(applicationId, applicationDataPath, (NGXCUDADevice?)device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            sbyte* pProjectId = null;
            sbyte* pEngineVersion = null;
            void* pApplicationDataPath = null;
            NGXFeatureCommonInfoNative featureInfoNative = default;
            NGXFeatureCommonInfoNative* pFeatureInfo = null;

            try
            {
                pProjectId = (sbyte*)storage!.String(projectId, NGXEncoding.Utf8);
                pEngineVersion = (sbyte*)storage!.String(engineVersion, NGXEncoding.Utf8);
                pApplicationDataPath = storage!.String(applicationDataPath, NGXEncoding.NativeWide);

                if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
                {
                    featureInfoNative = new(in featureInfoValue);
                }

                pFeatureInfo = featureInfo.HasValue ? storage!.Take(ref featureInfoNative) : null;
                NgxLifetime.BeginInitialization("CUDA", 0, storage!);
                attached = true;
                result = InitWithProjectIDNative(pProjectId, engineType, pEngineVersion, pApplicationDataPath, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("CUDA", 0, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult ReleaseFeature(NGXHandle handle)
        {
            if (handle.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(handle));
            }

            return ReleaseFeatureNative(handle);
        }

        public static NGXResult Shutdown()
        {
            NGXResult result = ShutdownNative();

            if (result is NGXResult.Success)
            {
                NgxLifetime.Shutdown("CUDA", 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(NGXCUDADevice? device)
        {
            NGXCUDADeviceNative* pDevice = null;
            bool cudaSucceeded = false;

            try
            {
                if (device is NGXCUDADevice deviceValue)
                {
                    pDevice = NgxLifetime.CudaDevice(deviceValue);
                }

                NGXResult result = Shutdown1Native(pDevice);

                if (result is NGXResult.Success)
                {
                    NgxLifetime.Shutdown("CUDA", (nint)pDevice);
                }

                return result;
            }
            finally
            {
                NgxLifetime.FinishCudaDevice((nint)pDevice, cudaSucceeded);
            }
        }

        public static NGXResult Shutdown1(in NGXCUDADevice device)
        {
            return Shutdown1((NGXCUDADevice?)device);
        }
    }
}
