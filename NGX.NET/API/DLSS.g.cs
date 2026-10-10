#nullable enable

namespace NGX.NET;

public static unsafe partial class Ngx
{
    public static partial class DLSS
    {
        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_OPTIMAL_SETTINGS")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetOptimalSettingsNative(NGXParameter pInParams, uint inUserSelectedWidth, uint inUserSelectedHeight, NGXPerfQualityValue inPerfQualityValue, out uint pOutRenderOptimalWidth, out uint pOutRenderOptimalHeight, out uint pOutRenderMaxWidth, out uint pOutRenderMaxHeight, out uint pOutRenderMinWidth, out uint pOutRenderMinHeight, out float pOutSharpness);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetStatsNative(NGXParameter pInParams, out ulong pVRAMAllocatedBytes);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS_1")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetStats1Native(NGXParameter pInParams, out ulong pVRAMAllocatedBytes, out uint pOptLevel);

        [LibraryImport(LibraryName, EntryPoint = "NGX_Bridge_NGX_DLSS_GET_STATS_2")]
        [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
        private static partial NGXResult GetStats2Native(NGXParameter pInParams, out ulong pVRAMAllocatedBytes, out uint pOptLevel, out uint isDevSnippetBranch);

        static DLSS()
        {
            NativeLoader.Register();
        }

        public static NGXResult GetOptimalSettings(NGXParameter parameters, uint userSelectedWidth, uint userSelectedHeight, NGXPerfQualityValue perfQualityValue, out uint renderOptimalWidth, out uint renderOptimalHeight, out uint renderMaxWidth, out uint renderMaxHeight, out uint renderMinWidth, out uint renderMinHeight, out float sharpness)
        {
            renderOptimalWidth = default;
            renderOptimalHeight = default;
            renderMaxWidth = default;
            renderMaxHeight = default;
            renderMinWidth = default;
            renderMinHeight = default;
            sharpness = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = GetOptimalSettingsNative(parameters, userSelectedWidth, userSelectedHeight, perfQualityValue, out renderOptimalWidth, out renderOptimalHeight, out renderMaxWidth, out renderMaxHeight, out renderMinWidth, out renderMinHeight, out sharpness);
            if (result.IsFailure)
            {
                renderOptimalWidth = default;
                renderOptimalHeight = default;
                renderMaxWidth = default;
                renderMaxHeight = default;
                renderMinWidth = default;
                renderMinHeight = default;
                sharpness = default;
            }

            return result;
        }

        public static NGXResult GetStats(NGXParameter parameters, out ulong vramAllocatedBytes)
        {
            vramAllocatedBytes = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = GetStatsNative(parameters, out vramAllocatedBytes);
            if (result.IsFailure)
            {
                vramAllocatedBytes = default;
            }

            return result;
        }

        public static ulong GetStats(NGXParameter parameters)
        {
            GetStats(parameters, out ulong vramAllocatedBytes).CheckError("Ngx.DLSS.GetStats");

            return vramAllocatedBytes;
        }

        public static NGXResult GetStats1(NGXParameter parameters, out ulong vramAllocatedBytes, out uint optLevel)
        {
            vramAllocatedBytes = default;
            optLevel = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = GetStats1Native(parameters, out vramAllocatedBytes, out optLevel);
            if (result.IsFailure)
            {
                vramAllocatedBytes = default;
                optLevel = default;
            }

            return result;
        }

        public static NGXResult GetStats2(NGXParameter parameters, out ulong vramAllocatedBytes, out uint optLevel, out uint isDeviceSnippetBranch)
        {
            vramAllocatedBytes = default;
            optLevel = default;
            isDeviceSnippetBranch = default;

            ArgumentNullException.ThrowIfNull((void*)parameters.Value, nameof(parameters));

            NGXResult result = GetStats2Native(parameters, out vramAllocatedBytes, out optLevel, out isDeviceSnippetBranch);
            if (result.IsFailure)
            {
                vramAllocatedBytes = default;
                optLevel = default;
                isDeviceSnippetBranch = default;
            }

            return result;
        }
    }
}
