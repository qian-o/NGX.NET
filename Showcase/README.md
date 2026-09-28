# Streamline.NET Showcase

A Windows x64 / .NET 10 rendering sample using the public Streamline.NET API.
Select DirectX 12 or Vulkan in the startup console. Both backends share the scene,
material system, shaders, camera, UI and Streamline frame sequence. The sample
uses DLSS Super Resolution, Ray Reconstruction and Frame Generation.
Reflex/PCL support low latency and frame markers internally. NIS, DirectSR and
DeepDVC are not loaded, called or deployed; the wrapper still exposes their APIs.

## Run

From the repository root, in PowerShell:

```powershell
./Showcase/Assets/UpdateAssets.ps1
dotnet run --project Showcase -c Release
```

The script downloads the complete Khronos Sponza glTF asset and the latest official
Windows x64 Streamline production files needed by DLSS and its dependencies. It preserves attribution and licenses,
checks referenced scene files, and records the downloaded revision/version and SDK
archive hash. Downloads are ignored by Git. Rebuild after updating assets so the
runtime and scene are copied to the application output directory.

The .NET SDK and an up-to-date graphics driver are required. DLSS Frame Generation
also requires Windows Hardware-accelerated GPU scheduling; the sample uses SDK
capability queries to enable its controls. Unsupported controls are disabled.
The SDK reports diagnostics through the console and log files.

Default output is a resizable 1600 x 900 window. With device support, defaults are
DLSS Quality, Ray Reconstruction, Frame Generation (one generated frame per
rendered frame) and automatic exposure. Hardware ray tracing remains on whenever
the device supports it, regardless of the Ray Reconstruction setting. Only devices
without hardware ray queries use the raster fallback. Multi Frame Generation is not exposed.

## Controls

The English panel is titled **Settings** and behaves like a normal game-settings
window. It can be dragged and collapsed using ImGui's title bar. It cannot be
closed or manually resized; `AlwaysAutoResize` fits its contents, with only a
viewport-size limit for very small windows. Initial placement is near the upper
left, and subsequent frames preserve the user's position. System DPI scaling is
applied to the 16-pixel text and layout. Fonts are not redistributed.

It shows the GPU name, presentation **FPS** (including generated frames), and three
controls:

| Control | Choices / behavior |
|---|---|
| DLSS Super Resolution | Off, Quality, Balanced, Performance, Ultra Performance |
| DLSS Frame Generation | Off / On; manages Reflex and required swap-chain changes internally |
| DLSS Ray Reconstruction | Off / On for reconstruction/denoising only; ray tracing keeps running |

With upscaling off and RR on, Ray Reconstruction operates at native resolution
using the SDK's DLAA quality internally; DLAA is not listed as an upscaling option.
RR off retains ray-traced lighting and animated TLAS updates.
Noise is expected; there is no replacement denoiser. With both SR and RR off, the
native RT image goes directly through tone mapping without FXAA. With SR on,
that reconstruction still processes the noisy input. Only one reconstruction
pass executes. Brightness is metered automatically.

Camera navigation remains right mouse + WASD, Q/E for vertical motion, and Shift
for faster movement. Only supported, implemented DLSS features are exposed;
Dynamic Multi Frame Generation and 3D-Guided Neural Rendering are not added as
placeholder options.

## Rendering and integration

`RHI` coordinates settings, resource lifetime and the shared frame sequence.
`StreamlineSession` owns SDK options, frame tokens, resource tags and evaluation.
The DirectX 12 and Vulkan backends implement GPU commands and synchronization;
`RenderLayout` defines shared image formats, sizes and bindings.

The frame sequence is scene/camera update → depth and material passes → ray-traced
lighting → SR or RR → exposure and tone mapping → UI composition → presentation.

- Sponza retains its authored materials, hierarchy, normal maps and alpha masks.
  Two animated objects provide motion and reflection references. The metal sphere
  uses roughness 0.08. Shared shaders implement metallic/roughness shading, daylight,
  mip filtering and up to eight anisotropic taps.
- The depth prepass uses the material pass's alpha mask and sidedness. Early
  equal-depth tests avoid material shading on hidden surfaces. The color pass
  reverses triangle submission order without changing winding, preserving the
  first visible primitive where faces are coplanar.
- Hardware ray queries handle visibility, indirect lighting and reflections.
  Geometry is grouped by opacity and sidedness for efficient hardware traversal.
  One diffuse and one specular path per pixel run independently, using temporal
  strata, importance sampling and throughput-based Russian roulette. Each path
  allows up to eight surface events; an unlit first diffuse hit retains one more
  event before roulette. A shared two-layer scratch image holds the path results.
- Reconstruction receives untonemapped HDR, depth and camera/object motion. RR
  additionally receives diffuse/specular albedo, world normals, linear roughness
  and specular hit distance. Matrices are unjittered and row-major. Missed specular
  rays use FP16_MAX (65504), independently of the scene traversal distance.
- Exposure meters reconstructed HDR before UI composition. Global exposure targets
  18% gray; local exposure uses material reflectance and bilateral filtering to
  compress broad illumination while preserving texture detail and silhouettes.
  Temporal adaptation, 32-bit exposure history and the neutral AgX view transform
  preserve the accepted lighting and shadow appearance.
- The UI renders separately with premultiplied alpha at output resolution.
  Composition is `final.rgb = ui.rgb + (1 - ui.a) * hudless.rgb`. FG uses the
  interposer's presentation hooks and receives both HUD-less color and UI.
- Three frame slots protect pending GPU work. The same real-frame token is used
  for constants, tags, evaluation and Reflex/PCL markers. Generated frames do not
  advance simulation or camera history.

### Settings and resource lifetime

At a settings change, finish pending GPU work before touching SDK resources.
For a reconstruction change, set the outgoing SR/RR mode to `Off`, then call
`slFreeResources` for its evaluated viewport. Clear obsolete resource tags, configure
the selected mode and reset temporal history. This follows the
[SR guide](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS.md)
and [RR guide, sections 5 and 8](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS_RR.md).

SR/RR and quality changes reuse the device, scene, pipelines and swap chain.
Render images are replaced only when their dimensions change; descriptors update
when an image changes. Window/surface changes and FG toggles recreate the swap
chain, coordinating FG plugin loading while presentation is stopped. FG-only
changes retain SR/RR allocations. An SDK feature's first evaluation may still incur
initialization work, so toggles are not guaranteed to be stall-free.

Shutdown finishes GPU work, disables active features and releases their resources,
then calls `slShutdown` while the graphics device, swap chain and callbacks are
still alive. GPU objects and the window are destroyed afterward. SDK parameters
with nested pointers and tagged resource descriptions retain stable storage for
their required lifetimes.

### Presentation rate

The panel uses half-second FPS intervals. When FG is loaded, `slDLSSGGetState` is
called once after each successful Present, with null options to avoid an optional
VRAM estimate. Its actual presentation counts include generated and dropped frames;
the configured multiplier is never used to estimate FPS. Without FG, each successful
native Present contributes one frame. A failed state query displays `--`; an FG
failure is logged and disables FG through the regular swap-chain path. Configuration
changes, minimization and failed presentation reset the interval.

## Build and diagnostics

```powershell
dotnet build Streamline.NET.slnx -c Release --warnaserror
dotnet publish Showcase/Showcase.csproj -c Release -r win-x64 --self-contained false
```

Slangc.NET and DXC native libraries are restored through NuGet. Slang uses default
downstream-library discovery; no DXC path is overridden. Shader bytecode is cached
by entry point, backend and ray-query variant during initialization.

SDK diagnostics and managed failure reports are written to `Logs/` under the
output directory. The application has no GPU timing queries, timing readbacks,
performance capture files or separate latency statistics. Reflex/PCL markers remain
part of the functional low-latency/FG integration. Report backend, GPU, driver,
feature settings and relevant logs/screenshots when reporting a runtime issue.

Build and host-side lifecycle checks do not validate NVIDIA's Windows runtime.
Visual quality, repeated SR/RR/FG switching, resize/minimize and native SDK warnings
must be checked on the target GPU. In particular, the previously reported
`WorldToScreenMatrix not invertible` warning must be checked again after restoring
the official switching sequence.

## Dependencies

Versions are centralized in `Directory.Packages.props`. Showcase references the
current Streamline.NET project without adding its rendering dependencies or runtime
assets to the wrapper package.

| Dependency | Purpose |
|---|---|
| Vortice.Direct3D12 / Vortice.DXGI | DirectX 12 backend |
| Vortice.Vulkan | Vulkan backend |
| Slangc.NET / Vortice.Dxc.Native | Shared shader compilation and DXIL runtime |
| SharpGLTF.Core | glTF loading |
| SixLabors.ImageSharp | Image decoding and mip generation |
| ImGui.NET | Settings and font atlas |

Scene and SDK attribution/licenses are preserved by the asset script. AgX attribution
is in `Assets/Shaders/LICENSE-AgX.txt`.

## Rendering references

- [NVIDIA RTX ray-tracing best practices](https://developer.nvidia.com/blog/best-practices-using-nvidia-rtx-ray-tracing/)
  for opaque geometry and avoiding unnecessary candidate-shader work.
- [Vulkan ray traversal](https://docs.vulkan.org/spec/latest/chapters/raytraversal.html)
  and [DXR functional specification](https://microsoft.github.io/DirectX-Specs/d3d/Raytracing.html)
  for consistent hardware facing, opacity and candidate handling.
- [Filament's material and lighting model](https://google.github.io/filament/main/filament.html)
  for GGX/Smith reflectance and indirect-light occlusion.
- [Heitz, Sampling the GGX Distribution of Visible Normals, JCGT 7(4), 2018](https://jcgt.org/published/0007/04/01/)
  for GGX visible-normal sampling.
- [PBRT, A Better Path Tracer](https://pbr-book.org/4ed/Light_Transport_I_Surface_Reflection/A_Better_Path_Tracer)
  for path termination, Russian roulette and multiple importance sampling.
- [PBRT, Mapping Path Tracing to the GPU](https://www.pbr-book.org/4ed/Wavefront_Rendering_on_GPUs/Mapping_Path_Tracing_to_the_GPU)
  for separating path workloads and the register/divergence versus bandwidth tradeoff.
- [PBRT, Infinite Area Lights](https://pbr-book.org/4ed/Light_Sources/Infinite_Area_Lights)
  for portal-guided environment sampling with visibility.
- [Durand and Dorsey, Fast Bilateral Filtering for the Display of High-Dynamic-Range Images](https://people.csail.mit.edu/fredo/PUBLI/Siggraph2002/)
  for edge-preserving base/detail tone reproduction.
- [three.js AgX implementation](https://github.com/mrdoob/three.js/blob/9b02bfe8671c4dd8c9327c1636edc529b6462812/src/renderers/shaders/ShaderChunk/tonemapping_pars_fragment.glsl.js)
  for the neutral AgX view transform, adapted from Filament/Blender.
- [NVIDIA DLSS Frame Generation guide, frame-time measurement](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS_G.md#130-how-to-obtain-the-actual-frame-times-and-number-of-frames-presented)
  for the presentation counter and its per-query lifetime.
- [NVIDIA DLSS programming guide](https://github.com/NVIDIA/DLSS/blob/main/doc/DLSS_Programming_Guide_Release.pdf)
  section 3.5 for the reconstruction mip bias and its aliasing/detail tradeoff.
- [NVIDIA DLSS-RR integration guide](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/DLSS-RR%20Integration%20Guide.pdf)
  sections 3.4.3 and 3.4.9 for world-space normals and sky hit-distance values.
