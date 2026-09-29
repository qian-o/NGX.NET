# Streamline.NET

[![NuGet](https://img.shields.io/nuget/vpre/Streamline.NET)](https://www.nuget.org/packages/Streamline.NET)

C# bindings for [NVIDIA Streamline](https://github.com/NVIDIA-RTX/Streamline), with .NET 10 and Native AOT support.

## Usage

The NuGet package contains managed bindings only. To set up the native runtime:

1. For Windows x64, download **`streamline-sdk-<release-tag>.zip`** from the matching [NVIDIA Streamline release](https://github.com/NVIDIA-RTX/Streamline/releases). Choose the SDK ZIP without an `-aarch64` or `-arm64ec` suffix, rather than a Source code archive.
2. Extract the ZIP and copy all files directly inside **`bin/x64/`** to a directory of your choice. Preserve the accompanying licenses and configure your application to copy these files to its build and publish output. Use the production files, not the `development` subdirectory.
3. Set the absolute interposer path before any SDK call, then follow the [official integration guide](https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md) to initialize Streamline and associate your graphics device.

```csharp
using Streamline.NET;

SL.SetLibraryPath(@"C:\Path\To\sl.interposer.dll");

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
