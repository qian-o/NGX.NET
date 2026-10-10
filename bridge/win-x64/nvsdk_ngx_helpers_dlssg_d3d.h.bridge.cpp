#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_dlssg_d3d.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_CREATE_DLSSG(ID3D12GraphicsCommandList * pInCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSSG_Create_Params * pInDlssgCreateParams)
{
    return NGX_D3D12_CREATE_DLSSG(pInCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssgCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_ESTIMATE_VRAM_DLSSG(NVSDK_NGX_Parameter * InParams, uint32_t mvecDepthWidth, uint32_t mvecDepthHeight, uint32_t colorWidth, uint32_t colorHeight, uint32_t colorBufferFormat, uint32_t mvecBufferFormat, uint32_t depthBufferFormat, uint32_t hudLessBufferFormat, uint32_t uiBufferFormat, size_t * estimatedVRAMInBytes)
{
    return NGX_D3D12_ESTIMATE_VRAM_DLSSG(InParams, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, estimatedVRAMInBytes);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_EVALUATE_DLSSG(ID3D12GraphicsCommandList * pInCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D12_DLSSG_Eval_Params * pInDlssgEvalParams, NVSDK_NGX_DLSSG_Opt_Eval_Params * pInDlssgOptEvalParams)
{
    return NGX_D3D12_EVALUATE_DLSSG(pInCmdList, pInHandle, pInParams, pInDlssgEvalParams, pInDlssgOptEvalParams);
}
