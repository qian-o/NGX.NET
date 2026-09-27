# Streamline.NET Showcase

A Windows x64 / .NET 10 rendering sample using the public Streamline.NET API.
Select DirectX 12 or Vulkan in the startup console. Both backends share the scene,
material system, shaders, camera, UI and Streamline frame sequence. The sample
uses DLSS Super Resolution / DLAA, Ray Reconstruction and Frame Generation.
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
DLSS Quality, ray tracing with Ray Reconstruction, Frame Generation (one generated
frame per rendered frame) and automatic exposure. Unsupported features fall back
to native raster rendering. Multi Frame Generation is not exposed.

## Controls

The panel is anchored to the upper-left corner with 16-pixel text and a fixed
280-pixel logical width. It cannot be dragged, resized or collapsed. Its content
height is constrained by the viewport, with scrolling only in very short windows.
System DPI scaling is applied once. The interface is always English, independent
of the Windows display language. Fonts are not redistributed.

The title shows the DLSS implementation version queried via
`slGetFeatureVersion(...).versionNGX`, followed by the GPU name and one FPS line:
`FPS` is the presentation rate including generated frames; `Render` is the
application-rendered frame rate.
A missing version is shown as `--`; the Streamline/interposer version is not used
as a substitute. The panel contains only three controls:

| Control | Choices / behavior |
|---|---|
| DLSS Super Resolution | Off, Deep Learning Anti-Aliasing (DLAA), Quality, Balanced, Performance, Ultra Performance |
| DLSS Frame Generation | Off / On; manages Reflex and required swap-chain changes internally |
| DLSS Ray Reconstruction | Off / On; automatically enables Ray Reconstruction for denoising |

With upscaling off and ray tracing on, Ray Reconstruction operates at native
resolution using DLAA quality. Only one reconstruction pass executes. Brightness
is metered automatically. There are no brightness controls, reset buttons,
advanced/details sections, status lists, tooltips or operation hints in the panel.

The close button or F1 hides the panel; the small Show button or F1 restores it.
Camera navigation remains right mouse + WASD, Q/E for vertical motion, and Shift
for faster movement. Only supported, implemented DLSS features are exposed;
Dynamic Multi Frame Generation and 3D-Guided Neural Rendering are not added as
placeholder options.

## Rendering and integration

- Native resolution uses FXAA. DLSS SR and DLAA receive untonemapped HDR color,
  depth and camera/object motion. RR receives the same scene with real noisy
  illumination, diffuse/specular albedo, world normals, linear roughness and
  specular hit distance. RR performs the reconstruction directly.
- The scene uses metallic/roughness materials, normal maps, alpha masking, a sky,
  a shadowed daylight sun and two moving objects with muted metal/ceramic materials.
  Daylight and exposure metering are automatic. glTF hierarchy
  transforms are evaluated on load; downloaded materials and textures are unchanged.
- Automatic exposure meters HDR color after reconstruction, before tone mapping
  and UI composition. A stratified 128 x 128 sampling grid averages luminance in
  linear light before conversion to exposure stops. Averaging individual sample
  logs would overexpose the scene when many raw ray samples are zero.
  The meter targets 18% gray, limits adaptation to +/-8 stops and follows the
  preceding submitted frame, with one-second brightening and quarter-second
  darkening half-lives. Reset/resize initializes from the current measurement.
  Lighting and Streamline's unexposed HDR inputs are not scaled by the meter.
  AgX's default view transform replaces the per-channel ACES approximation,
  with its published color-space matrices and highlight response. No saturation
  boost or additional creative look is applied. The implementation and MIT notice
  are in `Assets/Shaders/ToneMapping.slang` and `LICENSE-AgX.txt`.
- Hardware ray tracing uses a shared Slang `RayQuery` implementation: DXR 1.1
  `TraceRayInline` on DirectX 12 and `VK_KHR_ray_query` on Vulkan. It traces shadows
  and one stochastic diffuse and one GGX specular secondary ray per pixel. The
  secondary path evaluates up to two surface interactions with direct light and
  actual visibility, including emission or visible sky reached by the final
  scattered ray. There is no fixed secondary ambient fill. This remains a limited
  bounce renderer with an
  analytic daylight environment; performance needs measurement on the target GPU.
- Direct shading, GGX visible-normal sampling and the RR specular guide use the same
  height-correlated Smith model. This improves grazing-angle behavior without
  clamping away real lighting. The raster path adds screen-space contact occlusion
  to indirect light only; the traced path uses geometric visibility instead.
- Each rigid object has one non-indexed BLAS built at startup. Sponza's geometry is
  static; the moving objects update TLAS instance transforms. Each frame slot owns
  its TLAS, scratch buffer and instance upload allocation. A slot is first built,
  then updated in place after its GPU fence completes. Resize and reconstruction
  changes reuse the geometry acceleration structures.
- Ray-query candidates apply the material's alpha cutoff and one-/two-sided rules
  before committing hits. The instance ID and primitive index locate the original
  shared vertices, UVs and materials. There is no software BVH rendering path.
- With ray tracing off, lighting uses a raster sun shadow map. When ray-query
  capabilities are unavailable, the renderer compiles its raster lighting variant and disables ray
  tracing and RR while keeping the other supported rendering/features available.
  Vulkan requests its ray-query, acceleration-structure and buffer-device-address
  features before device creation, independently of the interposer's SDK needs.
- Textures retain their source dimensions and receive a full mip chain. Base color
  and emissive maps are decoded to linear color during sampling. Normals and
  metallic/roughness maps are sampled as data. Each channel uses its own texture
  dimensions for mip selection, with trilinear filtering between mip levels.
  Degenerate authored tangents use the same orthogonal fallback as missing tangents,
  avoiding zero-vector normalization in normal mapping and indirect paths.
- The UI renders separately with premultiplied alpha at output resolution. HUD-less
  color and UI obey `final.rgb = ui.rgb + (1 - ui.a) * hudless.rgb`.
- Frame generation uses the interposer's swap chain and presentation hooks. Its
  plugin is loaded/unloaded with swap-chain reconstruction. The same real-frame
  token is used for constants, tags, evaluation and latency markers. Generated
  frames do not advance simulation or camera history.
- Three frame slots protect pending GPU work. Resize/mode changes wait for relevant
  work, release SDK resources according to the feature contract, recreate targets
  and reset history. Normal frame rendering waits only when reusing a frame slot.
- All SDK parameters with nested pointers and tagged resource descriptions retain
  storage through their required lifetime. SDK shutdown runs while graphics objects
  and callbacks remain alive.

Frame statistics use half-second windows. With DLSS Frame Generation loaded,
`slDLSSGGetState` is called once after each successful Present on the presenting
thread, with null options to avoid a VRAM-estimation request. Its
`numFramesActuallyPresented` values are summed over the measured interval. This
includes generated frames and accounts for skipped/zero-count samples; the
configured multiplier is never used to manufacture a display rate. Without FG,
each successful native Present contributes one frame.

An unavailable state query makes the presentation rate unknown (`--`) for that
window. SDK-reported FG state/query failures are logged and turn FG off through the normal
swap-chain recreation path. Resize, mode switches, minimization and failed
presents reset the counters; initialization/rebuild stalls are not mixed into the
next window. Render FPS can fall when FG adds GPU work even while total FPS rises;
Windows testing must compare the presentation rate, not just Render FPS.

## Build and diagnostics

```powershell
dotnet build Streamline.NET.slnx -c Release --warnaserror
```

Slangc.NET and DXC native libraries are restored through NuGet. The application
uses the compiler's default downstream-library discovery without overriding its
DXC path. Shaders compile as part of renderer initialization.

The SDK writes its diagnostics to `Logs/` under the output directory. Startup and
feature availability are printed to the console. Managed failures also write a
`showcase-*.log` file there. For Windows acceptance, record the selected backend,
GPU and driver, SDK version, input/output sizes and feature settings, together with
logs and screenshots of any issue.

## Dependencies

Versions are centralized in `Directory.Packages.props`. The sample uses a project
reference to the current Streamline.NET source and does not add dependencies or
runtime files to the wrapper package.

| Dependency | Version | Purpose |
|---|---|---|
| Vortice.Direct3D12 / Vortice.DXGI | 3.8.3 | DirectX 12 backend |
| Vortice.Vulkan | 3.2.3 | Vulkan backend |
| Slangc.NET | 2026.18.0 | Shared shader compiler |
| Vortice.Dxc.Native | 1.0.5 | DXIL downstream compiler runtime |
| SharpGLTF.Core | 1.0.7 | glTF loading |
| SixLabors.ImageSharp | 3.1.12 | Image decoding and mip generation |
| ImGui.NET | 1.91.6.1 | Control panel and font atlas |

## Verification record

Resource preparation was checked on 2026-09-27. The English UI and frame-statistics
update was checked on 2026-09-28; the earlier DLSS-only and AgX checks remain below.

Development host: macOS arm64, .NET SDK 10.0.401.

- Solution Release build with warnings as errors: passed. Windows x64
  framework-dependent publishing also passed; native dependencies were inspected.
- PowerShell asset script: executed successfully using PowerShell 7.6.0; retrieved
  Streamline v2.14.1 production files and Sponza revision
  `7d4ba189827916452eeadc82d4b712dbc6280a6f`. The new runtime filter was checked
  separately against production/development and unrelated plugin entries; the
  network download was not repeated for this change.
- Deployment inspection: interposer/plugins are at the output root; Sponza and its
  referenced data remain under `Assets/Scenes/`; all downloads remain ignored.
  A fresh Windows publish contains the 11 DLSS/Reflex dependency DLLs and excludes
  DeepDVC, NIS, DirectSR and nvperf binaries, even with older cached SDK files present.
- Scene preparation: passed, 264,187 triangles including the moving objects,
  28 material records and 69 decoded texture resources with mip chains.
- All twelve SPIR-V shaders and HLSL translations, plus the raster lighting
  variant: compiled successfully. The hardware lighting binary contains SPIR-V
  ray-query instructions and its acceleration-structure binding; the raster variant
  contains neither.
- CPU checks for this revision: all 264,187 triangles map into three complete,
  non-overlapping BLAS ranges with correct material/object references. DXR and
  Vulkan emit identical 64-byte instance records, including 24-bit IDs, visibility
  masks, BLAS addresses and translated positions. Moving/paused instances preserve
  raster history. The raster shadow projection encloses the scene bounds.
- SPIR-V member offsets and strides match the 48-byte object records and 464-byte
  frame constants. The earlier software BVH coverage check is superseded by the
  hardware geometry/instance checks above.
- The user reported that both Windows backends ran successfully, then reported a
  near-black recommended ray-traced view. An isolated CPU reference using the actual
  Sponza geometry/materials reproduced underexposure without DLSS. Increasing the
  path limit alone did not resolve it. Selected atrium-floor probes reached the sky
  in only 3-7% of cosine-weighted directions. The reference also identified parallel
  normal/tangent pairs that produced non-finite indirect samples before the fix.
- The actual metering and tone-map shaders were translated to Metal and executed
  in an isolated Apple M4 harness. Known HDR luminances, manual bypass, reconstructed
  input selection, black/bright bounds, reset with invalid history, zero elapsed
  time, equal exposure for sparse/uniform samples of equal mean luminance and
  equivalent adaptation at 30/120 FPS passed. The corrected CPU scene reference
  produced finite HDR samples, which were also run through the shared GPU meter
  and tone mapper for a fixed/automatic-exposure comparison. This checks the shared shader
  math, not Windows Ray Query, DLSS RR or frame-generation execution. The corrected
  Windows appearance and GPU performance still need acceptance.
- Current UI checks use offscreen ImGui draw data with fixture values, not measured
  GPU results: English text under English/Chinese UI cultures, a 360 x 160 window with
  scrolling, 200% DPI and F1 hide/restore passed. Title/corner drag interactions
  preserved the panel's position and size. Capability defaults and the 12
  DLSS mode / ray-tracing combinations select one reconstruction path, including
  native-resolution RR with upscaling off. The native DLSS version query itself
  requires Windows acceptance; UI fixtures do not claim a measured runtime version.
- Frame-statistics tests passed for SDK-supplied presentation counts, generated
  frame drops, zero samples, unavailable queries, recovery, irregular intervals,
  and counter resets after resize/toggle/pause. The Windows native SDK counter and
  actual FG performance require target-machine validation; no measured Windows
  FPS increase is claimed.
- The shared AgX/tone-map shaders ran on Apple M4 in the isolated harness. Across
  32 color/exposure cases, 8-bit GPU results matched an independent evaluation of
  the published reference. The same HDR Sponza reference was compared before/after
  tone mapping with identical exposure. Windows DLSS/RR visual acceptance remains
  with the user. Validation helpers are outside the application and are not committed.

## Rendering references

- [Filament's material and lighting model](https://google.github.io/filament/main/filament.html)
  for GGX/Smith reflectance and indirect-light occlusion.
- [Heitz, Sampling the GGX Distribution of Visible Normals, JCGT 7(4), 2018](https://jcgt.org/published/0007/04/01/)
  for GGX visible-normal sampling.
- [PBRT, A Better Path Tracer](https://pbr-book.org/4ed/Light_Transport_I_Surface_Reflection/A_Better_Path_Tracer)
  for resolving environment/emission before terminating the scattering path.

- [three.js AgX implementation](https://github.com/mrdoob/three.js/blob/9b02bfe8671c4dd8c9327c1636edc529b6462812/src/renderers/shaders/ShaderChunk/tonemapping_pars_fragment.glsl.js)
  for the neutral AgX view transform, adapted from Filament/Blender.
- [NVIDIA DLSS Frame Generation guide, frame-time measurement](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/docs/ProgrammingGuideDLSS_G.md#130-how-to-obtain-the-actual-frame-times-and-number-of-frames-presented)
  for the presentation counter and its per-query lifetime.
