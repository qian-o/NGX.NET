#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_vk.h"
#include "nvsdk_ngx_helpers_dlssd_vk.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_CREATE_DLSSD_EXT1(VkDevice InDevice, VkCommandBuffer InCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_VULKAN_CREATE_DLSSD_EXT1(InDevice, InCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_EVALUATE_DLSSD_EXT(VkCommandBuffer InCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_VK_DLSSD_Eval_Params * pInDlssDEvalParams)
{
    return NGX_VULKAN_EVALUATE_DLSSD_EXT(InCmdList, pInHandle, pInParams, pInDlssDEvalParams);
}
