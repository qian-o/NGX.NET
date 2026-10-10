#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_cuda.h"

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_CREATE_DLISP_EXT(NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_Feature_Create_Params * pDlispCreateParams)
{
    return NGX_CUDA_CREATE_DLISP_EXT(ppOutHandle, pInParams, pDlispCreateParams);
}

extern "C" __attribute__((visibility("default"))) NVSDK_NGX_Result NGX_Bridge_NGX_CUDA_EVALUATE_DLISP_EXT(NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_CUDA_DLISP_Eval_Params * pDlispEvalParams)
{
    return NGX_CUDA_EVALUATE_DLISP_EXT(pInHandle, pInParams, pDlispEvalParams);
}
