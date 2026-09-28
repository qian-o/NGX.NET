# Streamline.NET Showcase

A Windows x64 / .NET 10 sample that renders Sponza with DirectX 12 or Vulkan and
integrates DLSS Super Resolution, Ray Reconstruction and Frame Generation through
Streamline.NET. Both backends share the scene, shaders, camera and Settings panel.

## Run

Install the .NET 10 SDK and a graphics driver supporting your selected features.
From the repository root, run in PowerShell:

```powershell
./Showcase/Assets/UpdateAssets.ps1
dotnet run --project Showcase -c Release
```

Choose DirectX 12 or Vulkan in the startup console. The asset script downloads
Khronos Sponza and NVIDIA's official Windows x64 Streamline production runtime,
checks scene references and the SDK archive digest, and preserves attribution and
licenses. Downloaded files are ignored by Git. Rebuild after updating assets.

The sample starts in a resizable 1600 × 900 window. Supported devices default to
DLSS Quality, Ray Reconstruction and Frame Generation with one generated frame per
rendered frame. FG requires Windows Hardware-accelerated GPU scheduling. SDK
capability checks disable unsupported controls.

## Controls

The English **Settings** panel shows the GPU and presentation FPS, including
SDK-reported generated frames. Drag its title bar to move it or use the arrow to
collapse it. The panel sizes itself to its contents and follows system DPI scaling.

| Setting | Options |
|---|---|
| DLSS Super Resolution | Off, Quality, Balanced, Performance, Ultra Performance |
| DLSS Frame Generation | Off / On |
| DLSS Ray Reconstruction | Off / On |

Hardware ray tracing stays active when supported. Turning RR off exposes noisy
ray-traced lighting; no substitute denoiser is used. Turning SR off while keeping
RR on runs reconstruction at native resolution using the SDK's DLAA mode internally.
With both off, native ray-traced color goes directly to display processing.
Devices without hardware ray queries use raster lighting with shadow mapping.
Exposure adjusts automatically.

Hold the right mouse button to look around. Use W/A/S/D to move, Q/E to move
vertically, and Shift to move faster.

## Code organization

| Files | Responsibility |
|---|---|
| `Program.cs`, `Window.cs`, `UserInterface.cs` | Startup, Win32 input/window lifetime and ImGui |
| `RHI.cs` | Shared frame sequence, settings changes and render-target lifetime |
| `RenderTypes.cs` | Graphics passes, shader entry points, buffer layout and image formats/sizes |
| `StreamlineSession.cs` | SDK initialization, feature options, frame tokens, tagging and evaluation |
| `Scene.cs`, `Camera.cs` | glTF conversion, materials, animation, camera and motion history |
| `FrameStatistics.cs` | Presentation FPS from actual SDK counts |
| `DirectX12/`, `Vulkan/` | Device/resources/swap chain, renderer commands and ray-query acceleration structures |
| `Assets/Shaders/` | Shared material, path-tracing, environment, exposure and tone-mapping shaders |

Each frame updates scene/camera history, renders depth and materials, computes
lighting, runs SR or RR, applies exposure/tone mapping, composites the UI and presents.

- Sponza's hierarchy is baked into static geometry with its authored materials,
  normal maps and alpha masks. Moving metal and ceramic spheres provide reflection
  and motion references.
- A matching depth prepass and early equal-depth tests reduce hidden material
  shading. Geometry is grouped by opacity and sidedness for hardware ray queries.
- Lighting rays test both sides of opaque surfaces while preserving alpha cutouts.
  Separate geometric normals keep reflected paths above the actual surface and
  offset ray origins independently of normal-map detail.
- Diffuse and specular paths run independently with importance sampling, temporal
  strata and throughput-based Russian roulette. Filtering uses mipmaps and
  anisotropic texture footprints.
- Reconstruction receives HDR color, depth and camera/object motion. RR also
  receives diffuse/specular albedo, world normals, roughness and specular hit distance.
- Global/local exposure and the AgX view transform run after reconstruction. The UI
  is rendered separately with premultiplied alpha at output resolution; FG receives
  HUD-less color and UI resources separately.

## Feature and resource lifetime

Three frame slots protect pending GPU work. Constants, tags, reconstruction and
Reflex/PCL markers use the same real-frame token. Generated frames do not advance
simulation or history.

Settings changes finish pending GPU work before releasing resources. An outgoing
SR/RR feature is set to `Off`, then its evaluated viewport is released with
`slFreeResources`. Tags and temporal history reset for the selected configuration.
The device, scene and pipelines remain alive; images are replaced only when their
size changes. Window/surface changes and FG toggles coordinate plugin loading with
swap-chain recreation. FG-only changes retain SR/RR allocations.

Shutdown releases feature resources and calls `slShutdown` while graphics objects
and callbacks remain valid, then destroys GPU resources and the window.

FPS uses actual presentation counts over half-second intervals. With FG loaded,
`slDLSSGGetState` is queried once after each successful Present. Unknown counts show
`--`; SDK-reported FG failures disable FG through the normal settings path.

## Build and publish

```powershell
dotnet build Streamline.NET.slnx -c Release --warnaserror
dotnet publish Showcase/Showcase.csproj -c Release -r win-x64 --self-contained false
```

Run `Showcase.exe` from `Showcase/bin/Release/net10.0/win-x64/publish/`. This publish
requires the .NET 10 runtime on the target machine. Scene files, shaders and the
prepared NVIDIA runtime are copied with the application.

NuGet supplies Vortice, Slangc.NET/DXC, SharpGLTF, ImageSharp and ImGui.NET; versions
are listed in `Directory.Packages.props`. Slang uses default downstream-library
discovery. Shader bytecode is cached by entry point, backend and ray-query variant.
System fonts are loaded locally and are not redistributed.

SDK diagnostics and managed failure reports go to `Logs/` beside the executable.
Include the backend, GPU, driver, feature settings and relevant logs when reporting
an issue. Rendering and native SDK behavior require Windows GPU validation.

## Sources and licenses

- [Streamline integration](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuide.md),
  [Super Resolution](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS.md),
  [Ray Reconstruction](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS_RR.md)
  and [Frame Generation](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS_G.md).
- [Khronos Sponza](https://github.com/KhronosGroup/glTF-Sample-Assets/tree/main/Models/Sponza):
  source attribution and licenses are preserved under `Assets/Scenes/Attribution/`.
- NVIDIA runtime licenses are copied beside the deployed binaries and under `Licenses/`.
- AgX attribution is preserved in [LICENSE-AgX.txt](Assets/Shaders/LICENSE-AgX.txt).
