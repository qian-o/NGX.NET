namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class D3D11
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLISPExt(nint ctx, NGXHandle* handle, NGXParameter parameters, NGXFeatureCreateParams* dlispCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSDExt(nint ctx, NGXHandle* handle, NGXParameter parameters, NGXDLSSDCreateParams* dlssDCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_CREATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateDLSSExt(nint ctx, NGXHandle* handle, NGXParameter parameters, NGXDLSSCreateParams* dlssCreateParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLISP_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLISPExt(nint ctx, NGXHandle handle, NGXParameter parameters, NGXD3D11DLISPEvalParams* dlispEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLSSD_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSDExt(nint ctx, NGXHandle handle, NGXParameter parameters, NGXD3D11DLSSDEvalParams* dlssDEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_D3D11_EVALUATE_DLSS_EXT")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateDLSSExt(nint ctx, NGXHandle handle, NGXParameter parameters, NGXD3D11DLSSEvalParams* dlssEvalParameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_AllocateParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult AllocateParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_CreateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult CreateFeature(nint deviceCtx, NGXFeature featureID, NGXParameter parameters, NGXHandle* handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_DestroyParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult DestroyParameters(NGXParameter parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_EvaluateFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeature(nint deviceCtx, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_EvaluateFeature_C")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult EvaluateFeatureC(nint deviceCtx, NGXHandle featureHandle, NGXParameter parameters, delegate* unmanaged[Cdecl]<float, byte*, void> callback);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetCapabilityParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetCapabilityParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetFeatureRequirements")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetFeatureRequirements(nint adapter, NGXFeatureDiscoveryInfo* featureDiscoveryInfo, NGXFeatureRequirement* supported);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetParameters")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetParameters(NGXParameter* parameters);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_GetScratchBufferSize")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetScratchBufferSize(NGXFeature featureId, NGXParameter parameters, ulong* sizeInBytes);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Init")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Init(ulong applicationId, void* applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Init_with_ProjectID")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult InitWithProjectID(byte* projectId, NGXEngineType engineType, byte* engineVersion, void* applicationDataPath, nint device, NGXFeatureCommonInfo* featureInfo, NGXVersion sdkVersion);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_ReleaseFeature")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult ReleaseFeature(NGXHandle handle);

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Shutdown")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown();

        [LibraryImport(LibraryName, EntryPoint = "NVSDK_NGX_D3D11_Shutdown1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult Shutdown1(nint device);

        static D3D11()
        {
            NativeLoader.Register();
        }

        public static NGXResult CreateDLISPExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXFeatureCreateParams dlispCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXFeatureCreateParams* pDlispCreateParameters = &dlispCreateParameters)
                {
                    return CreateDLISPExt(ctx, pHandle, parameters, pDlispCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSDExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSDCreateParams* pDlssDCreateParameters = &dlssDCreateParameters)
                {
                    return CreateDLSSDExt(ctx, pHandle, parameters, pDlssDCreateParameters);
                }
            }
        }

        public static NGXResult CreateDLSSExt(nint ctx, out NGXHandle handle, NGXParameter parameters, in NGXDLSSCreateParams dlssCreateParameters)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                fixed (NGXDLSSCreateParams* pDlssCreateParameters = &dlssCreateParameters)
                {
                    return CreateDLSSExt(ctx, pHandle, parameters, pDlssCreateParameters);
                }
            }
        }

        public static NGXResult EvaluateDLISPExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLISPEvalParams dlispEvalParameters)
        {
            fixed (NGXD3D11DLISPEvalParams* pDlispEvalParameters = &dlispEvalParameters)
            {
                return EvaluateDLISPExt(ctx, handle, parameters, pDlispEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSDExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLSSDEvalParams dlssDEvalParameters)
        {
            fixed (NGXD3D11DLSSDEvalParams* pDlssDEvalParameters = &dlssDEvalParameters)
            {
                return EvaluateDLSSDExt(ctx, handle, parameters, pDlssDEvalParameters);
            }
        }

        public static NGXResult EvaluateDLSSExt(nint ctx, NGXHandle handle, NGXParameter parameters, in NGXD3D11DLSSEvalParams dlssEvalParameters)
        {
            fixed (NGXD3D11DLSSEvalParams* pDlssEvalParameters = &dlssEvalParameters)
            {
                return EvaluateDLSSExt(ctx, handle, parameters, pDlssEvalParameters);
            }
        }

        public static NGXResult AllocateParameters(out NGXParameter parameters)
        {
            fixed (NGXParameter* pParameters = &parameters)
            {
                return AllocateParameters(pParameters);
            }
        }

        public static NGXResult CreateFeature(nint deviceCtx, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            fixed (NGXHandle* pHandle = &handle)
            {
                return CreateFeature(deviceCtx, featureID, parameters, pHandle);
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
