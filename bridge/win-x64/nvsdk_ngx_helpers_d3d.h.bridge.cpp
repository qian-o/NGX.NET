#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_helpers_d3d.h"

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_CREATE_DLISP_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_Feature_Create_Params * pDlispCreateParams)
{
    return NGX_D3D11_CREATE_DLISP_EXT(pInCtx, ppOutHandle, pInParams, pDlispCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_CREATE_DLSS_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSS_Create_Params * pInDlssCreateParams)
{
    return NGX_D3D11_CREATE_DLSS_EXT(pInCtx, ppOutHandle, pInParams, pInDlssCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_EVALUATE_DLISP_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D11_DLISP_Eval_Params * pDlispEvalParams)
{
    return NGX_D3D11_EVALUATE_DLISP_EXT(pInCtx, pInHandle, pInParams, pDlispEvalParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D11_EVALUATE_DLSS_EXT(ID3D11DeviceContext * pInCtx, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D11_DLSS_Eval_Params * pInDlssEvalParams)
{
    return NGX_D3D11_EVALUATE_DLSS_EXT(pInCtx, pInHandle, pInParams, pInDlssEvalParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_CREATE_DLISP_EXT(ID3D12GraphicsCommandList * InCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_Feature_Create_Params * pDlispCreateParams)
{
    return NGX_D3D12_CREATE_DLISP_EXT(InCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pDlispCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_CREATE_DLSS_EXT(ID3D12GraphicsCommandList * pInCmdList, unsigned int InCreationNodeMask, unsigned int InVisibilityNodeMask, NVSDK_NGX_Handle ** ppOutHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_DLSS_Create_Params * pInDlssCreateParams)
{
    return NGX_D3D12_CREATE_DLSS_EXT(pInCmdList, InCreationNodeMask, InVisibilityNodeMask, ppOutHandle, pInParams, pInDlssCreateParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_EVALUATE_DLISP_EXT(ID3D12GraphicsCommandList * pInCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D12_DLISP_Eval_Params * pDlispEvalParams)
{
    return NGX_D3D12_EVALUATE_DLISP_EXT(pInCmdList, pInHandle, pInParams, pDlispEvalParams);
}

extern "C" __declspec(dllexport) NVSDK_NGX_Result NGX_Bridge_NGX_D3D12_EVALUATE_DLSS_EXT(ID3D12GraphicsCommandList * pInCmdList, NVSDK_NGX_Handle * pInHandle, NVSDK_NGX_Parameter * pInParams, NVSDK_NGX_D3D12_DLSS_Eval_Params * pInDlssEvalParams)
{
    return NGX_D3D12_EVALUATE_DLSS_EXT(pInCmdList, pInHandle, pInParams, pInDlssEvalParams);
}
