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
        private static partial NGXResult InitWithProjectIDNative(byte* inProjectId, NGXEngineType inEngineType, byte* inEngineVersion, void* inApplicationDataPath, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

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

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXFeatureCreateParamsNative dlispCreateParametersNative = new(in dlispCreateParameters);
            NGXResult result = CreateDLISPExtNative(out handle, parameters, &dlispCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
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

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXCUDADLSSDCreateParamsNative dlssDCreateParametersNative = new(in dlssDCreateParameters);
            NGXResult result = CreateDLSSDExtNative(out handle, parameters, &dlssDCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
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
            handle = default;

            NGXCUDADeviceNative* pDevice = device is NGXCUDADevice deviceValue ? NativeLifetime.GetCudaDevice(deviceValue) : null;
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXCUDADLSSDCreateParamsNative dlssDCreateParametersNative = new(in dlssDCreateParameters);
            NGXResult result = CreateDLSSDExt1Native(pDevice, out handle, parameters, &dlssDCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
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
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlispEvalParametersScope = new();
            NGXCUDADLISPEvalParamsNative* pDlispEvalParameters = dlispEvalParametersScope.Alloc(new NGXCUDADLISPEvalParamsNative(in dlispEvalParameters, dlispEvalParametersScope));
            NGXResult result = EvaluateDLISPExtNative(handle, parameters, pDlispEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Cuda, parameters, "CUDA.EvaluateDLISPExt.pDlispEvalParams", dlispEvalParametersScope, result);

            return result;
        }

        public static NGXResult EvaluateDLSSDExt(NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDEvalParams dlssDEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssDEvalParametersScope = new();
            NGXCUDADLSSDEvalParamsNative* pDlssDEvalParameters = dlssDEvalParametersScope.Alloc(new NGXCUDADLSSDEvalParamsNative(in dlssDEvalParameters, dlssDEvalParametersScope));
            NGXResult result = EvaluateDLSSDExtNative(handle, parameters, pDlssDEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Cuda, parameters, "CUDA.EvaluateDLSSDExt.pInDlssDEvalParams", dlssDEvalParametersScope, result);

            return result;
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            parameters = default;

            NGXResult result = AllocateParametersNative(out parameters);
            if (result is not NGXResult.Success)
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

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

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
            handle = default;

            NGXCUDADeviceNative* pDevice = device is NGXCUDADevice deviceValue ? NativeLifetime.GetCudaDevice(deviceValue) : null;
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = CreateFeature1Native(pDevice, featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
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
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = DestroyParametersNative(parameters);

            if (result is NGXResult.Success)
            {
                NativeLifetime.Release(parameters);
            }

            return result;
        }

        public static NGXResult EvaluateFeature(NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallback? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallback? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureNative(featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
            GC.KeepAlive(guardedCallback);

            return result;
        }

        public static NGXResult EvaluateFeatureC(NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallbackC? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallbackC? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureCNative(featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
            GC.KeepAlive(guardedCallback);

            return result;
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            parameters = default;

            NGXResult result = GetCapabilityParametersNative(out parameters);
            if (result is not NGXResult.Success)
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
            supported = default;
            NGXFeatureRequirementNative supportedNative = default;

            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
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

            NGXResult result = GetParametersNative(out parameters);
            if (result is not NGXResult.Success)
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

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

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
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitNative(applicationId, scope.AllocWide(applicationDataPath), featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.Cuda, 0, scope, result);

            return result;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult Init1(ulong applicationId, string? applicationDataPath, NGXCUDADevice? device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            NGXCUDADeviceNative* pDevice = device is NGXCUDADevice deviceValue ? NativeLifetime.GetCudaDevice(deviceValue) : null;

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = Init1Native(applicationId, scope.AllocWide(applicationDataPath), pDevice, featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.Cuda, (nint)pDevice, scope, result);

            return result;
        }

        public static NGXResult Init1(ulong applicationId, string? applicationDataPath, in NGXCUDADevice device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init1(applicationId, applicationDataPath, (NGXCUDADevice?)device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitWithProjectIDNative(scope.AllocUtf8(projectId), engineType, scope.AllocUtf8(engineVersion), scope.AllocWide(applicationDataPath), featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.Cuda, 0, scope, result);

            return result;
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult ReleaseFeature(NGXHandle handle)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));

            return ReleaseFeatureNative(handle);
        }

        public static NGXResult Shutdown()
        {
            NGXResult result = ShutdownNative();

            if (result is NGXResult.Success)
            {
                NativeLifetime.Release(NGXGraphicsAPI.Cuda, 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(NGXCUDADevice? device)
        {
            NGXCUDADeviceNative* pDevice = device is NGXCUDADevice deviceValue ? NativeLifetime.GetCudaDevice(deviceValue) : null;
            NGXResult result = Shutdown1Native(pDevice);

            if (result is NGXResult.Success)
            {
                NativeLifetime.Release(NGXGraphicsAPI.Cuda, (nint)pDevice);
            }

            return result;
        }

        public static NGXResult Shutdown1(in NGXCUDADevice device)
        {
            return Shutdown1((NGXCUDADevice?)device);
        }
    }
}
