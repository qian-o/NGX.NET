# Streamline.NET

Independent .NET 10 C# wrapper for NVIDIA Streamline, with feature groups and
`in`/`ref`/`out`, string and Span overloads.

The interface snapshot targets **Streamline v2.14.1**, commit
`2122257e0fce486f91b385aa63b9a09b0a34b363`. Native declarations, layouts, defaults
and calling conventions are recorded in `Streamline.NET.Generator/streamline-ast.json`.

## Using the wrapper

`Streamline.NET` is a managed package with no additional runtime NuGet dependencies.
Your application supplies NVIDIA's runtime and plugins, initializes Streamline and
associates its graphics device. See the [official integration guide](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuide.md)
for that sequence.

Set the absolute interposer path before the first SDK call:

```csharp
using Streamline.NET;

SL.SetLibraryPath(@"C:\MyApplication\Streamline\sl.interposer.dll");
```

The first SDK call loads the library; subsequent path changes are rejected.
Plugin directories are configured through `Preferences.PathsToPlugins`.

After SDK initialization and device association:

```csharp
DLSSOptions options = new()
{
    Mode = DLSSMode.MaxQuality,
    OutputWidth = 1920,
    OutputHeight = 1080
};

DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
```

Feature operations are grouped under `SL.DLSS`, `SL.DLSSD`, `SL.DLSSG`,
`SL.Reflex`, `SL.PCL`, `SL.NIS`, `SL.DeepDVC` and `SL.DirectSR`. Core operations
remain on `SL`; structures and enums retain their top-level names.

Convenience overloads initialize output structures with `new()` and return them
only for `SLResult.Ok`. Other results throw `SLException`, which exposes the
`Result` and `NativeFunction`. Pointer and result-returning reference overloads
preserve direct control over errors, extension chains and partial outputs:

```csharp
DLSSOptimalSettings settings = new();
SLResult result = SL.DLSS.GetOptimalSettings(in options, ref settings);
```

Available features depend on the installed SDK plugins, device, driver and OS.
A feature ID alone does not establish runtime availability; use the SDK's support
and plugin-state queries. The wrapper does not select application fallbacks.

## Types and resource lifetime

- Use `new T()` for versioned SDK structures. It initializes native defaults and
  structure headers; `default(T)` and array allocation only zero their storage.
- `Bool8` represents native C++ `bool`; `SLBoolean` is the SDK's separate
  tri-state enum.
- `FrameToken` and `IAllocator` borrow native objects. Copying a wrapper does not
  transfer ownership. `SLArray<T>` owns its allocation through the original
  allocator and must be released with `Destroy` without copying ownership.
- Strings, spans and references are pinned or allocated for the duration of the
  call. Nested pointers, callbacks, resource descriptions and structure chains
  must remain valid for the lifetime required by the SDK.
- Native structure-chain nodes implement `ISLStructure`. Vulkan types retain
  their own native layout. The recorded ABI is Windows x64.
- SDK functions retain their native thread-safety requirements. The matrix
  recalculation helper shares history, matching upstream behavior. Managed
  exceptions must not escape an unmanaged callback.

Plugin function addresses are cached after a successful query and invalidated
when shutdown or plugin-load changes succeed. Pure data, string, preset, math and
structure-chain helpers do not load the native runtime. Windows signature helpers
perform the SDK's trust and NVIDIA identity checks.

## Showcase

The Windows x64 [Showcase](https://github.com/qian-o/Streamline.NET/blob/master/Showcase/README.md)
renders Sponza through DirectX 12 or Vulkan, with DLSS Super Resolution,
Ray Reconstruction and Frame Generation. Its rendering dependencies and downloaded
assets stay in the sample.

Run from the repository root in PowerShell:

```powershell
./Showcase/Assets/UpdateAssets.ps1
dotnet run --project Showcase -c Release
```

The sample README covers requirements, controls, rendering flow and publishing.

## Development

The solution contains the wrapper, generator and Showcase. Project settings and
package versions are centralized in `Directory.Build.props` and
`Directory.Packages.props`.

Interface extraction runs in GitHub Actions:

```sh
gh workflow run extract-streamline-api.yml --ref master
gh run list --workflow extract-streamline-api.yml
```

The workflow discovers the official release's public headers and extracts them
with the Windows toolchain. Review and import its `streamline-ast` artifact, then
regenerate and build locally:

```sh
dotnet run --project Streamline.NET.Generator -c Release
dotnet build Streamline.NET.slnx -c Release --warnaserror
```

Local generation reads the committed snapshot. Generated `.g.cs` files use UTF-8
BOM; implementation changes belong in the generator. Reviewed handwritten
translations are tracked by upstream body hashes in `ManualImplementations.json`.
`declaration-coverage.json` maps declarations and macros to implementations or
explicit exclusions. Unknown types, calling conventions, changed handwritten
bodies and unhandled declarations stop generation.

Release builds produce the managed NuGet package in `Streamline.NET/bin/Release/`.
It contains the assembly, XML documentation, README and license. NVIDIA headers,
runtime binaries and Showcase assets are supplied separately.

## License

MIT. See [LICENSE](https://github.com/qian-o/Streamline.NET/blob/master/LICENSE).
