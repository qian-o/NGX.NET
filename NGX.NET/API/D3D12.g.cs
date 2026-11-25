namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class D3D12
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLISPExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXFeatureCreateParams* dlispCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSDCreateParams* dlssDCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSGCreateParams* dlssgCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXHandle* handle, NGXParameter parameters, NGXDLSSCreateParams* dlssCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_ESTIMATE_VRAM_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, ulong* estimatedVRAMInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXD3D12DLISPEvalParams* dlispEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXD3D12DLSSDEvalParams* dlssDEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSSG")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, NGXD3D12DLSSGEvalParams* dlssgEvalParameters, NGXDLSSGOptEvalParams* dlssgOptEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D12_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, NGXD3D12DLSSEvalParams* dlssEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult AllocateParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult DestroyParameters(NGXParameter parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeature(nint commandList, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeatureC(nint commandList, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetCapabilityParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureRequirements(nint adapter, NGXFeatureDiscoveryInfo* featureDiscoveryInfo, NGXFeatureRequirement* supported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, ulong* sizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Init(ulong applicationId, void* applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult InitWithProjectID(byte* projectId, NGXEngineType engineType, byte* engineVersion, void* applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult ReleaseFeature(NGXHandle handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D12_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown1(nint device);

        static D3D12()
        {
            NativeLoader.Register();
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

        public static NGXResult CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSDCreateParams* pDlssDCreateParameters = &dlssDCreateParameters)
                {
                    return CreateDLSSDExt(commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssDCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSGCreateParams* pDlssgCreateParameters = &dlssgCreateParameters)
                {
                    return CreateDLSSG(commandList, creationNodeMask, visibilityNodeMask, pHandle, parameters, pDlssgCreateParameters);
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

        public static NGXResult EstimateVRAMDLSSG(NGXParameter parameters, uint mvecDepthWidth, uint mvecDepthHeight, uint colorWidth, uint colorHeight, uint colorBufferFormat, uint mvecBufferFormat, uint depthBufferFormat, uint hudLessBufferFormat, uint uiBufferFormat, out ulong estimatedVRAMInBytes)
        {
            fixed (ulong* pEstimatedVRAMInBytes = &estimatedVRAMInBytes)
            {
                return EstimateVRAMDLSSG(parameters, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, pEstimatedVRAMInBytes);
            }
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLISPEvalParams dlispEvalParameters)
        {
            fixed (NGXD3D12DLISPEvalParams* pDlispEvalParameters = &dlispEvalParameters)
            {
                return EvaluateDLISPExt(commandList, handle, parameters, pDlispEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSDEvalParams dlssDEvalParameters)
        {
            fixed (NGXD3D12DLSSDEvalParams* pDlssDEvalParameters = &dlssDEvalParameters)
            {
                return EvaluateDLSSDExt(commandList, handle, parameters, pDlssDEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams* dlssgOptEvalParameters)
        {
            fixed (NGXD3D12DLSSGEvalParams* pDlssgEvalParameters = &dlssgEvalParameters)
            {
                return EvaluateDLSSG(commandList, handle, parameters, pDlssgEvalParameters, dlssgOptEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSEvalParams dlssEvalParameters)
        {
            fixed (NGXD3D12DLSSEvalParams* pDlssEvalParameters = &dlssEvalParameters)
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

        public static NGXResult CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature(commandList, featureID, parameters, pHandle);
            }
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return GetCapabilityParameters(pParameters);
            }
        }

        public static NGXResult GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            fixed (NGXFeatureDiscoveryInfo* pFeatureDiscoveryInfo = &featureDiscoveryInfo)
            {
                fixed (NGXFeatureRequirement* pSupported = &supported)
                {
                    return GetFeatureRequirements(adapter, pFeatureDiscoveryInfo, pSupported);
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

        public static NGXResult Init(ulong applicationId, string applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return Init(applicationId, pApplicationDataPath, device, featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string projectId, NGXEngineType engineType, string engineVersion, string applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            byte* pProjectId = strings.AllocUtf8(projectId);
            byte* pEngineVersion = strings.AllocUtf8(engineVersion);
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return InitWithProjectID(pProjectId, engineType, pEngineVersion, pApplicationDataPath, device, featureInfo, sdkVersion);
        }
    }
}
