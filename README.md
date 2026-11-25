# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/v/NGX.NET)](https://www.nuget.org/packages/NGX.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Low-level, zero-overhead C# bindings for **NVIDIA NGX** (**DLSS Super Resolution**, **Ray Reconstruction**, and **Frame Generation**), auto-generated from the official NVIDIA DLSS SDK headers via Clang AST extraction.

## Features

- **Multi-Backend & Cross-Platform** — Full coverage of `Ngx.D3D11`, `Ngx.D3D12`, `Ngx.Vulkan`, `Ngx.CUDA`, `Ngx.Parameter`, `Ngx.DLSS`, and `Ngx.DLSSD` across `win-x64`, `win-arm64`, `linux-x64`, and `linux-arm64`.
- **Lightweight Core + Flexible Runtime Staging** — Ships only the minimal `ngx-bridge` native library in the NuGet package; add `<NGXRuntime>` items in your `.csproj` to automatically download, SHA-256 verify, and cache official binaries in the system temporary directory, or copy local SDK/dev/custom runtime binaries directly.
- **Zero-Overhead Interop** — `LibraryImport` P/Invokes, `readonly struct` handles (`NGXHandle`, `NGXParameter`), `unmanaged` function pointers, zero-allocation `ReadOnlySpan<byte>` parameter keys, and ergonomic `in`/`out`/string overloads.
- **Native AOT & Trimming Ready** — Built with `IsAotCompatible` enabled and zero external dependencies.

## Installation

```bash
dotnet add package NGX.NET
```

> **Requirements:** .NET 10+, Windows (`x64`, `arm64`) or Linux (`x64`, `arm64`), and a compatible NVIDIA GPU and driver.

### Runtime Binaries

By default, `NGX.NET` only includes `ngx-bridge`. Declare `<NGXRuntime>` items in your `.csproj` to stage official or local DLSS runtime libraries (`nvngx_dlss*` / `libnvidia-ngx-dlss*`) during `build` and `publish`:

```xml
<ItemGroup>
    <!-- Omit <Path> to download official binaries for a specific RID (or All) into Path.GetTempPath() -->
    <NGXRuntime Include="win-x64" />

    <!-- Or set <Path> to copy a local SDK/dev/rel folder or specific DLL/SO files without network access -->
    <!--
    <NGXRuntime Include="win-x64">
        <Path>path\to\dlss\lib\Windows_x86_64\rel</Path>
    </NGXRuntime>
    -->
</ItemGroup>
```

- **Online Download (`<Path>` omitted or empty)** — Supported `Include` values are `All`, `None`, `win-x64`, `win-arm64`, `linux-x64`, and `linux-arm64`. Official binaries are verified against pinned SHA-256 digests and cached under `<SystemTemp>/NGX.NET/cache/<version>`.
- **Local Directory or Files (`<Path>` specified)** — For a concrete RID (`win-x64`, `linux-x64`, etc.), `<Path>` can point to either a directory of `.dll`/`.so*` binaries or an individual library file (repeat the item for multiple files). For `Include="All"`, `<Path>` expects `<rid>/` subdirectories (such as `win-x64/` and `linux-x64/`).
- **Overrides, Packaging & Single-File Publish** — Concrete RID entries override matching RIDs from `All`, `Include="None"` disables inherited entries, `ProjectReference` consumers inherit staged runtimes cleanly, `dotnet pack` never bundles downloaded or copied runtime binaries, `<RuntimeIdentifier>` filters staged targets automatically, and runtime binaries stay side-by-side during `PublishSingleFile` builds.

## Quick Start

```csharp
using NGX.NET;

using NGXStrings strings = new();
void** paths = strings.AllocWide([Ngx.RuntimeDirectory]);

NGXFeatureCommonInfo commonInfo = new()
{
    PathListInfo = new()
    {
        Path = paths,
        Length = 1
    }
};

NGXResult result = Ngx.D3D12.Init(applicationId, applicationDataPath, d3d12Device, &commonInfo);
if (Ngx.NVSDK_NGX_FAILED(result))
{
    throw new InvalidOperationException(Ngx.GetResultString(result));
}

if (Ngx.NVSDK_NGX_SUCCEED(Ngx.D3D12.GetCapabilityParameters(out NGXParameter parameters)))
{
    Ngx.DLSS.GetOptimalSettings(
        parameters,
        targetWidth,
        targetHeight,
        NGXPerfQualityValue.MaxQuality,
        out uint renderWidth,
        out uint renderHeight,
        out _,
        out _,
        out _,
        out _,
        out _);
}
```

## Usage Notes

- Pass `Ngx.RuntimeDirectory` in `NGXFeatureCommonInfo.PathListInfo` when staging runtime binaries via `<NGXRuntime>`, or supply your own feature search paths.
- Check return codes with `Ngx.NVSDK_NGX_SUCCEED` / `Ngx.NVSDK_NGX_FAILED` and inspect failures via `Ngx.GetResultString`.
- Use `new()` when SDK structs define non-zero defaults; `default` zero-initializes all fields.
- Keep native buffers, GPU resources, callbacks, and `NGXStrings` allocations alive while NGX references them.
- Release features, destroy allocated `NGXParameter` instances, and shut down NGX explicitly before releasing the graphics device.

## Building & Regenerating Bindings

Run the **Update NGX** GitHub Actions workflow to pull the latest NVIDIA DLSS SDK, rebuild native bridges across all 4 platforms, refresh the runtime manifest, and regenerate C# bindings.

To regenerate locally from `NGX.NET.Generator/ast.json` and verify the package:

```bash
dotnet run --project NGX.NET.Generator -- .
dotnet build NGX.NET.slnx -c Release --warnaserror
python3 .github/scripts/build.py verify --root .
```

## License

Bindings and tooling are licensed under [MIT](LICENSE). Official NVIDIA DLSS SDK and runtime binaries remain subject to the [NVIDIA DLSS SDK License](https://github.com/NVIDIA/DLSS).
