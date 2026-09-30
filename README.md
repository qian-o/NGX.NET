# NGX.NET

[![NuGet](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

NativeAOT-compatible .NET 10 bindings for NVIDIA NGX. One package includes the managed API, `ngx-bridge` and the official DLSS Super Resolution, Ray Reconstruction and Frame Generation libraries.

Windows x64/arm64 supports Direct3D 11, Direct3D 12, Vulkan and CUDA. Linux x64/arm64 supports Vulkan and CUDA. NVIDIA hardware and driver requirements still apply to each feature.

Public types use the `NGX` prefix, such as `NGXResult`, `NGXParameter` and `NGXDLSSCreateParams`. Fields and enum members do not repeat the prefix. Enum members preserve native casing after removing the enum prefix and underscores (for example, `NGXDLSSDepthType.HW`). API method parameters use camelCase while preserving native prefixes and abbreviations.

Use `NGX.D3D11`, `NGX.D3D12`, `NGX.Vulkan`, `NGX.CUDA` and `NGX.Parameter` for the application API. `NGX.DLSS` and `NGX.DLSSD` expose shared helpers. Each native entry point has one unsafe signature, without managed `ref`/`out`, `Span` or direct-return convenience overloads. Native helpers that return structures by value retain their original signatures.

Explicitly mapped mathematical fields use `System.Numerics.Vector2`, `Vector3` and `Matrix4x4` with the native memory layout preserved. These .NET types retain their original names without an `NGX` prefix. RR world-to-view and view-to-clip fields use `Matrix4x4*` for their homogeneous transform semantics; they remain nullable, borrowed pointers. Matrix values are passed as supplied; the bindings do not transpose them or change coordinate conventions.

After initializing NGX with your graphics device and `NGX.RuntimeDirectory` in the feature search paths:

```csharp
using NGX.NET;
using Ngx = NGX.NET.NGX;

unsafe
{
    NGXParameter* capabilities = null;
    Ngx.ThrowIfFailed(Ngx.D3D12.GetCapabilityParameters(&capabilities));

    try
    {
        uint optimalWidth, optimalHeight, maxWidth, maxHeight, minWidth, minHeight;
        float sharpness;
        Ngx.ThrowIfFailed(Ngx.DLSS.GetOptimalSettings(
            capabilities, 2560, 1440, NGXPerfQualityValue.MaxQuality,
            &optimalWidth, &optimalHeight, &maxWidth, &maxHeight,
            &minWidth, &minHeight, &sharpness));
    }
    finally
    {
        Ngx.ThrowIfFailed(Ngx.D3D12.DestroyParameters(capabilities));
    }
}
```

Serialize NGX calls and release features only after their GPU work completes. Use `NGX.Succeeded` / `NGX.Failed` for native results.

String constants, including parameter keys, are exposed as `const string`. Use `NGXMarshal.StringToPtr` and `NGXMarshal.PtrToString` with an explicit `NGXEncoding.Utf8` or `NGXEncoding.NativeWide`. Native wide strings use Windows UTF-16 or Linux UTF-32 `wchar_t`; other platforms reject `NativeWide`. `StringToPtr` encodes the entire managed string, preserves embedded NUL characters and appends a final NUL terminator without a BOM. Null maps to null.

```csharp
unsafe
{
    void* path = NGXMarshal.StringToPtr(Ngx.RuntimeDirectory, NGXEncoding.NativeWide);
    try
    {
        // Pass path to a native wchar_t parameter while the allocation is alive.
        string? copy = NGXMarshal.PtrToString(path, NGXEncoding.NativeWide);
    }
    finally
    {
        NGXMarshal.Free(path);
    }

    // GetResultAsString returns borrowed memory owned by NGX. Do not free it.
    string? description = NGXMarshal.PtrToString(
        Ngx.GetResultAsString(NGXResult.Success), NGXEncoding.NativeWide);
}
```

`StringToPtr` allocates with `NativeMemory.Alloc`; pair each owned allocation with `NGXMarshal.Free`, which uses `NativeMemory.Free`. Reuse an allocated pointer across calls while its value is unchanged, and free it after native code no longer uses it. Reading a pointer does not transfer ownership. Do not free SDK-owned, borrowed or pinned pointers with this helper.

Use `NGXEncoding.Utf8` for parameter-key constants. For example, encoding `NGX.EParameterReserved00` (`"#\x00"`) must preserve the bytes `23 00 00`, including the original embedded NUL and the appended terminator. `PtrToString` reads only through the first NUL, so reading this key returns `"#"`; it is not a lossless round trip for keys containing embedded NUL characters.

[Showcase](Showcase) demonstrates DirectX 12 and Vulkan with Sponza, SR, RR and frame generation. The manual **Update NGX** workflow rebuilds all four native targets and refreshes the checked-in AST and binaries together.

The C# bindings and bridge source use the repository's MIT license. Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
