#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_cuda.h"
#include "nvsdk_ngx_helpers_dlssd_cuda.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT(NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_CUDA_CREATE_DLSSD_EXT(ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_CREATE_DLSSD_EXT1(NVSDK_NGX_CUDADevice * InDevice, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Create_Params * pInDlssDCreateParams)
{
    return NGX_CUDA_CREATE_DLSSD_EXT1(InDevice, ppOutHandle, pInParams, pInDlssDCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_EVALUATE_DLSSD_EXT(NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLSSD_Eval_Params * pInDlssDEvalParams)
{
    return NGX_CUDA_EVALUATE_DLSSD_EXT(pInHandle, pInParams, pInDlssDEvalParams);
}
