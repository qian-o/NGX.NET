#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_vk.h"

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_CREATE_DLISP_EXT(VkCommandBuffer InCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_Feature_Create_Params * pInDlispCreateParams)
{
    return NGX_VULKAN_CREATE_DLISP_EXT(InCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlispCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT(VkCommandBuffer InCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSS_Create_Params * pInDlssCreateParams)
{
    return NGX_VULKAN_CREATE_DLSS_EXT(InCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_CREATE_DLSS_EXT1(VkDevice InDevice, VkCommandBuffer InCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSS_Create_Params * pInDlssCreateParams)
{
    return NGX_VULKAN_CREATE_DLSS_EXT1(InDevice, InCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_EVALUATE_DLISP_EXT(VkCommandBuffer InCmdList, NVSDK_NGX_Handle * InHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_VK_DLISP_Eval_Params * pInDlispEvalParams)
{
    return NGX_VULKAN_EVALUATE_DLISP_EXT(InCmdList, InHandle, pInParams, pInDlispEvalParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_VULKAN_EVALUATE_DLSS_EXT(VkCommandBuffer InCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_VK_DLSS_Eval_Params * pInDlssEvalParams)
{
    return NGX_VULKAN_EVALUATE_DLSS_EXT(InCmdList, pInHandle, pInParams, pInDlssEvalParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Resource_VK NGX_Bridge_NVSDK_NGX_Create_Buffer_Resource_VK(VkBuffer buffer, unsigned int sizeInBytes, bool readWrite)
{
    return NVSDK_NGX_Create_Buffer_Resource_VK(buffer, sizeInBytes, readWrite);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Resource_VK NGX_Bridge_NVSDK_NGX_Create_ImageView_Resource_VK(VkImageView imageView, VkImage image, VkImageSubresourceRange subresourceRange, VkFormat format, unsigned int width, unsigned int height, bool readWrite)
{
    return NVSDK_NGX_Create_ImageView_Resource_VK(imageView, image, subresourceRange, format, width, height, readWrite);
}
