#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers.h"

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSS_GET_OPTIMAL_SETTINGS(NVSDK_NGX_Parameter * pInParams, unsigned int InUserSelectedWidth, unsigned int InUserSelectedHeight, NVSDK_NGX_PerfQuality_Value InPerfQualityValue, unsigned int * pOutRenderOptimalWidth, unsigned int * pOutRenderOptimalHeight, unsigned int * pOutRenderMaxWidth, unsigned int * pOutRenderMaxHeight, unsigned int * pOutRenderMinWidth, unsigned int * pOutRenderMinHeight, float * pOutSharpness)
{
    return NGX_DLSS_GET_OPTIMAL_SETTINGS(pInParams, InUserSelectedWidth, InUserSelectedHeight, InPerfQualityValue, pOutRenderOptimalWidth, pOutRenderOptimalHeight, pOutRenderMaxWidth, pOutRenderMaxHeight, pOutRenderMinWidth, pOutRenderMinHeight, pOutSharpness);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSS_GET_STATS(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes)
{
    return NGX_DLSS_GET_STATS(pInParams, pVRAMAllocatedBytes);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSS_GET_STATS_1(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel)
{
    return NGX_DLSS_GET_STATS_1(pInParams, pVRAMAllocatedBytes, pOptLevel);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSS_GET_STATS_2(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel, unsigned int * IsDevSnippetBranch)
{
    return NGX_DLSS_GET_STATS_2(pInParams, pVRAMAllocatedBytes, pOptLevel, IsDevSnippetBranch);
}
