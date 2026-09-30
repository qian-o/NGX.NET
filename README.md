# NGX.NET

Low-level C# bindings for NVIDIA NGX, targeting .NET 10 with Native AOT support.

Includes the native bridge and official DLSS Super Resolution, Ray Reconstruction and Frame Generation runtimes.

Supports Windows and Linux on x64 and ARM64. Bindings cover Direct3D 11/12 on Windows, and Vulkan and CUDA on both platforms.

Feature availability depends on the backend, NVIDIA GPU and driver.

[Showcase](Showcase) demonstrates DLSS integration in a Silk.NET Sponza renderer with DirectX 12 and Vulkan backends.

## License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
