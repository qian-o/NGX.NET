# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

C# bindings for NVIDIA NGX, including DLSS Super Resolution, Ray Reconstruction and Frame Generation.

## Installation

```sh
dotnet add package NGX.NET
```

Requires .NET 10. Supports `win-x64`, `win-arm64`, `linux-x64` and `linux-arm64`.

To automatically download and stage official DLSS binaries (`nvngx_dlss*` / `libnvidia-ngx-dlss*`) on build and publish, add to your `.csproj`:

```xml
<PropertyGroup>
    <NgxDownloadRuntime>true</NgxDownloadRuntime>
</PropertyGroup>
```

Binaries are verified with SHA-256, cached in `LocalApplicationData/NGX.NET/cache`, and staged according to `<RuntimeIdentifier>` / `<RuntimeIdentifiers>`. DLSS availability depends on the GPU and driver.

## Usage

Use `Ngx.D3D11`, `Ngx.D3D12`, `Ngx.Vulkan` or `Ngx.CUDA` to call NGX. Signatures and result codes match the native SDK, with overloads for strings and `in`/`ref`/`out` parameters.

- Pass `Ngx.RuntimeDirectory` in `NGXFeatureCommonInfo.PathListInfo` when enabling `NgxDownloadRuntime`, or provide your own feature library path.
- Check results with `Ngx.NVSDK_NGX_SUCCEED` or `Ngx.NVSDK_NGX_FAILED` before reading outputs.
- Use `new()` for SDK structure defaults; `default` zero-initializes.
- Keep GPU resources, callbacks and native addresses valid while NGX uses them. Use `NGXStrings` for persistent native strings.
- Release features, destroy parameter tables and shut down NGX explicitly.

## Updating

Run **Update NGX** in GitHub Actions to update the SDK, native bridges, runtime manifest and generated bindings.

To regenerate locally from `NGX.NET.Generator/ast.json` and build the package:

```sh
dotnet run --project NGX.NET.Generator -- .
dotnet build NGX.NET.slnx -c Release --warnaserror
python .github/scripts/build.py verify --root .
```

## License

Bindings and tooling: [MIT](LICENSE). Official NVIDIA DLSS runtime binaries remain under the [NVIDIA SDK terms](https://github.com/NVIDIA/DLSS).
