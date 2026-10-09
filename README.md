# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

C# bindings for NVIDIA NGX.

## API

Use `Ngx` for SDK calls. Public structures contain managed values; their native
representations and string conversion are internal to the assembly.
Text inputs reject embedded NUL and invalid Unicode; native parameter-key
constants retain their literal byte/terminator semantics.

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
    in featureInfo,
    NGXVersion.Api);

Ngx.ThrowIfFailed(result);
Ngx.ThrowIfFailed(Ngx.D3D12.GetCapabilityParameters(out NGXParameter parameters));
Ngx.ThrowIfFailed(Ngx.Parameter.GetI(
    parameters, Ngx.ParameterSuperSamplingAvailable, out int available));

// After all features and their GPU work have finished:
Ngx.ThrowIfFailed(Ngx.D3D12.DestroyParameters(parameters));
Ngx.ThrowIfFailed(Ngx.D3D12.Shutdown1(device));
```

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
- Enum members use PascalCase: `Custom`, `Dlaa`, `IsHdr`, `Api` and
  `FailFeatureNotSupported`. Their SDK values are unchanged.
- Pass strings, arrays and structures directly. Scalar outputs use `out`;
  structure inputs use `in`, with nullable overloads for optional inputs.
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
with `python3 verification/Marshalling/run.py`. It also verifies direct generator
output in a fresh directory, without formatting the generated files. This does
not run NVIDIA GPU work.

## Showcase

[Showcase](https://github.com/qian-o/NGX.NET/tree/master/Showcase) is a rendering sample built with NGX.NET.

## References

[Official NVIDIA SDK](https://github.com/NVIDIA/DLSS)

## License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
