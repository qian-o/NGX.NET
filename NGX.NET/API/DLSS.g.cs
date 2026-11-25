namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class DLSS
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_OPTIMAL_SETTINGS")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetOptimalSettings(NGXParameter parameters, uint userSelectedWidth, uint userSelectedHeight, NGXPerfQualityValue perfQualityValue, uint* renderOptimalWidth, uint* renderOptimalHeight, uint* renderMaxWidth, uint* renderMaxHeight, uint* renderMinWidth, uint* renderMinHeight, float* sharpness);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetStats(NGXParameter parameters, ulong* vramAllocatedBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS_1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetStats1(NGXParameter parameters, ulong* vramAllocatedBytes, uint* optLevel);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS_2")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        public static partial NGXResult GetStats2(NGXParameter parameters, ulong* vramAllocatedBytes, uint* optLevel, uint* isDevSnippetBranch);

        static DLSS()
        {
            NativeLoader.Register();
        }

        public static NGXResult GetOptimalSettings(NGXParameter parameters, uint userSelectedWidth, uint userSelectedHeight, NGXPerfQualityValue perfQualityValue, out uint renderOptimalWidth, out uint renderOptimalHeight, out uint renderMaxWidth, out uint renderMaxHeight, out uint renderMinWidth, out uint renderMinHeight, out float sharpness)
        {
            fixed (uint* pRenderOptimalWidth = &renderOptimalWidth)
            {
                fixed (uint* pRenderOptimalHeight = &renderOptimalHeight)
                {
                    fixed (uint* pRenderMaxWidth = &renderMaxWidth)
                    {
                        fixed (uint* pRenderMaxHeight = &renderMaxHeight)
                        {
                            fixed (uint* pRenderMinWidth = &renderMinWidth)
                            {
                                fixed (uint* pRenderMinHeight = &renderMinHeight)
                                {
                                    fixed (float* pSharpness = &sharpness)
                                    {
                                        return GetOptimalSettings(parameters, userSelectedWidth, userSelectedHeight, perfQualityValue, pRenderOptimalWidth, pRenderOptimalHeight, pRenderMaxWidth, pRenderMaxHeight, pRenderMinWidth, pRenderMinHeight, pSharpness);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static NGXResult GetStats(NGXParameter parameters, out ulong vramAllocatedBytes)
        {
            fixed (ulong* pVramAllocatedBytes = &vramAllocatedBytes)
            {
                return GetStats(parameters, pVramAllocatedBytes);
            }
        }

        public static NGXResult GetStats1(NGXParameter parameters, out ulong vramAllocatedBytes, out uint optLevel)
        {
            fixed (ulong* pVramAllocatedBytes = &vramAllocatedBytes)
            {
                fixed (uint* pOptLevel = &optLevel)
                {
                    return GetStats1(parameters, pVramAllocatedBytes, pOptLevel);
                }
            }
        }

        public static NGXResult GetStats2(NGXParameter parameters, out ulong vramAllocatedBytes, out uint optLevel, out uint isDevSnippetBranch)
        {
            fixed (ulong* pVramAllocatedBytes = &vramAllocatedBytes)
            {
                fixed (uint* pOptLevel = &optLevel)
                {
                    fixed (uint* pIsDevSnippetBranch = &isDevSnippetBranch)
                    {
                        return GetStats2(parameters, pVramAllocatedBytes, pOptLevel, pIsDevSnippetBranch);
                    }
                }
            }
        }
    }
}
