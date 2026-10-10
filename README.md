# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

C# bindings for NVIDIA NGX.

Native SDK calls support Windows and Linux on x64 and arm64. Assembly loading and
managed conversion remain available on other platforms; unsupported platforms
are rejected when the native library is resolved.

## API

Use `Ngx` for SDK calls. Public structures contain managed values; their native
representations and string conversion are internal to the assembly.
Text inputs reject embedded NUL and invalid Unicode; native parameter-key
constants retain their literal byte/terminator semantics.
Check results directly against `NGXResult.Success`. Only that value is treated
as success; the caller decides how to handle any other result.

```csharp
using NGX.NET;

NGXFeatureCommonInfo featureInfo = new()
{
    PathListInfo = new()
    {
        Paths = [Ngx.RuntimeDirectory]
    }
};

NGXResult result = Ngx.D3D12.InitWithProjectID(
    projectId,
    NGXEngineType.Custom,
    engineVersion,
    applicationDataPath,
    device,
    featureInfo,
    NGXVersion.Api);

if (result is not NGXResult.Success)
{
    throw new NGXException(result, "Ngx.D3D12.InitWithProjectID");
}

NGXParameter parameters = Ngx.D3D12.GetCapabilityParameters();
int available = Ngx.Parameter.GetI(parameters, Ngx.ParameterSuperSamplingAvailable);

// After all features and their GPU work have finished:
result = Ngx.D3D12.DestroyParameters(parameters);
if (result is not NGXResult.Success)
{
    throw new NGXException(result, "Ngx.D3D12.DestroyParameters");
}

result = Ngx.D3D12.Shutdown1(device);
if (result is not NGXResult.Success)
{
    throw new NGXException(result, "Ngx.D3D12.Shutdown1");
}
```

Methods returning `NGXResult` with output parameters also have an overload that
returns those outputs and throws `NGXException` on failure. One output is returned
directly; multiple outputs use a generated readonly struct with get-only properties:

```csharp
Extensions extensions = Ngx.Vulkan.RequiredExtensions();
string[] instanceExtensions = extensions.InstanceExtensions;
string[] deviceExtensions = extensions.DeviceExtensions;

OptimalSettings optimalSettings = Ngx.DLSS.GetOptimalSettings(
    parameters, width, height, quality);
uint renderWidth = optimalSettings.RenderOptimalWidth;
uint renderHeight = optimalSettings.RenderOptimalHeight;
```

The original `NGXResult`/`out` overloads remain available for expected failures,
such as capability probes. Result overloads call those same managed methods,
preserving conversion, cleanup and ownership behavior. `NGXException.Result`
retains the SDK result, and its message identifies the operation. Methods without
outputs continue returning `NGXResult`.

Result types are derived from the managed output signature, after native array
counts have been folded into arrays. Their names remove a leading `Get`, `Query`,
`Enumerate`, `Create`, `Allocate`, `Estimate`, `Calculate` or `Required` at a
PascalCase word boundary; other method names receive a `Result` suffix. Numeric
version suffixes are preserved (`GetStats1` produces `Stats1`). Property names
remove pointer prefixes (`p`/`pp`) and `Out` prefixes, use the existing PascalCase
rules, and expand the `Exts` word to `Extensions`. Property order and types follow
the original outputs. These rules do not contain a list of result type names.
Identical type names share one definition only when all property names, types and
their order match, as with DLSS/DLSSD `OptimalSettings`. Incompatible result names,
duplicate properties and overload signature conflicts stop generation with a
diagnostic. Result structs do not make returned arrays immutable.

`NGXParameter` and `NGXHandle` are readonly structs with a readonly `Value` field.
Construct them with `new NGXParameter(address)` or `new NGXHandle(address)`; their
equality, hashing and deconstruction compare or expose that address. Both are
borrowed handle values. Release owned parameter maps and features explicitly
through the matching backend. The legacy
`GetParameters` result belongs to the SDK and must not be passed to
`DestroyParameters`. Serialize SDK calls and keep devices, command buffers, views
and GPU resources alive for their actual use; conversion does not take ownership
of those resources.

Initialization strings, paths and logging callbacks are retained internally until
successful shutdown. Evaluate helpers retain converted pointer inputs with their
parameter map. Successful replacement, `Parameter.Reset`, `DestroyParameters`
or backend shutdown releases those inputs. A failed helper may have partially
modified the map, so its storage is retained until replacement or cleanup.
Omitting DLSSG optional settings preserves the previous matrix storage, matching
the SDK helper's behavior. `GetVoidPointer` returns a borrowed address; do not keep
it across parameter mutation or cleanup.

Logging and progress delegates passed to `Ngx` have their exceptions contained
and reported through `Trace`. Logging handlers must support concurrent invocation.
Callbacks registered manually as raw addresses require the caller to retain the
delegate and contain exceptions for the entire native registration lifetime.

## Migrating from the pointer API

- Replace `NGX.NET.NGX` and its aliases with `Ngx`.
- Compare results directly with `NGXResult.Success` and handle failures at the
  call site. Use `.Length` for array counts.
- Enum members use PascalCase: `Custom`, `Dlaa`, `IsHdr`, `Api` and
  `FailFeatureNotSupported`. Their SDK values are unchanged.
- Pass strings, arrays and structures directly. Outputs can use return values or `out`;
  structure inputs use `in`, with nullable overloads for optional inputs.
  Call sites can omit the `in` argument modifier.
- Use `NGXParameter` and `NGXHandle` values and `IsNull` instead of pointers.
- String arrays provide their own counts. Vulkan extension queries return
  managed arrays; fixed native text buffers become strings.
- Optional struct pointers become nullable values. Application/resource unions
  use nullable members, with the enclosing discriminator selecting the member.
- Callback pointer wrappers are now delegates. Explicit external addresses such
  as devices, GPU resources and Vulkan procedure addresses remain `nint`.

The constructor-based conversion allocates native storage for nested pointer
inputs. See `verification/Marshalling/results.json` for the measured managed
allocation cost and validation limits. Run the managed, layout and export checks
with `python3 verification/Marshalling/run.py`. It also executes result overloads
against managed stubs and verifies direct generator output in a fresh directory,
without formatting the generated files. This does not run NVIDIA GPU work.

## Showcase

[Showcase](https://github.com/qian-o/NGX.NET/tree/master/Showcase) is a rendering sample built with NGX.NET.

## References

[Official NVIDIA SDK](https://github.com/NVIDIA/DLSS)

## License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
