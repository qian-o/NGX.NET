#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_cuda.h"
#include "nvsdk_ngx_helpers_dlssd_cuda.h"

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT(NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_CUDA_CREATE_DLSSD_EXT(ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT1(NVSDK_NGX_CUDADevice * InDevice, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_CUDA_CREATE_DLSSD_EXT1(InDevice, ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_EVALUATE_DLSSD_EXT(NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Eval_Params * pInDlssDEvalParams)
{
    return NGX_CUDA_EVALUATE_DLSSD_EXT(pInHandle, pInParams, pInDlssDEvalParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_OPTIMAL_SETTINGS(NVSDK_NGX_Parameter * pInParams, unsigned int InUserSelectedWidth, unsigned int InUserSelectedHeight, NVSDK_NGX_PerfQuality_Value InPerfQualityValue, unsigned int * pOutRenderOptimalWidth, unsigned int * pOutRenderOptimalHeight, unsigned int * pOutRenderMaxWidth, unsigned int * pOutRenderMaxHeight, unsigned int * pOutRenderMinWidth, unsigned int * pOutRenderMinHeight, float * pOutSharpness)
{
    return NGX_DLSSD_GET_OPTIMAL_SETTINGS(pInParams, InUserSelectedWidth, InUserSelectedHeight, InPerfQualityValue, pOutRenderOptimalWidth, pOutRenderOptimalHeight, pOutRenderMaxWidth, pOutRenderMaxHeight, pOutRenderMinWidth, pOutRenderMinHeight, pOutSharpness);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes)
{
    return NGX_DLSSD_GET_STATS(pInParams, pVRAMAllocatedBytes);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS_1(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel)
{
    return NGX_DLSSD_GET_STATS_1(pInParams, pVRAMAllocatedBytes, pOptLevel);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS_2(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel, unsigned int * IsDevSnippetBranch)
{
    return NGX_DLSSD_GET_STATS_2(pInParams, pVRAMAllocatedBytes, pOptLevel, IsDevSnippetBranch);
}
