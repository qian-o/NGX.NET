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
        private static partial NGXResult InitWithProjectIDNative(sbyte* inProjectId, NGXEngineType inEngineType, sbyte* inEngineVersion, void* inApplicationDataPath, nint inInstance, nint inPD, nint inDevice, nint inGIPA, nint inGDPA, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult ReleaseFeatureNative(NGXHandle inHandle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_RequiredExtensions")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult RequiredExtensionsNative(out uint outInstanceExtCount, out sbyte** outInstanceExts, out uint outDeviceExtCount, out sbyte** outDeviceExts);

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
            NGXDLSSGCreateParamsNative dlssgCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssgCreateParametersNative = new(in dlssgCreateParameters);
                NGXResult result = CreateDLSSGNative(commandBuffer, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssgCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                return result;
            }
            finally
            {
                dlssgCreateParametersNative.Dispose();
            }
        }

        public static NGXHandle CreateDLSSG(nint commandBuffer, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            NGXResult result = CreateDLSSG(commandBuffer, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssgCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateDLSSG");
            }

            return handle;
        }

        public static NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out nuint estimatedVRAMInBytes)
        {
            estimatedVRAMInBytes = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = EstimateVRAMDLSSGNative(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, out estimatedVRAMInBytes);
            if (result is not NGXResult.Success)
            {
                estimatedVRAMInBytes = default;
            }

            return result;
        }

        public static nuint EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat)
        {
            NGXResult result = EstimateVRAMDLSSG(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, out nuint estimatedVRAMInBytes);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.EstimateVRAMDLSSG");
            }

            return estimatedVRAMInBytes;
        }

        public static NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams? dlssgOptEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXVKDLSSGEvalParamsNative dlssgEvalParametersNative = default;
            NGXVKDLSSGEvalParamsNative* pDlssgEvalParameters = null;
            NGXDLSSGOptEvalParamsNative dlssgOptEvalParametersNative = default;
            NGXDLSSGOptEvalParamsNative* pDlssgOptEvalParameters = null;

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

                dlssgEvalParametersNative = new(in dlssgEvalParameters);
                pDlssgEvalParameters = storage!.Take(ref dlssgEvalParametersNative);
                storage!.HasFrameGenerationOptions = dlssgOptEvalParameters.HasValue;

                if (dlssgOptEvalParameters is NGXDLSSGOptEvalParams dlssgOptEvalParametersValue)
                {
                    dlssgOptEvalParametersNative = new(in dlssgOptEvalParametersValue);
                }

                pDlssgOptEvalParameters = dlssgOptEvalParameters.HasValue ? storage!.Take(ref dlssgOptEvalParametersNative) : null;
                NgxLifetime.BeginParameters(parameters.Value, "Vulkan.EvaluateDLSSG", storage!);
                attached = true;
                result = EvaluateDLSSGNative(commandBuffer, handle, parameters, pDlssgEvalParameters, pDlssgOptEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "Vulkan.EvaluateDLSSG", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssgOptEvalParametersNative.Dispose();
                dlssgEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSGEvalParams dlssgEvalParameters, in NGXDLSSGOptEvalParams dlssgOptEvalParameters)
        {
            return EvaluateDLSSG(commandBuffer, handle, parameters, in dlssgEvalParameters, (NGXDLSSGOptEvalParams?)dlssgOptEvalParameters);
        }

        public static NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
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
                NGXResult result = CreateDLISPExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlispCreateParametersNative);
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

        public static NGXHandle CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            NGXResult result = CreateDLISPExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlispCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateDLISPExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            handle = default;
            NGXDLSSDCreateParamsNative dlssDCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssDCreateParametersNative = new(in dlssDCreateParameters);
                NGXResult result = CreateDLSSDExt1Native(device, commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssDCreateParametersNative);
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

        public static NGXHandle CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt1(device, commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateDLSSDExt1");
            }

            return handle;
        }

        public static NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            handle = default;
            NGXDLSSCreateParamsNative dlssCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssCreateParametersNative = new(in dlssCreateParameters);
                NGXResult result = CreateDLSSExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                return result;
            }
            finally
            {
                dlssCreateParametersNative.Dispose();
            }
        }

        public static NGXHandle CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            NGXResult result = CreateDLSSExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateDLSSExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            handle = default;
            NGXDLSSCreateParamsNative dlssCreateParametersNative = default;

            try
            {
                if (parameters.IsNull)
                {
                    throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
                }

                dlssCreateParametersNative = new(in dlssCreateParameters);
                NGXResult result = CreateDLSSExt1Native(device, commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssCreateParametersNative);
                if (result is not NGXResult.Success)
                {
                    handle = default;
                }

                return result;
            }
            finally
            {
                dlssCreateParametersNative.Dispose();
            }
        }

        public static NGXHandle CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            NGXResult result = CreateDLSSExt1(device, commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateDLSSExt1");
            }

            return handle;
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLISPEvalParams dlispEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXVKDLISPEvalParamsNative dlispEvalParametersNative = default;
            NGXVKDLISPEvalParamsNative* pDlispEvalParameters = null;

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
                NgxLifetime.BeginParameters(parameters.Value, "Vulkan.EvaluateDLISPExt", storage!);
                attached = true;
                result = EvaluateDLISPExtNative(commandList, handle, parameters, pDlispEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "Vulkan.EvaluateDLISPExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlispEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSDEvalParams dlssDEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXVKDLSSDEvalParamsNative dlssDEvalParametersNative = default;
            NGXVKDLSSDEvalParamsNative* pDlssDEvalParameters = null;

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
                NgxLifetime.BeginParameters(parameters.Value, "Vulkan.EvaluateDLSSDExt", storage!);
                attached = true;
                result = EvaluateDLSSDExtNative(commandList, handle, parameters, pDlssDEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "Vulkan.EvaluateDLSSDExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssDEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSEvalParams dlssEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXVKDLSSEvalParamsNative dlssEvalParametersNative = default;
            NGXVKDLSSEvalParamsNative* pDlssEvalParameters = null;

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

                dlssEvalParametersNative = new(in dlssEvalParameters);
                pDlssEvalParameters = storage!.Take(ref dlssEvalParametersNative);
                NgxLifetime.BeginParameters(parameters.Value, "Vulkan.EvaluateDLSSExt", storage!);
                attached = true;
                result = EvaluateDLSSExtNative(commandList, handle, parameters, pDlssEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "Vulkan.EvaluateDLSSExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssEvalParametersNative.Dispose();
            }
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            parameters = default;
            NgxLifetime.PrepareParameters();
            NGXResult result = AllocateParametersNative(out parameters);
            if (result is NGXResult.Success)
            {
                NgxLifetime.RegisterParameters("Vulkan", parameters.Value);
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
                throw new NGXException(result, "Ngx.Vulkan.AllocateParameters");
            }

            return parameters;
        }

        public static NGXResult CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = CreateFeatureNative(commandBuffer, featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature(commandBuffer, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateFeature");
            }

            return handle;
        }

        public static NGXResult CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = CreateFeature1Native(device, commandList, featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature1(device, commandList, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.CreateFeature1");
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

        public static NGXResult EvaluateFeature(nint commandList, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallback? callback)
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
                NGXResult result = EvaluateFeatureNative(commandList, featureHandle, parameters, callbackNative);

                return result;
            }
            finally
            {
                NgxCallbacks.Release(callbackNative);
            }
        }

        public static NGXResult EvaluateFeatureC(nint commandList, NGXHandle featureHandle, NGXParameter parameters, NGXPfnProgressCallbackC? callback)
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
                NGXResult result = EvaluateFeatureCNative(commandList, featureHandle, parameters, callbackNative);

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
                NgxLifetime.RegisterParameters("Vulkan", parameters.Value);
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
                throw new NGXException(result, "Ngx.Vulkan.GetCapabilityParameters");
            }

            return parameters;
        }

        public static NGXResult GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties)
        {
            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = default;
            uint extensionCount = 0;
            extensionProperties = [];
            NGXVkExtensionPropertiesNative* pExtensionProperties = null;

            try
            {
                featureDiscoveryInfoNative = new(in featureDiscoveryInfo);
                NGXResult result = GetFeatureDeviceExtensionRequirementsNative(instance, physicalDevice, &featureDiscoveryInfoNative, out extensionCount, out pExtensionProperties);
                if (result is NGXResult.Success)
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
            finally
            {
                featureDiscoveryInfoNative.Dispose();
            }
        }

        public static NGXVkExtensionProperties[] GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureDeviceExtensionRequirements(instance, physicalDevice, in featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.GetFeatureDeviceExtensionRequirements");
            }

            return extensionProperties;
        }

        public static NGXResult GetFeatureInstanceExtensionRequirements(in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties)
        {
            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = default;
            uint extensionCount = 0;
            extensionProperties = [];
            NGXVkExtensionPropertiesNative* pExtensionProperties = null;

            try
            {
                featureDiscoveryInfoNative = new(in featureDiscoveryInfo);
                NGXResult result = GetFeatureInstanceExtensionRequirementsNative(&featureDiscoveryInfoNative, out extensionCount, out pExtensionProperties);
                if (result is NGXResult.Success)
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
            finally
            {
                featureDiscoveryInfoNative.Dispose();
            }
        }

        public static NGXVkExtensionProperties[] GetFeatureInstanceExtensionRequirements(in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureInstanceExtensionRequirements(in featureDiscoveryInfo, out NGXVkExtensionProperties[] extensionProperties);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.GetFeatureInstanceExtensionRequirements");
            }

            return extensionProperties;
        }

        public static NGXResult GetFeatureRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = default;
            NGXFeatureRequirementNative supportedNative = default;
            supported = default;

            try
            {
                featureDiscoveryInfoNative = new(in featureDiscoveryInfo);
                NGXResult result = GetFeatureRequirementsNative(instance, physicalDevice, &featureDiscoveryInfoNative, out supportedNative);
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

        public static NGXFeatureRequirement GetFeatureRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureRequirements(instance, physicalDevice, in featureDiscoveryInfo, out NGXFeatureRequirement supported);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.GetFeatureRequirements");
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
                NgxLifetime.RegisterParameters("Vulkan", parameters.Value);
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
                throw new NGXException(result, "Ngx.Vulkan.GetParameters");
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
                throw new NGXException(result, "Ngx.Vulkan.GetScratchBufferSize");
            }

            return sizeInBytes;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
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
                NgxLifetime.BeginInitialization("Vulkan", device, storage!);
                attached = true;
                result = InitNative(applicationId, pApplicationDataPath, instance, pd, device, gipa, gdpa, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("Vulkan", device, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, instance, pd, device, gipa, gdpa, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
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
                NgxLifetime.BeginInitialization("Vulkan", device, storage!);
                attached = true;
                result = InitWithProjectIDNative(pProjectId, engineType, pEngineVersion, pApplicationDataPath, instance, pd, device, gipa, gdpa, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("Vulkan", device, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, instance, pd, device, gipa, gdpa, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult ReleaseFeature(NGXHandle handle)
        {
            if (handle.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(handle));
            }

            return ReleaseFeatureNative(handle);
        }

        public static NGXResult RequiredExtensions(out string[] instanceExtensions, out string[] deviceExtensions)
        {
            uint instanceExtensionCount = 0;
            instanceExtensions = [];
            sbyte** pInstanceExtensions = null;
            uint deviceExtensionCount = 0;
            deviceExtensions = [];
            sbyte** pDeviceExtensions = null;
            NGXResult result = RequiredExtensionsNative(out instanceExtensionCount, out pInstanceExtensions, out deviceExtensionCount, out pDeviceExtensions);
            if (result is NGXResult.Success)
            {
                instanceExtensions = new string[checked((int)instanceExtensionCount)];

                if (instanceExtensions.Length is not 0 && pInstanceExtensions is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < instanceExtensions.Length; i++)
                {
                    instanceExtensions[i] = NGXMarshal.PtrToString(pInstanceExtensions[i], NGXEncoding.Utf8)!;
                }

                deviceExtensions = new string[checked((int)deviceExtensionCount)];

                if (deviceExtensions.Length is not 0 && pDeviceExtensions is null)
                {
                    throw new InvalidOperationException("NGX returned a null extension array.");
                }

                for (int i = 0; i < deviceExtensions.Length; i++)
                {
                    deviceExtensions[i] = NGXMarshal.PtrToString(pDeviceExtensions[i], NGXEncoding.Utf8)!;
                }
            }
            else
            {
                instanceExtensions = [];
                deviceExtensions = [];
            }

            return result;
        }

        public static Extensions RequiredExtensions()
        {
            NGXResult result = RequiredExtensions(out string[] instanceExtensions, out string[] deviceExtensions);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.Vulkan.RequiredExtensions");
            }

            return new(instanceExtensions, deviceExtensions);
        }

        public static NGXResult Shutdown()
        {
            NGXResult result = ShutdownNative();

            if (result is NGXResult.Success)
            {
                NgxLifetime.Shutdown("Vulkan", 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(nint device)
        {
            NGXResult result = Shutdown1Native(device);

            if (result is NGXResult.Success)
            {
                NgxLifetime.Shutdown("Vulkan", device);
            }

            return result;
        }
    }
}
