namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class CUDA
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLISPExt(NGXHandle* handle, NGXParameter parameters, NGXFeatureCreateParams* dlispCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSDExt(NGXHandle* handle, NGXParameter parameters, NGXCUDADLSSDCreateParams* dlssDCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSDExt1(NGXCUDADevice* device, NGXHandle* handle, NGXParameter parameters, NGXCUDADLSSDCreateParams* dlssDCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLISPExt(NGXHandle handle, NGXParameter parameters, NGXCUDADLISPEvalParams* dlispEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_CUDA_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSDExt(NGXHandle handle, NGXParameter parameters, NGXCUDADLSSDEvalParams* dlssDEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult AllocateParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature(NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_CreateFeature1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature1(NGXCUDADevice* device, NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult DestroyParameters(NGXParameter parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeature(NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeatureC(NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetCapabilityParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureRequirements(int cudaDevice, NGXFeatureDiscoveryInfo* featureDiscoveryInfo, NGXFeatureRequirement* supported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, ulong* sizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Init(ulong applicationId, void* applicationDataPath, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Init1(ulong applicationId, void* applicationDataPath, NGXCUDADevice* device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult InitWithProjectID(byte* projectId, NGXEngineType engineType, byte* engineVersion, void* applicationDataPath, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult ReleaseFeature(NGXHandle handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_CUDA_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown1(NGXCUDADevice* device);

        static CUDA()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLISPExt(out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXFeatureCreateParams* pDlispCreateParameters = &dlispCreateParameters)
                {
                    return CreateDLISPExt(pHandle, parameters, pDlispCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSDExt(out NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXCUDADLSSDCreateParams* pDlssDCreateParameters = &dlssDCreateParameters)
                {
                    return CreateDLSSDExt(pHandle, parameters, pDlssDCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSDExt1(NGXCUDADevice* device, out NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDCreateParams dlssDCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXCUDADLSSDCreateParams* pDlssDCreateParameters = &dlssDCreateParameters)
                {
                    return CreateDLSSDExt1(device, pHandle, parameters, pDlssDCreateParameters);
                }
            }
        }

        public static NGXResult EvaluateDLISPExt(NGXHandle handle, NGXParameter parameters, in NGXCUDADLISPEvalParams dlispEvalParameters)
        {
            fixed (NGXCUDADLISPEvalParams* pDlispEvalParameters = &dlispEvalParameters)
            {
                return EvaluateDLISPExt(handle, parameters, pDlispEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSDExt(NGXHandle handle, NGXParameter parameters, in NGXCUDADLSSDEvalParams dlssDEvalParameters)
        {
            fixed (NGXCUDADLSSDEvalParams* pDlssDEvalParameters = &dlssDEvalParameters)
            {
                return EvaluateDLSSDExt(handle, parameters, pDlssDEvalParameters);
            }
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return AllocateParameters(pParameters);
            }
        }

        public static NGXResult CreateFeature(NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature(featureID, parameters, pHandle);
            }
        }

        public static NGXResult CreateFeature1(NGXCUDADevice* device, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature1(device, featureID, parameters, pHandle);
            }
        }

        public static NGXResult GetCapabilityParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return GetCapabilityParameters(pParameters);
            }
        }

        public static NGXResult GetFeatureRequirements(int cudaDevice, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            fixed (NGXFeatureDiscoveryInfo* pFeatureDiscoveryInfo = &featureDiscoveryInfo)
            {
                fixed (NGXFeatureRequirement* pSupported = &supported)
                {
                    return GetFeatureRequirements(cudaDevice, pFeatureDiscoveryInfo, pSupported);
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

        public static NGXResult Init(ulong applicationId, string applicationDataPath, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return Init(applicationId, pApplicationDataPath, featureInfo, sdkVersion);
        }

        public static NGXResult Init1(ulong applicationId, string applicationDataPath, NGXCUDADevice* device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return Init1(applicationId, pApplicationDataPath, device, featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string projectId, NGXEngineType engineType, string engineVersion, string applicationDataPath, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion)
        {
            using NGXStrings strings = new();
            byte* pProjectId = strings.AllocUtf8(projectId);
            byte* pEngineVersion = strings.AllocUtf8(engineVersion);
            void* pApplicationDataPath = strings.AllocWide(applicationDataPath);

            return InitWithProjectID(pProjectId, engineType, pEngineVersion, pApplicationDataPath, featureInfo, sdkVersion);
        }
    }
}
