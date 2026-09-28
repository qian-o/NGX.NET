# Streamline.NET

C# bindings for [NVIDIA Streamline](https://github.com/NVIDIA-RTX/Streamline), covering DLSS Super Resolution, Ray Reconstruction, Frame Generation, Reflex and the other public SDK APIs. The bindings follow official Streamline releases.

- Feature groups such as `SL.DLSS`, `SL.DLSSD`, `SL.DLSSG` and `SL.Reflex`.
- Native pointer APIs plus reference, string, Span and direct-return overloads.
- .NET 10, Native AOT and trimming support, with no additional managed runtime dependencies.

## Usage

Your application supplies the NVIDIA runtime and plugins. Follow the [official integration guide](https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md) for SDK initialization and graphics-device setup.

```csharp
using Streamline.NET;

SL.SetLibraryPath(@"C:\MyApplication\Streamline\sl.interposer.dll");

// After SDK initialization and graphics-device setup:
DLSSOptions options = new()
{
    Mode = DLSSMode.MaxQuality,
    OutputWidth = 1920,
    OutputHeight = 1080
};

DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
```

Direct-return overloads throw `SLException` on non-success results. Pointer and `ref` overloads return `SLResult` for explicit error handling. Use `new()` to initialize SDK structures, including their type and version headers.

## Showcase

[Showcase](Showcase/README.md) renders Sponza with DirectX 12 or Vulkan and demonstrates DLSS Super Resolution, Ray Reconstruction and Frame Generation. See its README for setup and controls.

## License

[MIT](LICENSE)
