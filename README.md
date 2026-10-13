# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

C# bindings for NVIDIA NGX, including DLSS Super Resolution, Ray Reconstruction and Frame Generation.

## Installation

```sh
dotnet add package NGX.NET
```

Requires .NET 10. Native binaries are included for Windows and Linux on x64 and ARM64. DLSS availability depends on the GPU and driver.

## Usage

Use `Ngx.D3D11`, `Ngx.D3D12`, `Ngx.Vulkan` or `Ngx.CUDA` to call NGX. The bindings preserve native signatures and result codes, with convenience overloads for strings and `in`/`ref`/`out` parameters.

See the [Showcase](Showcase) for a complete DirectX 12 and Vulkan integration.

- Add `Ngx.RuntimeDirectory` to NGX's initialization search paths to load the bundled feature libraries.
- Check results with `Ngx.NVSDK_NGX_SUCCEED` or `Ngx.NVSDK_NGX_FAILED` before reading outputs.
- Use `new()` for SDK structure defaults; `default` produces zero-initialized data.
- Keep GPU resources, callbacks and native addresses valid while NGX uses them. Pin managed data when NGX retains its address across calls.
- Use `NGXStrings` for stable native strings. String overloads allocate temporary strings for each call.
- Release features, destroy parameter tables and shut down NGX explicitly. Copying handles or structures does not transfer ownership.

## Updating

Run **Update NGX** in GitHub Actions to update the SDK, native binaries and generated bindings. It validates all four targets before committing the update and deletes temporary artifacts when finished.

To regenerate bindings from the checked-in AST:

```sh
dotnet run --project NGX.NET.Generator -- .
dotnet build NGX.NET.slnx -c Release --warnaserror
```

## License

Bindings and build tooling: [MIT](LICENSE). Bundled NVIDIA components retain their [NVIDIA SDK terms](https://github.com/NVIDIA/DLSS).
