# NGX.NET

[![NuGet](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

NativeAOT-compatible .NET 10 bindings for NVIDIA NGX. One package includes the managed API, `ngx-bridge` and the official DLSS Super Resolution, Ray Reconstruction and Frame Generation libraries.

Windows x64/arm64 supports Direct3D 11, Direct3D 12, Vulkan and CUDA. Linux x64/arm64 supports Vulkan and CUDA. NVIDIA hardware and driver requirements still apply to each feature.

Public types use the `NGX` prefix, such as `NGXResult`, `NGXParameter` and `NGXDLSSCreateParams`. Fields and enum members do not repeat the prefix.

Use `NGX.D3D11`, `NGX.D3D12`, `NGX.Vulkan`, `NGX.CUDA` and `NGX.Parameter` for the application API. `NGX.DLSS` and `NGX.DLSSD` expose shared helpers. Native pointers remain available alongside `ref`/`out` and reviewed result-returning overloads.

After initializing NGX with your graphics device and `NGX.RuntimeDirectory` in the feature search paths:

```csharp
using NGX.NET;
using Ngx = NGX.NET.NGX;

unsafe
{
    NGXParameter* capabilities = Ngx.D3D12.GetCapabilityParameters();

    try
    {
        NGXOptimalSettings settings = Ngx.DLSS.GetOptimalSettings(
            capabilities, 2560, 1440, NGXPerfQualityValue.MaxQuality);
    }
    finally
    {
        Ngx.ThrowIfFailed(Ngx.D3D12.DestroyParameters(capabilities));
    }
}
```

Serialize NGX calls and release features only after their GPU work completes. Use `NGX.Succeeded` / `NGX.Failed` for native results. `NGXNativeWideString` handles Windows UTF-16 and Linux UTF-32 `wchar_t` strings.

[Showcase](Showcase) demonstrates DirectX 12 and Vulkan with Sponza, SR, RR and frame generation. The manual **Update NGX** workflow rebuilds all four native targets and refreshes the checked-in AST and binaries together.

The C# bindings and bridge source use the repository's MIT license. Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
