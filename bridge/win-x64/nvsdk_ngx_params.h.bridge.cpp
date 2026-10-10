#include <stdint.h>
#include <stddef.h>
#include <wchar.h>
#include <string.h>
#include <vulkan/vulkan.h>
#include "nvsdk_ngx.h"
#include "nvsdk_ngx_params.h"

extern "C" __declspec(dllexport) void NGX_Bridge_Parameter_Reset(NVSDK_NGX_Parameter* parameters)
{
    parameters->Reset();
}
