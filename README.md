# NGX.NET

Low-level C# bindings for NVIDIA NGX, targeting .NET 10 with Native AOT support.

Includes the native bridge and official DLSS Super Resolution, Ray Reconstruction and Frame Generation runtimes.

| Platform | Architectures | Backends |
| --- | --- | --- |
| Windows | x64, ARM64 | Direct3D 11, Direct3D 12, Vulkan, CUDA |
| Linux | x64, ARM64 | Vulkan, CUDA |

Feature availability depends on the backend, NVIDIA GPU and driver.

[Showcase](Showcase) is a Windows x64 Sponza renderer with DirectX 12 and Vulkan backends.

## License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
