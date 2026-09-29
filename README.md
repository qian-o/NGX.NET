# Streamline.NET

[![NuGet](https://img.shields.io/nuget/vpre/Streamline.NET)](https://www.nuget.org/packages/Streamline.NET)

C# bindings for [NVIDIA Streamline](https://github.com/NVIDIA-RTX/Streamline), with .NET 10 and Native AOT support.
Explicit download helpers retrieve the native runtime from NVIDIA's official SDK releases.

## Usage

```csharp
using Streamline.NET;

string directory = @"C:\Path\To\Streamline";
await SL.DownloadRuntimeAsync(directory);
SL.SetLibraryPath(directory);

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

[MIT](https://github.com/qian-o/Streamline.NET/blob/master/LICENSE)
