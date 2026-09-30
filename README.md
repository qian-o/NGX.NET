# NGX.NET

[![NuGet Version](https://img.shields.io/nuget/vpre/NGX.NET)](https://www.nuget.org/packages/NGX.NET)

NGX.NET provides C# bindings for NVIDIA NGX, bringing DLSS to .NET games and rendering applications.

- **DLSS Super Resolution** reconstructs high-resolution images from lower-resolution rendering.
- **DLSS Ray Reconstruction** reconstructs ray-traced lighting and reflections with AI denoising.
- **DLSS Frame Generation** generates intermediate frames to increase the displayed frame rate.

# Showcase

The included [Showcase](Showcase) renders Sponza with Silk.NET using DirectX 12 or Vulkan. Compare native rendering with DLSS, switch quality modes, and toggle Ray Reconstruction and Frame Generation.

See [NGXSession.cs](Showcase/NGXSession.cs) for SDK initialization, capability queries and DLSS feature management.

# Reference

[NVIDIA/DLSS](https://github.com/NVIDIA/DLSS)

[NVIDIA DLSS](https://developer.nvidia.com/rtx/dlss)

# License

The C# bindings and bridge source are [MIT licensed](LICENSE). Bundled NVIDIA SDK code and runtime libraries retain their NVIDIA terms.
