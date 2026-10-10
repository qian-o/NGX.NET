# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

C# bindings for NVIDIA NGX, with managed parameters, structures and output values. The [Showcase](Showcase) provides a rendering example.

## Installation

```sh
dotnet add package NGX.NET
```

Requires .NET 10. The package has no additional runtime NuGet dependencies.

## Example

After initializing D3D12 with your native device, query feature availability:

```csharp
using NGX.NET;

NGXParameter parameters = Ngx.D3D12.GetCapabilityParameters();
int available = Ngx.Parameter.GetI(parameters, Ngx.ParameterSuperSamplingAvailable);

Console.WriteLine($"DLSS available: {available}.");

Ngx.D3D12.DestroyParameters(parameters).CheckError("Ngx.D3D12.DestroyParameters");
```

Functions return `NGXResult` with `out` values; single-output functions also have a value-returning overload. Use `IsSuccess`, `IsFailure` or `CheckError` for SDK result handling.

See [ownership and native inputs](documents/ownership.md) and [results and output overloads](documents/errors.md) for the API contracts. Managed names omit pointer and direction prefixes; parameter setter/getter function-pointer delegates are replaced by `Ngx.Parameter` methods.

## Platforms and binaries

Native NGX calls support `win-x64`, `win-arm64`, `linux-x64` and `linux-arm64`. Assembly loading and managed conversion remain available on other platforms; unsupported platforms are rejected when the native library is resolved.

The package includes `ngx-bridge` and NVIDIA DLSS feature libraries from `native/<rid>/`, packaged in `runtimes/<rid>/native/`. Use `Ngx.RuntimeDirectory` in the initialization path list. Keep GPU resources and their native views alive for the entire submitted work lifetime. Binding-owned inputs are retained only when the SDK keeps their addresses; D3D11/D3D12 DLSS evaluation uses allocation-free stack conversion.

## Updating bindings

Run the **Update NGX** GitHub Actions workflow to obtain one SDK release, all four native targets and `ast.json`. The workflow commits the exact generated bridge translation units and linker inputs to `bridge/<rid>/`, outside Git LFS, and writes the package version in `NuGet.Packaging.props`. The first bridge sources are produced by CI.

Updates are submitted from a dedicated branch as a pull request after all checks pass. Enable **Allow GitHub Actions to create and approve pull requests** in the repository's Actions settings. Python 3.13.16, matching LLVM/libclang 21.1.7 and Action commit SHAs are fixed; runner OS labels are explicit. Hosted image revisions and Visual Studio minor versions remain maintained by GitHub.

Regenerate and verify locally:

```sh
dotnet run --project NGX.NET.Generator -- .
dotnet run --project verification/Generation -- .
dotnet build NGX.NET.slnx -c Release --warnaserror
dotnet format NGX.NET.slnx --no-restore --verify-no-changes --include-generated --exclude NGX.NET.Generator/obj NGX.NET/obj Showcase/obj --severity warn
python3 verification/Marshalling/run.py
```

These checks validate managed conversion, ownership, layouts, enum values, import names and generated code. They do not execute NVIDIA GPU work or validate NativeAOT execution.

## License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms. See the [NVIDIA SDK](https://github.com/NVIDIA/DLSS).
