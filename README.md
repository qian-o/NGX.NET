# Streamline.NET

Independent .NET 10 C# wrapper for NVIDIA Streamline. It preserves the native
application API and adds focused `in`/`ref`/`out`, string and Span overloads.

The current interface snapshot is Streamline **v2.14.1**, commit
`2122257e0fce486f91b385aa63b9a09b0a34b363`. The snapshot records the extraction
configuration, dependency sources, layouts, declarations and calling conventions.

## Package

`Streamline.NET` is one managed NuGet package with no additional runtime NuGet
dependencies. It includes the assembly, XML documentation, README and MIT license.
NVIDIA runtime files and SDK headers are not distributed with it.

The application supplies the Streamline runtime and plugin paths. Configure the
absolute interposer path before the first SDK call:

```csharp
using Streamline.NET;

SL.SetLibraryPath(@"C:\MyApplication\Streamline\sl.interposer.dll");
```

This only stores the path. The first SDK call loads that file; subsequent path
changes are rejected. Plugin search paths remain controlled by
`Preferences.PathsToPlugins`. The wrapper does not initialize the SDK, associate
a device, change PATH, wait for GPU work, or choose a fallback feature.

After application initialization and device association:

```csharp
DLSSOptions options = new()
{
    Mode = DLSSMode.MaxQuality,
    OutputWidth = 1920,
    OutputHeight = 1080
};

DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
```

Feature operations and their dedicated helpers are grouped under `SL.DLSS`,
`SL.DLSSD`, `SL.DLSSG`, `SL.Reflex`, `SL.PCL`, `SL.NIS`, `SL.DeepDVC` and
`SL.DirectSR`. Core operations remain on `SL`; data types keep their existing names.

Value-returning overloads initialize the output structure with `new()` and return
it only for `SLResult.Ok`. Other results, including non-success warnings, throw
`SLException`; its `Result` and `NativeFunction` properties retain the SDK result
and operation name. Library loading and missing fixed exports retain their .NET
exception types.

Pointer entry points and result-returning reference overloads remain available
under the same grouped method names. Use the reference form for a custom structure
version or `Next` chain, or to handle SDK results and partial outputs yourself:

```csharp
DLSSOptimalSettings settings = new();
SLResult result = SL.DLSS.GetOptimalSettings(in options, ref settings);
```

Plugin entry points are queried through the SDK, cached only after a
successful query, and invalidated on successful shutdown or plugin-load changes.

## Types and lifetime

- `new T()` runs the native defaults and structure-header initialization represented
  by the snapshot. `default(T)` and newly allocated arrays are CLR-zeroed; initialize
  each versioned array element before passing it to the SDK.
- `Bool8` represents native C++ `bool`. `SLBoolean` preserves the separate native
  tri-state enumeration.
- `FrameToken` and `IAllocator` borrow SDK object addresses. Their methods use the
  recorded virtual dispatch contract. Copying the wrapper does not acquire ownership.
- `SLArray<T>` owns native storage through its original allocator. Follow its native
  non-copying constraint and call `Destroy` on the owning value. Element copying
  corresponds to the original `copyFrom`/`copyTo` helpers.
- References, spans and temporary strings are pinned or allocated only for the
  call. Nested pointers, callbacks, resources and heterogeneous structure chains
  retain their original lifetime requirements. Managed exceptions must not cross
  an unmanaged callback boundary.
- Native structure-chain nodes implement `ISLStructure`. External Vulkan structures
  have their own native layout and no Streamline header. Only the required Vulkan
  definitions and constants are included.

Pure data, strings, presets, structure-chain, math and Vulkan helpers do not load
Streamline. The Windows signature helpers preserve the separate trust and NVIDIA
identity checks and release their system resources. `RecalculateCameraMatrices`
retains the upstream shared history; it is not thread-safe or isolated by viewport.

The original result macros map to ordinary C# control flow with single evaluation:

```csharp
SLResult result = operation();
if (result != SLResult.Ok)
{
    return result;
}
```

For native flag `operator&` semantics, use `SL.HasAnyFlags(value, mask)` or
`(value & mask) != 0`. Native `to_underlying` maps to a cast to the enum's extracted
underlying integer type.

## Development

The solution contains the wrapper and `Streamline.NET.Generator`. Project settings
and development package versions are centralized, following the Metal.NET layout.

Interface extraction runs **only in GitHub Actions**:

```sh
gh workflow run extract-streamline-api.yml --ref master
gh run list --workflow extract-streamline-api.yml
```

A successful run produces one `streamline-ast` artifact containing
`Streamline.NET.Generator/streamline-ast.json`. Review and import that JSON into the
repository, then regenerate locally:

```sh
dotnet run --project Streamline.NET.Generator -c Release
dotnet build Streamline.NET.slnx -c Release --warnaserror
dotnet run --project .github/validation/WrapperChecks.csproj -c Release -- .
python3 .github/scripts/verify_package.py
```

Local generation reads the committed snapshot without downloading SDK inputs.
Once the development dependencies are restored, use `--no-restore` for an offline
run. Generated files use UTF-8 BOM and the `.g.cs` suffix; changes to generated
behavior belong in the generator. Handwritten translations are tied to reviewed
upstream body hashes in `ManualImplementations.json`.

`declaration-coverage.json` records every declaration's implementation or exclusion
and every macro's C# representation. Unknown types, conventions, changed handwritten
bodies, and unhandled declarations stop generation. The generator reports added,
changed and removed declarations and only updates its own output files.

Validation helpers are run manually. GitHub Actions is limited to interface
extraction. Real SDK/GPU functionality is not reported as runtime-tested by the
local checks.

See [the design](https://github.com/qian-o/Streamline.NET/blob/master/Streamline.NET.Design.md) for the full contract and
[the delivery record](https://github.com/qian-o/Streamline.NET/blob/master/Streamline.NET.Delivery.md) for verified results and limits.

## License

MIT. See [LICENSE](https://github.com/qian-o/Streamline.NET/blob/master/LICENSE).
