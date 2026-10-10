#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_d3d.h"
#include "nvsdk_ngx_helpers_dlssd_d3d.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_CREATE_DLSSD_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_D3D11_CREATE_DLSSD_EXT(pInCtx, ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_EVALUATE_DLSSD_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D11_DLSSD_Eval_Params * pInDlssDEvalParams)
{
    return NGX_D3D11_EVALUATE_DLSSD_EXT(pInCtx, pInHandle, pInParams, pInDlssDEvalParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_CREATE_DLSSD_EXT(ID3D12GraphicsCommandList * pInCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_D3D12_CREATE_DLSSD_EXT(pInCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_EVALUATE_DLSSD_EXT(ID3D12GraphicsCommandList * pInCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D12_DLSSD_Eval_Params * pInDlssDEvalParams)
{
    return NGX_D3D12_EVALUATE_DLSSD_EXT(pInCmdList, pInHandle, pInParams, pInDlssDEvalParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_OPTIMAL_SETTINGS(NVSDK_NGX_Parameter * pInParams, unsigned int InUserSelectedWidth, unsigned int InUserSelectedHeight, NVSDK_NGX_PerfQuality_Value InPerfQualityValue, unsigned int * pOutRenderOptimalWidth, unsigned int * pOutRenderOptimalHeight, unsigned int * pOutRenderMaxWidth, unsigned int * pOutRenderMaxHeight, unsigned int * pOutRenderMinWidth, unsigned int * pOutRenderMinHeight, float * pOutSharpness)
{
    return NGX_DLSSD_GET_OPTIMAL_SETTINGS(pInParams, InUserSelectedWidth, InUserSelectedHeight, InPerfQualityValue, pOutRenderOptimalWidth, pOutRenderOptimalHeight, pOutRenderMaxWidth, pOutRenderMaxHeight, pOutRenderMinWidth, pOutRenderMinHeight, pOutSharpness);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes)
{
    return NGX_DLSSD_GET_STATS(pInParams, pVRAMAllocatedBytes);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS_1(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel)
{
    return NGX_DLSSD_GET_STATS_1(pInParams, pVRAMAllocatedBytes, pOptLevel);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_DLSSD_GET_STATS_2(NVSDK_NGX_Parameter * pInParams, unsigned long long * pVRAMAllocatedBytes, unsigned int * pOptLevel, unsigned int * IsDevSnippetBranch)
{
    return NGX_DLSSD_GET_STATS_2(pInParams, pVRAMAllocatedBytes, pOptLevel, IsDevSnippetBranch);
}
