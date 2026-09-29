# Streamline.NET

[![NuGet](https://img.shields.io/nuget/vpre/Streamline.NET)](https://www.nuget.org/packages/Streamline.NET)

C# bindings for [NVIDIA Streamline](https://github.com/NVIDIA-RTX/Streamline), with .NET 10 and Native AOT support.

## Usage

See the [native runtime setup notes](https://github.com/qian-o/Streamline.NET/blob/master/Streamline.NET/readme.txt) for downloads and initialization.

```csharp
using Streamline.NET;

// After SDK initialization and graphics-device setup:
DLSSOptions options = new()
{
    Mode = DLSSMode.MaxQuality,
    OutputWidth = 1920,
    OutputHeight = 1080
};

DLSSOptimalSettings settings = SL.DLSS.GetOptimalSettings(in options);
```

## Showcase

[DirectX 12 and Vulkan sample](https://github.com/qian-o/Streamline.NET/blob/master/Showcase/README.md) with DLSS Super Resolution, Ray Reconstruction and Frame Generation.

## License

[MIT](https://github.com/qian-o/Streamline.NET/blob/master/LICENSE). See [THIRD-PARTY-NOTICES](https://github.com/qian-o/Streamline.NET/blob/master/THIRD-PARTY-NOTICES) for third-party attributions.
