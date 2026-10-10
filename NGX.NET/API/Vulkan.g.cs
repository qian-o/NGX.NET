#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class Vulkan
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_CREATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSGNative(nint pInCmdBuf, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSGCreateParamsNative* pInDlssgCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_ESTIMATE_VRAM_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EstimateVRAMDLSSGNative(NGXParameter inParams, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out nuint estimatedVRAMInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_EVALUATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSGNative(nint pInCmdBuf, NGXHandle pInHandle, NGXParameter pInParams, NGXVKDLSSGEvalParamsNative* pInDlssgEvalParams, NGXDLSSGOptEvalParamsNative* pInDlssgOptEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLISPExtNative(nint inCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXFeatureCreateParamsNative* pInDlispCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSSD_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSDExt1Native(nint inDevice, nint inCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSDCreateParamsNative* pInDlssDCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSExtNative(nint inCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSCreateParamsNative* pInDlssCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateDLSSExt1Native(nint inDevice, nint inCmdList, uint inCreationNodeMask, uint inVisibilityNodeMask, out NGXHandle ppOutHandle, NGXParameter pInParams, NGXDLSSCreateParamsNative* pInDlssCreateParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLISPExtNative(nint inCmdList, NGXHandle inHandle, NGXParameter pInParams, NGXVKDLISPEvalParamsNative* pInDlispEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSDExtNative(nint inCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXVKDLSSDEvalParamsNative* pInDlssDEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateDLSSExtNative(nint inCmdList, NGXHandle pInHandle, NGXParameter pInParams, NGXVKDLSSEvalParamsNative* pInDlssEvalParams);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult AllocateParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeatureNative(nint inCmdBuffer, NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_CreateFeature1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult CreateFeature1Native(nint inDevice, nint inCmdList, NGXFeature inFeatureID, NGXParameter inParameters, out NGXHandle outHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult DestroyParametersNative(NGXParameter inParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureNative(nint inCmdList, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult EvaluateFeatureCNative(nint inCmdList, NGXHandle inFeatureHandle, NGXParameter inParameters, nint inCallback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetCapabilityParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureDeviceExtensionRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureDeviceExtensionRequirementsNative(nint instance, nint physicalDevice, NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out uint outExtensionCount, out NGXVkExtensionPropertiesNative* outExtensionProperties);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureInstanceExtensionRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureInstanceExtensionRequirementsNative(NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out uint outExtensionCount, out NGXVkExtensionPropertiesNative* outExtensionProperties);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetFeatureRequirementsNative(nint instance, nint physicalDevice, NGXFeatureDiscoveryInfoNative* featureDiscoveryInfo, out NGXFeatureRequirementNative outSupported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetParametersNative(out NGXParameter outParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetScratchBufferSizeNative(NGXFeature inFeatureId, NGXParameter inParameters, out nuint outSizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitNative(ulong inApplicationId, void* inApplicationDataPath, nint inInstance, nint inPD, nint inDevice, nint inGIPA, nint inGDPA, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult InitWithProjectIDNative(byte* inProjectId, NGXEngineType inEngineType, byte* inEngineVersion, void* inApplicationDataPath, nint inInstance, nint inPD, nint inDevice, nint inGIPA, nint inGDPA, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ReleaseFeatureNative(NGXHandle inHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_RequiredExtensions")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult RequiredExtensionsNative(out uint outInstanceExtCount, out byte** outInstanceExts, out uint outDeviceExtCount, out byte** outDeviceExts);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ShutdownNative();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult Shutdown1Native(nint inDevice);

        static Vulkan()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLSSG(nint commandBuffer, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSGCreateParamsNative dlssgCreateParametersNative = new(in dlssgCreateParameters);
            NGXResult result = CreateDLSSGNative(commandBuffer, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssgCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSG(nint commandBuffer, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            CreateDLSSG(commandBuffer, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssgCreateParameters).CheckError("Ngx.Vulkan.CreateDLSSG");

            return handle;
        }

        public static NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out nuint estimatedVRAMInBytes)
        {
            estimatedVRAMInBytes = default;

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
            EstimateVRAMDLSSG(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, out nuint estimatedVRAMInBytes).CheckError("Ngx.Vulkan.EstimateVRAMDLSSG");

            return estimatedVRAMInBytes;
        }

        public static NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams? dlssgOptEvalParameters)
        {
            NativeScope? dlssgOptEvalParametersScope = null;
            NGXDLSSGOptEvalParamsNative* pDlssgOptEvalParameters = null;

            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssgEvalParametersScope = new();
            NGXVKDLSSGEvalParamsNative* pDlssgEvalParameters = dlssgEvalParametersScope.Alloc(new NGXVKDLSSGEvalParamsNative(in dlssgEvalParameters, dlssgEvalParametersScope));

            if (dlssgOptEvalParameters is NGXDLSSGOptEvalParams dlssgOptEvalParametersValue)
            {
                dlssgOptEvalParametersScope = new();
                pDlssgOptEvalParameters = dlssgOptEvalParametersScope.Alloc(new NGXDLSSGOptEvalParamsNative(in dlssgOptEvalParametersValue));
            }

            NGXResult result = EvaluateDLSSGNative(commandBuffer, handle, parameters, pDlssgEvalParameters, pDlssgOptEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "Vulkan.EvaluateDLSSG.pInDlssgEvalParams", dlssgEvalParametersScope, result);

            if (dlssgOptEvalParametersScope is not null)
            {
                NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "Vulkan.EvaluateDLSSG.pInDlssgOptEvalParams", dlssgOptEvalParametersScope, result);
            }

            return result;
        }

        public static NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSGEvalParams dlssgEvalParameters, in NGXDLSSGOptEvalParams dlssgOptEvalParameters)
        {
            return EvaluateDLSSG(commandBuffer, handle, parameters, in dlssgEvalParameters, (NGXDLSSGOptEvalParams?)dlssgOptEvalParameters);
        }

        public static NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            handle = default;

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
            CreateDLISPExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlispCreateParameters).CheckError("Ngx.Vulkan.CreateDLISPExt");

            return handle;
        }

        public static NGXResult CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSDCreateParamsNative dlssDCreateParametersNative = new(in dlssDCreateParameters);
            NGXResult result = CreateDLSSDExt1Native(device, commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssDCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            CreateDLSSDExt1(device, commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssDCreateParameters).CheckError("Ngx.Vulkan.CreateDLSSDExt1");

            return handle;
        }

        public static NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            handle = default;

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
            CreateDLSSExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssCreateParameters).CheckError("Ngx.Vulkan.CreateDLSSExt");

            return handle;
        }

        public static NGXResult CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXDLSSCreateParamsNative dlssCreateParametersNative = new(in dlssCreateParameters);
            NGXResult result = CreateDLSSExt1Native(device, commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssCreateParametersNative);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            CreateDLSSExt1(device, commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssCreateParameters).CheckError("Ngx.Vulkan.CreateDLSSExt1");

            return handle;
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLISPEvalParams dlispEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlispEvalParametersScope = new();
            NGXVKDLISPEvalParamsNative* pDlispEvalParameters = dlispEvalParametersScope.Alloc(new NGXVKDLISPEvalParamsNative(in dlispEvalParameters, dlispEvalParametersScope));
            NGXResult result = EvaluateDLISPExtNative(commandList, handle, parameters, pDlispEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "Vulkan.EvaluateDLISPExt.pInDlispEvalParams", dlispEvalParametersScope, result);

            return result;
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSDEvalParams dlssDEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssDEvalParametersScope = new();
            NGXVKDLSSDEvalParamsNative* pDlssDEvalParameters = dlssDEvalParametersScope.Alloc(new NGXVKDLSSDEvalParamsNative(in dlssDEvalParameters, dlssDEvalParametersScope));
            NGXResult result = EvaluateDLSSDExtNative(commandList, handle, parameters, pDlssDEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "Vulkan.EvaluateDLSSDExt.pInDlssDEvalParams", dlssDEvalParametersScope, result);

            return result;
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSEvalParams dlssEvalParameters)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));
            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NativeScope dlssEvalParametersScope = new();
            NGXVKDLSSEvalParamsNative* pDlssEvalParameters = dlssEvalParametersScope.Alloc(new NGXVKDLSSEvalParamsNative(in dlssEvalParameters, dlssEvalParametersScope));
            NGXResult result = EvaluateDLSSExtNative(commandList, handle, parameters, pDlssEvalParameters);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, parameters, "Vulkan.EvaluateDLSSExt.pInDlssEvalParams", dlssEvalParametersScope, result);

            return result;
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            parameters = default;

            NGXResult result = AllocateParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter AllocateParameters()
        {
            AllocateParameters(out NGXParameter parameters).CheckError("Ngx.Vulkan.AllocateParameters");

            return parameters;
        }

        public static NGXResult CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = CreateFeatureNative(commandBuffer, featureID, parameters, out handle);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters)
        {
            CreateFeature(commandBuffer, featureID, parameters, out NGXHandle handle).CheckError("Ngx.Vulkan.CreateFeature");

            return handle;
        }

        public static NGXResult CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = CreateFeature1Native(device, commandList, featureID, parameters, out handle);
            if (result.IsFailure)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters)
        {
            CreateFeature1(device, commandList, featureID, parameters, out NGXHandle handle).CheckError("Ngx.Vulkan.CreateFeature1");

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
            parameters = default;

            NGXResult result = GetCapabilityParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetCapabilityParameters()
        {
            GetCapabilityParameters(out NGXParameter parameters).CheckError("Ngx.Vulkan.GetCapabilityParameters");

            return parameters;
        }

        public static NGXResult GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties)
        {
            uint extensionCount = 0;
            extensionProperties = [];
            NGXVkExtensionPropertiesNative* pExtensionProperties = null;

            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
            NGXResult result = GetFeatureDeviceExtensionRequirementsNative(instance, physicalDevice, &featureDiscoveryInfoNative, out extensionCount, out pExtensionProperties);
            if (result.IsSuccess)
            {
                extensionProperties = new NGXVkExtensionProperties[checked((int)extensionCount)];

                if (extensionProperties.Length is not 0 && pExtensionProperties is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < extensionProperties.Length; i++)
                {
                    extensionProperties[i] = new(in pExtensionProperties[i]);
                }
            }
            else
            {
                extensionProperties = [];
            }

            return result;
        }

        public static NGXVkExtensionProperties[] GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            GetFeatureDeviceExtensionRequirements(instance, physicalDevice, in featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties).CheckError("Ngx.Vulkan.GetFeatureDeviceExtensionRequirements");

            return extensionProperties;
        }

        public static NGXResult GetFeatureInstanceExtensionRequirements(in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties)
        {
            uint extensionCount = 0;
            extensionProperties = [];
            NGXVkExtensionPropertiesNative* pExtensionProperties = null;

            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
            NGXResult result = GetFeatureInstanceExtensionRequirementsNative(&featureDiscoveryInfoNative, out extensionCount, out pExtensionProperties);
            if (result.IsSuccess)
            {
                extensionProperties = new NGXVkExtensionProperties[checked((int)extensionCount)];

                if (extensionProperties.Length is not 0 && pExtensionProperties is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < extensionProperties.Length; i++)
                {
                    extensionProperties[i] = new(in pExtensionProperties[i]);
                }
            }
            else
            {
                extensionProperties = [];
            }

            return result;
        }

        public static NGXVkExtensionProperties[] GetFeatureInstanceExtensionRequirements(in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            GetFeatureInstanceExtensionRequirements(in featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties).CheckError("Ngx.Vulkan.GetFeatureInstanceExtensionRequirements");

            return extensionProperties;
        }

        public static NGXResult GetFeatureRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            supported = default;
            NGXFeatureRequirementNative supportedNative = default;

            using NativeScope scope = new();

            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = new(in featureDiscoveryInfo, scope);
            NGXResult result = GetFeatureRequirementsNative(instance, physicalDevice, &featureDiscoveryInfoNative, out supportedNative);
            if (result.IsSuccess)
            {
                supported = new(in supportedNative);
            }
            else
            {
                supported = default;
            }

            return result;
        }

        public static NGXFeatureRequirement GetFeatureRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            GetFeatureRequirements(instance, physicalDevice, in featureDiscoveryInfo, out NGXFeatureRequirement supported).CheckError("Ngx.Vulkan.GetFeatureRequirements");

            return supported;
        }

        public static NGXResult GetParameters(out NGXParameter parameters)
        {
            parameters = default;

            NGXResult result = GetParametersNative(out parameters);
            if (result.IsFailure)
            {
                parameters = default;
            }

            return result;
        }

        public static NGXParameter GetParameters()
        {
            GetParameters(out NGXParameter parameters).CheckError("Ngx.Vulkan.GetParameters");

            return parameters;
        }

        public static NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, out nuint sizeInBytes)
        {
            sizeInBytes = default;

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
            GetScratchBufferSize(featureId, parameters, out nuint sizeInBytes).CheckError("Ngx.Vulkan.GetScratchBufferSize");

            return sizeInBytes;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitNative(applicationId, scope.AllocWide(applicationDataPath), instance, pd, device, gipa, gdpa, featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, device, scope, result);

            return result;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, instance, pd, device, gipa, gdpa, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
        {
            NGXFeatureCommonInfoNative featureInfoNative = default;

            NativeScope scope = new();

            if (featureInfo is NGXFeatureCommonInfo featureInfoValue)
            {
                featureInfoNative = new(in featureInfoValue, scope);
            }

            NGXResult result = InitWithProjectIDNative(scope.AllocUtf8(projectId), engineType, scope.AllocUtf8(engineVersion), scope.AllocWide(applicationDataPath), instance, pd, device, gipa, gdpa, featureInfo.HasValue ? &featureInfoNative : null, sdkVersion);
            NativeLifetime.Retain(NGXGraphicsAPI.Vulkan, device, scope, result);

            return result;
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, instance, pd, device, gipa, gdpa, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult ReleaseFeature(NGXHandle handle)
        {
            ArgumentNullException.ThrowIfNull((void*)handle.Value, nameof(handle));

            return ReleaseFeatureNative(handle);
        }

        public static NGXResult RequiredExtensions(out string[] instanceExtensions, out string[] deviceExtensions)
        {
            uint instanceExtensionCount = 0;
            instanceExtensions = [];
            byte** pInstanceExtensions = null;
            uint deviceExtensionCount = 0;
            deviceExtensions = [];
            byte** pDeviceExtensions = null;

            NGXResult result = RequiredExtensionsNative(out instanceExtensionCount, out pInstanceExtensions, out deviceExtensionCount, out pDeviceExtensions);
            if (result.IsSuccess)
            {
                instanceExtensions = new string[checked((int)instanceExtensionCount)];

                if (instanceExtensions.Length is not 0 && pInstanceExtensions is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < instanceExtensions.Length; i++)
                {
                    instanceExtensions[i] = Marshal.PtrToStringUTF8((nint)pInstanceExtensions[i])!;
                }

                deviceExtensions = new string[checked((int)deviceExtensionCount)];

                if (deviceExtensions.Length is not 0 && pDeviceExtensions is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < deviceExtensions.Length; i++)
                {
                    deviceExtensions[i] = Marshal.PtrToStringUTF8((nint)pDeviceExtensions[i])!;
                }
            }
            else
            {
                instanceExtensions = [];
                deviceExtensions = [];
            }

            return result;
        }

        public static NGXResult Shutdown()
        {
            NGXResult result = ShutdownNative();

            if (result.IsSuccess)
            {
                NativeLifetime.Release(NGXGraphicsAPI.Vulkan, 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(nint device)
        {
            NGXResult result = Shutdown1Native(device);

            if (result.IsSuccess)
            {
                NativeLifetime.Release(NGXGraphicsAPI.Vulkan, device);
            }

            return result;
        }
    }
}
