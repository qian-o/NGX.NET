# Results and output overloads

Methods returning `NGXResult` with output parameters provide the complete status-returning API. Compare the result with `NGXResult.Success`; only that value is currently treated as success. Scalar and structure outputs remain `default` on failure, while extension arrays remain empty arrays.

Functions with outputs also have a value-returning overload that throws `NGXException` on failure. One output is returned directly. Multiple outputs use generated readonly result structures with get-only properties. The overload calls the same managed status-returning method, preserving conversion, cleanup and ownership. `NGXException.Result` stores the SDK result, and its message identifies the operation. Functions without outputs return `NGXResult`.

Result type names derive from managed output signatures after array counts are folded into arrays. A leading `Get`, `Query`, `Enumerate`, `Create`, `Allocate`, `Estimate`, `Calculate` or `Required` word is removed; other methods receive a `Result` suffix. Numeric version suffixes remain, such as `GetStats1` producing `Stats1`.

Property names remove pointer and output prefixes, expand `Exts` to `Extensions`, and preserve output order and types. Identical names share a result definition only when property names, types and order all match, as with DLSS/DLSSD `OptimalSettings`. Conflicting result names, duplicate properties and overload signatures stop generation with a diagnostic. Result structures do not make returned arrays immutable.

```csharp
Ngx.Vulkan.RequiredExtensions(out string[] instanceExtensions, out string[] deviceExtensions);
Ngx.DLSS.GetOptimalSettings(parameters, width, height, quality, out uint renderOptimalWidth, out uint renderOptimalHeight, out uint renderMaxWidth, out uint renderMaxHeight, out uint renderMinWidth, out uint renderMinHeight, out float sharpness);
```
