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
        private static partial NGXResult InitWithProjectIDNative(sbyte* inProjectId, NGXEngineType inEngineType, sbyte* inEngineVersion, void* inApplicationDataPath, nint inDevice, NGXFeatureCommonInfoNative* inFeatureInfo, NGXVersion inSDKVersion);

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
                throw new NGXException(result, "Ngx.D3D12.CreateDLISPExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
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
                NGXResult result = CreateDLSSDExtNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssDCreateParametersNative);
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

        public static NGXHandle CreateDLSSDExt(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSDCreateParams dlssDCreateParameters)
        {
            NGXResult result = CreateDLSSDExt(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssDCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D12.CreateDLSSDExt");
            }

            return handle;
        }

        public static NGXResult CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, out NGXHandle handle, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
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
                NGXResult result = CreateDLSSGNative(commandList, creationNodeMask, visibilityNodeMask, out handle, parameters, &dlssgCreateParametersNative);
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

        public static NGXHandle CreateDLSSG(nint commandList, uint creationNodeMask, uint visibilityNodeMask, NGXParameter parameters, in NGXDLSSGCreateParams dlssgCreateParameters)
        {
            NGXResult result = CreateDLSSG(commandList, creationNodeMask, visibilityNodeMask, out NGXHandle handle, parameters, in dlssgCreateParameters);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D12.CreateDLSSG");
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
                throw new NGXException(result, "Ngx.D3D12.CreateDLSSExt");
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
                throw new NGXException(result, "Ngx.D3D12.EstimateVRAMDLSSG");
            }

            return estimatedVRAMInBytes;
        }

        public static NGXResult EvaluateDLISPExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLISPEvalParams dlispEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXD3D12DLISPEvalParamsNative dlispEvalParametersNative = default;
            NGXD3D12DLISPEvalParamsNative* pDlispEvalParameters = null;

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
                NgxLifetime.BeginParameters(parameters.Value, "D3D12.EvaluateDLISPExt", storage!);
                attached = true;
                result = EvaluateDLISPExtNative(commandList, handle, parameters, pDlispEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "D3D12.EvaluateDLISPExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlispEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSDExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSDEvalParams dlssDEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXD3D12DLSSDEvalParamsNative dlssDEvalParametersNative = default;
            NGXD3D12DLSSDEvalParamsNative* pDlssDEvalParameters = null;

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
                NgxLifetime.BeginParameters(parameters.Value, "D3D12.EvaluateDLSSDExt", storage!);
                attached = true;
                result = EvaluateDLSSDExtNative(commandList, handle, parameters, pDlssDEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "D3D12.EvaluateDLSSDExt", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssDEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSGEvalParams dlssgEvalParameters, NGXDLSSGOptEvalParams? dlssgOptEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXD3D12DLSSGEvalParamsNative dlssgEvalParametersNative = default;
            NGXD3D12DLSSGEvalParamsNative* pDlssgEvalParameters = null;
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
                NgxLifetime.BeginParameters(parameters.Value, "D3D12.EvaluateDLSSG", storage!);
                attached = true;
                result = EvaluateDLSSGNative(commandList, handle, parameters, pDlssgEvalParameters, pDlssgOptEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "D3D12.EvaluateDLSSG", returned, result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                dlssgOptEvalParametersNative.Dispose();
                dlssgEvalParametersNative.Dispose();
            }
        }

        public static NGXResult EvaluateDLSSG(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSGEvalParams dlssgEvalParameters, in NGXDLSSGOptEvalParams dlssgOptEvalParameters)
        {
            return EvaluateDLSSG(commandList, handle, parameters, in dlssgEvalParameters, (NGXDLSSGOptEvalParams?)dlssgOptEvalParameters);
        }

        public static NGXResult EvaluateDLSSExt(nint commandList, NGXHandle handle, NGXParameter parameters, in NGXD3D12DLSSEvalParams dlssEvalParameters)
        {
            NativeCall? storage = new();
            NGXResult result = NGXResult.Fail;
            bool attached = false;
            bool returned = false;
            NGXD3D12DLSSEvalParamsNative dlssEvalParametersNative = default;
            NGXD3D12DLSSEvalParamsNative* pDlssEvalParameters = null;

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
                NgxLifetime.BeginParameters(parameters.Value, "D3D12.EvaluateDLSSExt", storage!);
                attached = true;
                result = EvaluateDLSSExtNative(commandList, handle, parameters, pDlssEvalParameters);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndParameters(parameters.Value, "D3D12.EvaluateDLSSExt", returned, result is NGXResult.Success, ref storage);
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
                NgxLifetime.RegisterParameters("D3D12", parameters.Value);
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
                throw new NGXException(result, "Ngx.D3D12.AllocateParameters");
            }

            return parameters;
        }

        public static NGXResult CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters, out NGXHandle handle)
        {
            handle = default;

            if (parameters.IsNull)
            {
                throw new ArgumentException("A non-null NGX handle is required.", nameof(parameters));
            }

            NGXResult result = CreateFeatureNative(commandList, featureID, parameters, out handle);
            if (result is not NGXResult.Success)
            {
                handle = default;
            }

            return result;
        }

        public static NGXHandle CreateFeature(nint commandList, NGXFeature featureID, NGXParameter parameters)
        {
            NGXResult result = CreateFeature(commandList, featureID, parameters, out NGXHandle handle);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D12.CreateFeature");
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
                NgxLifetime.RegisterParameters("D3D12", parameters.Value);
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
                throw new NGXException(result, "Ngx.D3D12.GetCapabilityParameters");
            }

            return parameters;
        }

        public static NGXResult GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo, out NGXFeatureRequirement supported)
        {
            NGXFeatureDiscoveryInfoNative featureDiscoveryInfoNative = default;
            NGXFeatureRequirementNative supportedNative = default;
            supported = default;

            try
            {
                featureDiscoveryInfoNative = new(in featureDiscoveryInfo);
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
            finally
            {
                featureDiscoveryInfoNative.Dispose();
            }
        }

        public static NGXFeatureRequirement GetFeatureRequirements(nint adapter, in NGXFeatureDiscoveryInfo featureDiscoveryInfo)
        {
            NGXResult result = GetFeatureRequirements(adapter, in featureDiscoveryInfo, out NGXFeatureRequirement supported);
            if (result is not NGXResult.Success)
            {
                throw new NGXException(result, "Ngx.D3D12.GetFeatureRequirements");
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
                NgxLifetime.RegisterParameters("D3D12", parameters.Value);
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
                throw new NGXException(result, "Ngx.D3D12.GetParameters");
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
                throw new NGXException(result, "Ngx.D3D12.GetScratchBufferSize");
            }

            return sizeInBytes;
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
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
                NgxLifetime.BeginInitialization("D3D12", device, storage!);
                attached = true;
                result = InitNative(applicationId, pApplicationDataPath, device, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("D3D12", device, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult Init(ulong applicationId, string? applicationDataPath, nint device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return Init(applicationId, applicationDataPath, device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint device, NGXFeatureCommonInfo? featureInfo, NGXVersion sdkVersion)
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
                NgxLifetime.BeginInitialization("D3D12", device, storage!);
                attached = true;
                result = InitWithProjectIDNative(pProjectId, engineType, pEngineVersion, pApplicationDataPath, device, pFeatureInfo, sdkVersion);
                returned = true;

                return result;
            }
            finally
            {
                if (attached)
                {
                    NgxLifetime.EndInitialization("D3D12", device, returned && result is NGXResult.Success, ref storage);
                }

                storage?.Dispose();
                featureInfoNative.Dispose();
            }
        }

        public static NGXResult InitWithProjectID(string? projectId, NGXEngineType engineType, string? engineVersion, string? applicationDataPath, nint device, in NGXFeatureCommonInfo featureInfo, NGXVersion sdkVersion)
        {
            return InitWithProjectID(projectId, engineType, engineVersion, applicationDataPath, device, (NGXFeatureCommonInfo?)featureInfo, sdkVersion);
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
                NgxLifetime.Shutdown("D3D12", 0);
            }

            return result;
        }

        public static NGXResult Shutdown1(nint device)
        {
            NGXResult result = Shutdown1Native(device);

            if (result is NGXResult.Success)
            {
                NgxLifetime.Shutdown("D3D12", device);
            }

            return result;
        }
    }
}
