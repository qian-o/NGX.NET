#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class D3D12
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLISPExtNative(nint inCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXFeatureCreateParamsNative* pDlispCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSDExtNative(nint pInCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSDCreateParamsNative* pInDlssDCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSGNative(nint pInCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSGCreateParamsNative* pInDlssgCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSExtNative(nint pInCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSCreateParamsNative* pInDlssCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_ESTIMATE_VRAM_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EstimateVRAMDLSSGNative(NGXParameter inParams, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out nuint estimatedVRAMInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLISPExtNative(nint pInCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D12DLISPEvalParamsNative* pDlispEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSDExtNative(nint pInCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D12DLSSDEvalParamsNative* pInDlssDEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSGNative(nint pInCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D12DLSSGEvalParamsNative* pInDlssgEvalParams, NGXDLSSGOptEvalParamsNative* pInDlssgOptEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSExtNative(nint pInCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXD3D12DLSSEvalParamsNative* pInDlssEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult AllocateParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeatureNative(nint inCmdList, NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult DestroyParametersNative(NGXParameter inParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureNative(nint inCmdList, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureCNative(nint inCmdList, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetCapabilityParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureRequirementsNative(nint adapter, NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out NGXFeatureRequirementNative outSupported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetScratchBufferSizeNative(NGXFeature inFeatureId, NGXParameter inParameters, out nuint outSizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitNative(ulong inApplicationId, void* inApplicationDataPath, nint inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitWithProjectIDNative(byte* inProjectId, NGXEngineType inEngineType, byte* inEngineVersion, void* inApplicationDataPath, nint inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ReleaseFeatureNative(NGXHandle inHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ShutdownNative();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult Shutdown1Native(nint inDevice);

        static D3D12()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXFeatureCreateParamsNative dlispCreateParametersNative = new(in dlispCreateParameters);
            NGXResult result = CreateDLISPExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlispCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            CreateDLISPExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlispCreateParameters).CheckError("Ngx.D3D12.CreateDLISPExt");

            return handle;
        }

        public static NGXResult CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSDCreateParamsNative dlssDCreateParametersNative = new(in dlssDCreateParameters);
            NGXResult result = CreateDLSSDExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssDCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            CreateDLSSDExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssDCreateParameters).CheckError("Ngx.D3D12.CreateDLSSDExt");

            return handle;
        }

        public static NGXResult CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSGCreateParamsNative dlssgCreateParametersNative = new(in dlssgCreateParameters);
            NGXResult result = CreateDLSSGNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssgCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            CreateDLSSG(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssgCreateParameters).CheckError("Ngx.D3D12.CreateDLSSG");

            return handle;
        }

        public static NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSCreateParamsNative dlssCreateParametersNative = new(in dlssCreateParameters);
            NGXResult result = CreateDLSSExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            CreateDLSSExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssCreateParameters).CheckError("Ngx.D3D12.CreateDLSSExt");

            return handle;
        }

        public static NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out nuint estimatedVRAMInBytes)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = EstimateVRAMDLSSGNative(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, out estimatedVRAMInBytes);
            if (result.IsFailure)
            {
                estimatedVRAMInBytes = default;
            }

            return result;
        }

        public static nuint EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat)
        {
            EstimateVRAMDLSSG(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, out nuint estimatedVRAMInBytes).CheckError("Ngx.D3D12.EstimateVRAMDLSSG");

            return estimatedVRAMInBytes;
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLISPEvalParams dlispEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXD3D12DLISPEvalParamsNative dlispEvalParametersNative = new(in dlispEvalParameters);

            return EvaluateDLISPExtNative(commandList, handle, parameters, &dlispEvalParametersNative);
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSDEvalParams dlssDEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssDEvalParametersScope = new();
            NGXD3D12DLSSDEvalParamsNative* pDlssDEvalParameters = dlssDEvalParametersScope.Alloc(new NGXD3D12DLSSDEvalParamsNative(in dlssDEvalParameters, dlssDEvalParametersScope));
            NGXResult result = EvaluateDLSSDExtNative(commandList, handle, parameters, pDlssDEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "D3D12.EvaluateDLSSDExt.pInDlssDEvalParams", dlssDEvalParametersScope, result);

            return result;
        }

        public static NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams? dlssgOptEvalParameters)
        {
            NativeScope? dlssgOptEvalParametersScope = null;
            NGXDLSSGOptEvalParamsNative* pDlssgOptEvalParameters = null;

            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXD3D12DLSSGEvalParamsNative dlssgEvalParametersNative = new(in dlssgEvalParameters);

            if (dlssgOptEvalParameters is NGXDLSSGOptEvalParams dlssgOptEvalParametersValue)
            {
                dlssgOptEvalParametersScope = new();
                pDlssgOptEvalParameters = dlssgOptEvalParametersScope.Alloc(new NGXDLSSGOptEvalParamsNative(in dlssgOptEvalParametersValue));
            }

            NGXResult result = EvaluateDLSSGNative(commandList, handle, parameters, &dlssgEvalParametersNative, pDlssgOptEvalParameters);

            if (dlssgOptEvalParametersScope is not null)
            {
                NativeLifetime.Retain(NGXGraphicsAPI.D3D12, parameters, "D3D12.EvaluateDLSSG.pInDlssgOptEvalParams", dlssgOptEvalParametersScope, result);
            }

            return result;
        }

        public static NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSGEvalParams dlssgEvalParameters, in NGXDLSSGOptEvalParams dlssgOptEvalParameters)
        {
            return EvaluateDLSSG(commandList, handle, parameters, in dlssgEvalParameters, (NGXDLSSGOptEvalParams?)dlssgOptEvalParameters);
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSEvalParams dlssEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXD3D12DLSSEvalParamsNative dlssEvalParametersNative = new(in dlssEvalParameters);

            return EvaluateDLSSExtNative(commandList, handle, parameters, &dlssEvalParametersNative);
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            NGXResult result = AllocateParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter AllocateParameters()
        {
            AllocateParameters(out NGXParameter parameters).CheckError("Ngx.D3D12.AllocateParameters");

            return parameters;
        }

        public static NGXResult CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = CreateFeatureNative(commandList, featureID, parameters, out handle);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters)
        {
            CreateFeature(commandList, featureID, parameters, out NGXHandle handle).CheckError("Ngx.D3D12.CreateFeature");

            return handle;
        }

        public static NGXResult DestroyParameters(NGXParameter parameters)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = DestroyParametersNative(parameters);
            if (result.IsSuccess)
            {
                NativeLifetime.Release(parameters);
            }

            return result;
        }

        public static NGXResult EvaluateFeature(nint commandList, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallback? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallback? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureNative(commandList, featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
            GC.KeepAlive(guardedCallback);

            return result;
        }

        public static NGXResult EvaluateFeatureC(nint commandList, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallbackC? callback)
        {
            ArgumentNullException.ThrowIfNull((void*)featureHandle.Value, nameof(featureHandle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXPfnProgressCallbackC? guardedCallback = CallbackGuard.Wrap(callback);
            NGXResult result = EvaluateFeatureCNative(commandList, featureHandle, parameters, guardedCallback is null ? 0 : Marshal.GetFunctionPointerForDelegate(guardedCallback));
            GC.KeepAlive(guardedCallback);

            return result;
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            NGXResult result = GetCapabilityParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetCapabilityParameters()
        {
            GetCapabilityParameters(out NGXParameter parameters).CheckError("Ngx.D3D12.GetCapabilityParameters");

            return parameters;
        }

        public static NGXResult GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
            NGXResult result = GetFeatureRequirementsNative(adapter, &featureDiscoveryInfoNative, out NGXFeatureRequirementNative supportedNative);
            if (result.IsFailure)
            {
                supported = default;

                return result;
            }

            supported = new(in supportedNative);

            return result;
        }

        public static NGXFeatureRequirement GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            GetFeatureRequirements(adapter, in featureDiscoveryInfo, out NGXFeatureRequirement supported).CheckError("Ngx.D3D12.GetFeatureRequirements");

            return supported;
        }

        public static NGXResult GetParameters(out NGXParameter parameters)
        {
            NGXResult result = GetParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetParameters()
        {
            GetParameters(out NGXParameter parameters).CheckError("Ngx.D3D12.GetParameters");

            return parameters;
        }

        public static NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, out nuint sizeInBytes)
        {
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = GetScratchBufferSizeNative(featureId, parameters, out sizeInBytes);
            if (result.IsFailure)
            {
                sizeInBytes = default;
            }

            return result;
        }

        public static nuint GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters)
        {
            GetScratchBufferSize(featureId, parameters, out nuint sizeInBytes).CheckError("Ngx.D3D12.GetScratchBufferSize");

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
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, device, scope, result);

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
            NativeLifetime.Retain(NGXGraphicsAPI.D3D12, device, scope, result);

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
            if (result.IsSuccess)
            {
                NativeLifetime.Release(NGXGraphicsAPI.D3D12, 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(nint device)
        {
            NGXResult result = Shutdown1Native(device);
            if (result.IsSuccess)
            {
                NativeLifetime.Release(NGXGraphicsAPI.D3D12, device);
            }

            return result;
        }
    }
}
