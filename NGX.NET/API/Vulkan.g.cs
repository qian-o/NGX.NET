namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class Vulkan
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_CREATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSG(nint commandBuffer, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSGCreateParams* dlssgCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_ESTIMATE_VRAM_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, ulong* estimatedVRAMInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VK_EVALUATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, NGXVKDLSSGEvalParams* dlssgEvalParameters, NGXDLSSGOptEvalParams* dlssgOptEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXFeatureCreateParams* dlispCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSSD_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSDCreateParams* dlssDCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSCreateParams* dlssCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSCreateParams* dlssCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXVKDLISPEvalParams* dlispEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXVKDLSSDEvalParams* dlssDEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_VULKAN_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXVKDLSSEvalParams* dlssEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult AllocateParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_CreateFeature1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult DestroyParameters(NGXParameter parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeature(nint commandList, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeatureC(nint commandList, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetCapabilityParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureDeviceExtensionRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, NGXFeatureDiscoveryInfo* featureDiscoveryInfo, uint* extensionCount, NGXVkExtensionProperties** extensionProperties);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureInstanceExtensionRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureInstanceExtensionRequirements(NGXFeatureDiscoveryInfo* featureDiscoveryInfo, uint* extensionCount, NGXVkExtensionProperties** extensionProperties);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureRequirements(nint instance, nint physicalDevice, NGXFeatureDiscoveryInfo* featureDiscoveryInfo, NGXFeatureRequirement* supported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, ulong* sizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Init(ulong applicationId, void* applicationDataPath, nint instance, nint pd, nint device, delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>> gipa, delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>> gdpa, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult InitWithProjectID(byte* projectId, NGXEngineType engineType, byte* engineVersion, void* applicationDataPath, nint instance, nint pd, nint device, delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>> gipa, delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>> gdpa, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult ReleaseFeature(NGXHandle handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_RequiredExtensions")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult RequiredExtensions(uint* instanceExtensionCount, byte*** instanceExtensions, uint* deviceExtensionCount, byte*** deviceExtensions);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_VULKAN_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown1(nint device);

        static Vulkan()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLSSG(nint commandBuffer, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSGCreateParams* pDlssgCreateParameters = &dlssgCreateParameters)
                {
                    return CreateDLSSG(commandBuffer, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssgCreateParameters);
                }
            }
        }

        public static NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out ulong estimatedVRAMInBytes)
        {
            fixed (ulong* pEstimatedVRAMInBytes = &estimatedVRAMInBytes)
            {
                return EstimateVRAMDLSSG(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, pEstimatedVRAMInBytes);
            }
        }

        public static NGXResult EvaluateDLSSG(nint commandBuffer, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams* dlssgOptEvalParameters)
        {
            fixed (NGXVKDLSSGEvalParams* pDlssgEvalParameters = &dlssgEvalParameters)
            {
                return EvaluateDLSSG(commandBuffer, handle, parameters, pDlssgEvalParameters, dlssgOptEvalParameters);
            }
        }

        public static NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXFeatureCreateParams* pDlispCreateParameters = &dlispCreateParameters)
                {
                    return CreateDLISPExt(commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlispCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSDExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSDCreateParams* pDlssDCreateParameters = &dlssDCreateParameters)
                {
                    return CreateDLSSDExt1(device, commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssDCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSCreateParams* pDlssCreateParameters = &dlssCreateParameters)
                {
                    return CreateDLSSExt(commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSExt1(nint device, nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSCreateParams* pDlssCreateParameters = &dlssCreateParameters)
                {
                    return CreateDLSSExt1(device, commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssCreateParameters);
                }
            }
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLISPEvalParams dlispEvalParameters)
        {
            fixed (NGXVKDLISPEvalParams* pDlispEvalParameters = &dlispEvalParameters)
            {
                return EvaluateDLISPExt(commandList, handle, parameters, pDlispEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSDEvalParams dlssDEvalParameters)
        {
            fixed (NGXVKDLSSDEvalParams* pDlssDEvalParameters = &dlssDEvalParameters)
            {
                return EvaluateDLSSDExt(commandList, handle, parameters, pDlssDEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXVKDLSSEvalParams dlssEvalParameters)
        {
            fixed (NGXVKDLSSEvalParams* pDlssEvalParameters = &dlssEvalParameters)
            {
                return EvaluateDLSSExt(commandList, handle, parameters, pDlssEvalParameters);
            }
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return AllocateParameters(pParameters);
            }
        }

        public static NGXResult CreateFeature(nint commandBuffer, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature(commandBuffer, featureID, parameters, pHandle);
            }
        }

        public static NGXResult CreateFeature1(nint device, nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature1(device, commandList, featureID, parameters, pHandle);
            }
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return GetCapabilityParameters(pParameters);
            }
        }

        public static NGXResult GetFeatureDeviceExtensionRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out uint extensionCount, NGXVkExtensionProperties** extensionProperties)
        {
            fixed (NGXFeatureDiscoveryInfo* pFeatureDiscoveryInfo = &featureDiscoveryInfo)
            {
                fixed (uint* pExtensionCount = &extensionCount)
                {
                    return GetFeatureDeviceExtensionRequirements(instance, physicalDevice, pFeatureDiscoveryInfo, pExtensionCount, extensionProperties);
                }
            }
        }

        public static NGXResult GetFeatureInstanceExtensionRequirements(in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out uint extensionCount, NGXVkExtensionProperties** extensionProperties)
        {
            fixed (NGXFeatureDiscoveryInfo* pFeatureDiscoveryInfo = &featureDiscoveryInfo)
            {
                fixed (uint* pExtensionCount = &extensionCount)
                {
                    return GetFeatureInstanceExtensionRequirements(pFeatureDiscoveryInfo, pExtensionCount, extensionProperties);
                }
            }
        }

        public static NGXResult GetFeatureRequirements(nint instance, nint physicalDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            fixed (NGXFeatureDiscoveryInfo* pFeatureDiscoveryInfo = &featureDiscoveryInfo)
            {
                fixed (NGXFeatureRequirement* pSupported = &supported)
                {
                    return GetFeatureRequirements(instance, physicalDevice, pFeatureDiscoveryInfo, pSupported);
                }
            }
        }

        public static NGXResult GetParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return GetParameters(pParameters);
            }
        }

        public static NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, out ulong sizeInBytes)
        {
            fixed (ulong* pSizeInBytes = &sizeInBytes)
            {
                return GetScratchBufferSize(featureId, parameters, pSizeInBytes);
            }
        }

        public static NGXResult Init(ulong applicationId, string applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return Init(applicationId, pApplicationDataPath, instance, pd, device, (delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>>)gipa, (delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>>)gdpa, featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string projectId, NGXEngineType engineType, string engineVersion, string applicationDataPath, nint instance, nint pd, nint device, nint gipa, nint gdpa, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            byte* pProjectId = strings.AllocUtf8(projectId);
            byte* pEngineVersion = strings.AllocUtf8(engineVersion);
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return InitWithProjectID(pProjectId, engineType, pEngineVersion, pApplicationDataPath, instance, pd, device, (delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>>)gipa, (delegate* unmanaged[Cdecl]<nint, byte*, delegate* unmanaged[Cdecl]<void>>)gdpa, featureInfo, sdkVersion);
        }

        public static NGXResult RequiredExtensions(out uint instanceExtensionCount, byte*** instanceExtensions, out uint deviceExtensionCount, byte*** deviceExtensions)
        {
            fixed (uint* pInstanceExtensionCount = &instanceExtensionCount)
            {
                fixed (uint* pDeviceExtensionCount = &deviceExtensionCount)
                {
                    return RequiredExtensions(pInstanceExtensionCount, instanceExtensions, pDeviceExtensionCount, deviceExtensions);
                }
            }
        }
    }
}
