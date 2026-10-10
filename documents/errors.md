# Results and output overloads

Every status-returning function exposes `NGXResult`, with output values passed through `out`. `IsSuccess` and `IsFailure` use the SDK macros: a result fails when `(value & 0xFFF00000)` equals `NGXResult.Fail`. Existing failure enum values follow this mask; other values, including zero, count as success.

Call `result.CheckError(operation)` to throw `NGXException` on failure. The optional operation names the call in the exception message; `NGXException.Result` preserves the SDK code. Callers can inspect status directly for expected failures such as capability probes.

Only functions with exactly one managed output also provide a value-returning overload. It forwards to the same status-returning method, calls `CheckError`, and returns that output. Functions without outputs or with multiple outputs keep the status-returning API. There are no aggregate result structures or tuple replacements.

Failure resets scalar, handle and structure outputs to `default`. Extension arrays preserve the established empty-array output rather than returning null. Initialization and evaluation use the same result rules for retained memory; see [ownership](ownership.md).

```csharp
Ngx.Vulkan.RequiredExtensions(out string[] instanceExtensions, out string[] deviceExtensions).CheckError("Ngx.Vulkan.RequiredExtensions");
Ngx.DLSS.GetOptimalSettings(parameters, width, height, quality, out uint renderOptimalWidth, out uint renderOptimalHeight, out uint renderMaxWidth, out uint renderMaxHeight, out uint renderMinWidth, out uint renderMinHeight, out float sharpness).CheckError("Ngx.DLSS.GetOptimalSettings");
```
