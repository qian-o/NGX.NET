#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_dlssg_vk.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_VK_CREATE_DLSSG(VkCommandBuffer pInCmdBuf, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSSG_Create_Params * pInDlssgCreateParams)
{
    return NGX_VK_CREATE_DLSSG(pInCmdBuf, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssgCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_VK_ESTIMATE_VRAM_DLSSG(NVSDK_NGX_Parameter * InParams, uint32_t mvecDepthWidth, uint32_t mvecDepthHeight, uint32_t colorWidth, uint32_t colorHeight, uint32_t colorBufferFormat, uint32_t mvecBufferFormat, uint32_t depthBufferFormat, uint32_t hudLessBufferFormat, uint32_t uiBufferFormat, size_t * estimatedVRAMInBytes)
{
    return NGX_VK_ESTIMATE_VRAM_DLSSG(InParams, mvecDepthWidth, mvecDepthHeight, colorWidth, colorHeight, colorBufferFormat, mvecBufferFormat, depthBufferFormat, hudLessBufferFormat, uiBufferFormat, estimatedVRAMInBytes);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_VK_EVALUATE_DLSSG(VkCommandBuffer pInCmdBuf, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_VK_DLSSG_Eval_Params * pInDlssgEvalParams, NVSDK_NGX_DLSSG_Opt_Eval_Params * pInDlssgOptEvalParams)
{
    return NGX_VK_EVALUATE_DLSSG(pInCmdBuf, pInHandle, pInParams, pInDlssgEvalParams, pInDlssgOptEvalParams);
}
