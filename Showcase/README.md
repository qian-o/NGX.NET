# Streamline.NET Showcase

A Windows x64 / .NET 10 rendering sample using the public Streamline.NET API.
Select DirectX 12 or Vulkan in the startup console. Both backends share the scene,
material system, shaders, camera, UI and Streamline frame sequence.

## Run

From the repository root, in PowerShell:

```powershell
./Showcase/Assets/UpdateAssets.ps1
dotnet run --project Showcase -c Release
```

The script downloads the complete Khronos Sponza glTF asset and the latest official
Windows x64 Streamline production runtime. It preserves attribution and licenses,
checks referenced scene files, and records the downloaded revision/version and SDK
archive hash. Downloads are ignored by Git. Rebuild after updating assets so the
runtime and scene are copied to the application output directory.

The .NET SDK and an up-to-date graphics driver are required. DLSS Frame Generation
also requires Windows Hardware-accelerated GPU scheduling; the sample uses SDK
capability queries to enable its controls. Unsupported options show the SDK reason
in the feature availability panel. DirectSR is available only when the DirectX 12
runtime and installed plugin support it.

Default output is a resizable 1600 x 900 window. The **Recommended** preset uses
ray-traced lighting, DLSS Ray Reconstruction in Quality mode, 2x Frame Generation
and Reflex Low Latency when the current device supports them. Unsupported features
are omitted automatically. No Multi Frame Generation support is assumed for the
RTX 4070 Ti SUPER.

Automatic exposure is enabled by default. It meters the scene so that enabling
geometric sky occlusion does not leave the atrium at the raster view's fixed exposure.

## Controls

The panel uses Chinese on a Chinese Windows installation with a compatible system
font, and English otherwise. Fonts are loaded from the operating system; the sample
does not redistribute them. Text and controls follow the window's DPI scaling.

Start with a preset:

| Preset | Intended use |
|---|---|
| Recommended | Natural ray-traced light and denoising, with DLSS Quality and supported frame generation. |
| Quality first | Native-resolution anti-aliasing, with RR when available; higher GPU cost. |
| Performance first | Raster lighting and DLSS Balanced, with supported frame generation. |
| Native reference | Native resolution with FXAA; upscaling and frame generation off for comparison. |

The main controls combine **ray tracing and denoising** so that enabling ray tracing
does not accidentally expose noisy input. **Image quality** selects the balance
between internal resolution and reconstruction. The displayed sizes show internal
rendering resolution followed by window output resolution. **Exposure** changes
brightness: +1 EV doubles it and -1 EV halves it. **Reset daylight** restores the
natural lighting and exposure without changing reconstruction or frame generation.

Advanced options contain the separate reconstruction algorithms, noisy ray-traced
input comparison, supported frame multipliers, Reflex Boost, DeepDVC and daylight
controls. Raw SDK details and measurements are in a separate section. Frame
Generation shows the requested real/generated frame relationship; it is not a
measurement of displayed FPS.

Hold the right mouse button and use WASD to move, Q/E to move vertically and Shift
to accelerate. **F1** or the panel's close button hides the controls; F1 or the
small Show settings button restores them. Camera reset, lock and object-animation
pause are available for comparisons. The panel scrolls in short windows. Resizing
the main window changes output resolution; animation pause keeps rendering.

## Rendering and integration

- Native resolution uses FXAA. DLSS SR and DLAA receive untonemapped HDR color,
  depth and camera/object motion. RR receives the same scene with real noisy
  illumination, diffuse/specular albedo, world normals, linear roughness and
  specular hit distance. RR performs the reconstruction directly.
- The scene uses metallic/roughness materials, normal maps, alpha masking, a sky,
  a shadowed daylight sun and two moving objects with muted metal/ceramic materials.
  Optional warm fill lights are off by default. Sun height, direction and intensity,
  sky brightness and exposure are adjustable. glTF hierarchy transforms are evaluated
  on load; downloaded assets are unchanged.
- Automatic exposure meters HDR color after reconstruction, before tone mapping
  and UI composition. A stratified 128 x 128 sampling grid averages luminance in
  linear light before conversion to exposure stops. Averaging individual sample
  logs would overexpose the scene when many raw ray samples are zero.
  The meter targets 18% gray, limits adaptation to +/-8 stops and follows the
  preceding submitted frame, with one-second brightening and quarter-second
  darkening half-lives. Reset/resize initializes from the current measurement.
  **Brightness / Automatic exposure** disables adaptation for fixed-exposure
  comparisons; **Exposure compensation** adjusts the metered result. **Reset
  daylight** restores automatic exposure and neutral compensation. Lighting and
  Streamline's unexposed HDR inputs are not scaled by the meter.
- Hardware ray tracing uses a shared Slang `RayQuery` implementation: DXR 1.1
  `TraceRayInline` on DirectX 12 and `VK_KHR_ray_query` on Vulkan. It traces shadows
  and one stochastic diffuse and one GGX specular secondary ray per pixel. The
  secondary path evaluates up to two surface interactions with direct light and
  actual visibility, including emission or visible sky reached by the final
  scattered ray. Fixed secondary ambient fill is removed. Ray tracing with RR
  disabled exposes the noisy input. This remains a limited bounce renderer with an
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
- NIS consumes antialiased SDR color at input resolution. DirectSR runs on its
  required command queue, between two submitted command lists. DeepDVC processes
  tone-mapped SDR color before UI composition.
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

The information panel reports rendered FPS, CPU frame time, measured GPU rendering
time, and the available Reflex interval from simulation start to GPU completion.
Displayed FPS is marked unavailable because this implementation has no reliable
presentation measurement source. GPU rendering time excludes generated frames.

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

Resource preparation was checked on 2026-09-27. The exposure and traced-lighting
corrections were checked on 2026-09-28.

Development host: macOS arm64, .NET SDK 10.0.401.

- Solution Release build with warnings as errors: passed. Windows x64
  framework-dependent publishing also passed; native dependencies were inspected.
- PowerShell asset script: executed successfully using PowerShell 7.6.0; retrieved
  Streamline v2.14.1 production files and Sponza revision
  `7d4ba189827916452eeadc82d4b712dbc6280a6f`.
- Deployment inspection: interposer/plugins are at the output root; Sponza and its
  referenced data remain under `Assets/Scenes/`; all downloads remain ignored.
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
  GPU results: Chinese/English text, CJK glyph coverage, small-window scrolling,
  200% DPI and F1 hide/restore were checked. All 64 preset/capability combinations
  and the independent daylight reset were checked. Validation helpers remain outside
  the application and are not committed.

## Rendering references

- [Filament's material and lighting model](https://google.github.io/filament/main/filament.html)
  for GGX/Smith reflectance and indirect-light occlusion.
- [Heitz, Sampling the GGX Distribution of Visible Normals, JCGT 7(4), 2018](https://jcgt.org/published/0007/04/01/)
  for GGX visible-normal sampling.
- [PBRT, A Better Path Tracer](https://pbr-book.org/4ed/Light_Transport_I_Surface_Reflection/A_Better_Path_Tracer)
  for resolving environment/emission before terminating the scattering path.
