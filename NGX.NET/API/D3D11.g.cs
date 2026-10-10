#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class D3D11
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLISPExtNative(nint pInCtx, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXFeatureCreateParamsNative* pDlispCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSDExtNative(nint pInCtx, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSDCreateParamsNative* pInDlssDCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSExtNative(nint pInCtx, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSCreateParamsNative* pInDlssCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLISPExtNative(nint pInCtx, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D11DLISPEvalParamsNative* pDlispEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSDExtNative(nint pInCtx, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D11DLSSDEvalParamsNative* pInDlssDEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSExtNative(nint pInCtx, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D11DLSSEvalParamsNative* pInDlssEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult AllocateParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeatureNative(nint inDevCtx, NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult DestroyParametersNative(NGXParameter inParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureNative(nint inDevCtx, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureCNative(nint inDevCtx, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetCapabilityParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureRequirementsNative(nint adapter, NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out NGXFeatureRequirementNative outSupported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetScratchBufferSizeNative(NGXFeature inFeatureId, NGXParameter inParameters, out nuint outSizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitNative(ulong inApplicationId, void* inApplicationDataPath, nint inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitWithProjectIDNative(byte* inProjectId, NGXEngineType inEngineType, byte* inEngineVersion, void* inApplicationDataPath, nint inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ReleaseFeatureNative(NGXHandle inHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ShutdownNative();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult Shutdown1Native(nint inDevice);

        static D3D11()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLISPExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXFeatureCreateParamsNative dlispCreateParametersNative = new(in dlispCreateParameters);
            NGXResult result = CreateDLISPExtNative(ctx, out handle, parameters, &dlispCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLISPExt(nint ctx, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            NGXResult result = CreateDLISPExt(ctx, out NGXHandle handle, parameters, in dlispCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D11.CreateDLISPExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSDCreateParamsNative dlssDCreateParametersNative = new(in dlssDCreateParameters);
            NGXResult result = CreateDLSSDExtNative(ctx, out handle, parameters, &dlssDCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSDExt(nint ctx, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt(ctx, out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D11.CreateDLSSDExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSCreateParamsNative dlssCreateParametersNative = new(in dlssCreateParameters);
            NGXResult result = CreateDLSSExtNative(ctx, out handle, parameters, &dlssCreateParametersNative);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSExt(nint ctx, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            NGXResult result = CreateDLSSExt(ctx, out NGXHandle handle, parameters, in dlssCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D11.CreateDLSSExt");
            }

            return handle;
        }

        public static NGXResult EvaluateDLISPExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLISPEvalParams dlispEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXD3D11DLISPEvalParamsNative dlispEvalParametersNative = new(in dlispEvalParameters);

            return EvaluateDLISPExtNative(ctx, handle, parameters, &dlispEvalParametersNative);
        }

        public static NGXResult EvaluateDLSSDExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLSSDEvalParams dlssDEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssDEvalParametersScope = new();
            NGXD3D11DLSSDEvalParamsNative* pDlssDEvalParameters = dlssDEvalParametersScope.Alloc(new NGXD3D11DLSSDEvalParamsNative(in dlssDEvalParameters, dlssDEvalParametersScope));
            NGXResult result = EvaluateDLSSDExtNative(ctx, handle, parameters, pDlssDEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.D3D11, parameters, "D3D11.EvaluateDLSSDExt.pInDlssDEvalParams", dlssDEvalParametersScope, result);

            return result;
        }

        public static NGXResult EvaluateDLSSExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLSSEvalParams dlssEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXD3D11DLSSEvalParamsNative dlssEvalParametersNative = new(in dlssEvalParameters);

            return EvaluateDLSSExtNative(ctx, handle, parameters, &dlssEvalParametersNative);
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
                throw new NGXException(result, "Ngx.D3D11.AllocateParameters");
            }

            return parameters;
        }

        public static NGXResult CreateFeature(nint deviceCtx, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = CreateFeatureNative(deviceCtx, featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(nint deviceCtx, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature(deviceCtx, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D11.CreateFeature");
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

        public static NGXResult EvaluateFeature(nint deviceCtx, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallback? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallback? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureNative(deviceCtx, featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
            GC.KeepAlive(guardedCallback);

            return result;
        }

        public static NGXResult EvaluateFeatureC(nint deviceCtx, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallbackC? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallbackC? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureCNative(deviceCtx, featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
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
                throw new NGXException(result, "Ngx.D3D11.GetCapabilityParameters");
            }

            return parameters;
        }

        public static NGXResult GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            supported = default;
            NGXFeatureRequirementNative supportedNative = default;

            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
            NGXResult result = GetFeatureRequirementsNative(adapter, &featureDiscoveryInfoNative, out supportedNative);
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

        public static NGXFeatureRequirement GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureRequirements(adapter, in featureDiscoveryInfo, out NGXFeatureRequirement supported);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D11.GetFeatureRequirements");
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
                throw new NGXException(result, "Ngx.D3D11.GetParameters");
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
                throw new NGXException(result, "Ngx.D3D11.GetScratchBufferSize");
            }

            return sizeInBytes;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitNative(applicationId, scope.AllocWide(applicationDataPath), device, featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.D3D11, device, scope, result);

            return result;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitWithProjectIDNative(scope.AllocUtf8(projectId), engineType, scope.AllocUtf8(engineVersion), scope.AllocWide(applicationDataPath), device, featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.D3D11, device, scope, result);

            return result;
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
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
                NativeLifetime.Release(NGXGraphicsAPI.D3D11, 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(nint device)
        {
            NGXResult result = Shutdown1Native(device);

            if (result is NGXResult.Success)
            {
                NativeLifetime.Release(NGXGraphicsAPI.D3D11, device);
            }

            return result;
        }
    }
}
