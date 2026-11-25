# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/v/NGX.NET)](https://www.nuget.org/packages/NGX.NET)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

Low-level, zero-overhead C# bindings for **NVIDIA NGX** (**DLSS Super Resolution**, **Ray Reconstruction**, and **Frame Generation**), auto-generated from the official NVIDIA DLSS SDK headers.

## Features

- **Multi-Backend & Cross-Platform** — Covers D3D11, D3D12, Vulkan, and CUDA across `win-x64`, `win-arm64`, `linux-x64`, and `linux-arm64`.
- **Zero-Overhead Interop** — `LibraryImport` P/Invokes, `readonly struct` handles, `unmanaged` function pointers, and `ReadOnlySpan<byte>` parameter keys.
- **On-Demand Runtime Staging** — Ships only the minimal `ngx-bridge` library; stage official or local DLSS runtime binaries via `<NGXRuntime>`.
- **Native AOT Ready** — Built with `IsAotCompatible` and zero external dependencies.

## Installation

```bash
dotnet add package NGX.NET
```

### Runtime Binaries

Add `<NGXRuntime>` to your `.csproj` to stage DLSS runtime libraries (`nvngx_dlss*` / `libnvidia-ngx-dlss*`) on build and publish:

```xml
<ItemGroup>
    <!-- Download official binaries (cached in system temp): All | win-x64 | win-arm64 | linux-x64 | linux-arm64 -->
    <NGXRuntime Include="win-x64" />

    <!-- Or copy from a local directory or file -->
    <!--
    <NGXRuntime Include="win-x64">
        <Path>path\to\dlss\lib\Windows_x86_64\rel</Path>
    </NGXRuntime>
    -->
</ItemGroup>
```

## Quick Start

```csharp
using NGX.NET;

using NGXStrings strings = new();
void** paths = strings.AllocWide([Ngx.RuntimeDirectory]);

NGXFeatureCommonInfo commonInfo = new()
{
    PathListInfo = new() { Path = paths, Length = 1 }
};

NGXResult result = Ngx.D3D12.Init(applicationId, applicationDataPath, d3d12Device, &commonInfo);
if (Ngx.NVSDK_NGX_FAILED(result))
{
    throw new InvalidOperationException(Ngx.GetResultString(result));
}

if (Ngx.NVSDK_NGX_SUCCEED(Ngx.D3D12.GetCapabilityParameters(out NGXParameter parameters)))
{
    Ngx.DLSS.GetOptimalSettings(
        parameters, targetWidth, targetHeight, NGXPerfQualityValue.MaxQuality,
        out uint renderWidth, out uint renderHeight,
        out _, out _, out _, out _, out _);
}
```

## Building

```bash
dotnet run --project NGX.NET.Generator -- .
dotnet build NGX.NET.slnx -c Release
```

## License

[MIT](LICENSE). Official NVIDIA DLSS runtime binaries remain subject to the [NVIDIA DLSS SDK License](https://github.com/NVIDIA/DLSS).
