# Streamline.NET Showcase

A Windows x64 / .NET 10 sample rendering Sponza with DirectX 12 or Vulkan and
DLSS Super Resolution, Ray Reconstruction and Frame Generation.

## Run

Install the .NET 10 SDK and a graphics driver supporting your selected features.
For DirectX 12, provide [DXC](https://github.com/microsoft/DirectXShaderCompiler/releases)
(`dxcompiler.dll` and `dxil.dll`) where Slang can load it, such as beside the
Showcase executable. Slang uses DXC to compile DXIL shaders.

Run from the repository root in PowerShell:

```powershell
dotnet run --project Showcase -c Release
```

Choose DirectX 12 or Vulkan in the startup console. Sponza is included in the
repository. The first launch downloads NVIDIA's official Streamline runtime to
`Assets/Streamline/` beside the executable; later launches reuse the verified cache.

Supported DLSS features are enabled by default; unavailable controls are disabled.
Frame Generation requires Windows Hardware-accelerated GPU scheduling.

## Controls

**Settings** shows the GPU and FPS, including generated frames. Drag its title bar
to move the panel or use the arrow to collapse it.

| Setting | Options |
|---|---|
| DLSS Super Resolution | Off, Quality, Balanced, Performance, Ultra Performance |
| DLSS Frame Generation | Off / On |
| DLSS Ray Reconstruction | Off / On |
| Pause Animation | Off / On |

Use W/A/S/D to move, Q/E to move vertically, and Shift to move faster. Hold the
right mouse button to look around. Movement keys are reserved for the UI while a
dropdown is open or text input is active.

Pause Animation freezes the gold and chromium spheres. The camera and settings
remain available; resuming continues from the paused position.

When supported, ray tracing stays active independently of Ray Reconstruction.
Turning RR off exposes noisy lighting. With SR off and RR on, reconstruction runs
at native resolution.

## Sources and licenses

- [NVIDIA Streamline](https://github.com/NVIDIA-RTX/Streamline): runtime licenses
  are included beside the binaries in `Assets/Streamline/` and under
  `Assets/Streamline/Licenses/`.
- [Khronos Sponza](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/Sponza):
  attribution and licenses are preserved under `Assets/Scenes/Attribution/`.
- Tone-mapping attribution: [LICENSE-ToneMapping.txt](Assets/Shaders/LICENSE-ToneMapping.txt).
