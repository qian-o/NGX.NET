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

It shows the GPU name and one **FPS** value, including generated frames when FG
is active. There is no DLSS version header, render/output resolution display,
separate Render FPS readout, close button, hidden state, reopen button or F1
visibility shortcut. The three graphics controls use the official feature names:

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
pass executes. Brightness
is metered automatically. There are no brightness controls, reset buttons,
advanced/details sections, status lists, tooltips or operation hints in the panel.

Camera navigation remains right mouse + WASD, Q/E for vertical motion, and Shift
for faster movement. Only supported, implemented DLSS features are exposed;
Dynamic Multi Frame Generation and 3D-Guided Neural Rendering are not added as
placeholder options.

## Rendering and integration

- Native RT is unfiltered when reconstruction is off; FXAA is used only by the
  unsupported-hardware raster fallback. DLSS reconstruction receives untonemapped HDR color,
  depth and camera/object motion. RR receives the same scene with real noisy
  illumination, diffuse/specular albedo, world normals, linear roughness and
  specular hit distance. RR performs the reconstruction directly. Sky and missed
  reflection rays use the guide's FP16_MAX distance (65504), independently of the
  finite scene traversal limit. World/view and projection matrices retain the
  documented row-major, left-multiplication convention.
- The scene uses metallic/roughness materials, normal maps, alpha masking, a sky,
  a shadowed daylight sun and two moving metal/ceramic objects. The metal sphere
  is now a polished reflection reference (roughness 0.08, formerly 0.27), and the
  spheres use 64 segments / 32 rings for a smoother silhouette. Imported architecture
  retains its authored materials, normals and roughness.
  Daylight and exposure metering are automatic. glTF hierarchy
  transforms are evaluated on load; downloaded materials and textures are unchanged.
- A depth prepass applies the same sidedness, alpha cutoff, anisotropic footprint
  and mip bias as the material pass. Opaque fragments only test sidedness there.
  The material pass uses early equal-depth tests with depth writes disabled, so
  hidden surfaces do not run the full material shader. Its triangle order is reversed
  while winding is preserved, retaining the original first-visible primitive when
  coplanar faces overlap. Both passes use the same vertex shader and camera constants;
  the fourth `parameters` component supplies the reverse-order triangle count only
  for the color pass. Vulkan explicitly synchronizes depth writes between rendering scopes.
- Automatic exposure meters HDR color after reconstruction, before tone mapping
  and UI composition. Each 16 x 16 output-pixel tile averages luminance in linear
  light before conversion to exposure stops; partial edge tiles retain their
  actual pixel count. One 8 x 8 thread group cooperates on each tile, fetching four
  pixels per lane and reducing their sums in shared memory. Averaging individual
  sample logs would overexpose the scene
  when many raw ray samples are zero.
  The meter targets 18% gray, limits adaptation to +/-8 stops and follows the
  preceding submitted frame, with one-second brightening and quarter-second
  darkening half-lives. Exposure history uses 32-bit floats to preserve small
  adaptation steps at high frame rates. Reset/resize initializes from the current measurement.
  Lighting and Streamline's unexposed HDR inputs are not scaled by the meter.
- Local exposure estimates illumination using aligned material reflectance guides,
  then separates its broad base from detail with a bilateral filter. A pixel-weighted
  histogram controls compression toward a six-stop base range, capped at +/-2 stops
  of local correction. The illumination anchor and compression adapt over time.
  Joint bilateral upsampling protects silhouettes; texture RGB is never blurred,
  and zero radiance stays black. This makes shadow detail visible while retaining
  highlight color, without changing the incoming light or material textures.
  AgX's default view transform replaces the per-channel ACES approximation,
  with its published color-space matrices and highlight response. No saturation
  boost or additional creative look is applied. The implementation and MIT notice
  are in `Assets/Shaders/ToneMapping.slang` and `LICENSE-AgX.txt`.
- Hardware ray tracing uses a shared Slang `RayQuery` implementation: DXR 1.1
  `TraceRayInline` on DirectX 12 and `VK_KHR_ray_query` on Vulkan. Each pixel uses
  one diffuse and one GGX specular sample per frame, plus sun/sky visibility samples.
  A stable pixel rotation visits all four 2 x 2 strata across four frames. This
  replaces four samples of each lobe per frame to reduce tracing cost. Individual
  raw frames are noisier; RR's final temporal quality requires Windows acceptance.
  Secondary paths evaluate up to eight surface scattering events. Their throughput
  includes the primary diffuse/specular weight, and continuation uses Russian
  roulette with probability `sqrt(clamp(max(throughput), 0, 1))`. Surviving paths
  divide their throughput by that probability, preserving expected lighting without
  a nonzero energy cutoff. The first hit and its lighting are always evaluated;
  a diffuse first hit that has received no light keeps one extra event before
  roulette starts. This conservative survival rule protects dark-region variance
  better than terminating proportionally to the unmodified path weight. Emission
  or visible sky reached by the final scattered ray is still resolved. Noise
  distribution changes, so RR's motion quality requires target-machine acceptance.
  Sky importance sampling uses the scene's upper bounding rectangle as a portal,
  with full geometry visibility and multiple importance sampling against the BSDF.
  The rectangle emits no light; it directs samples toward the atrium sky to reduce
  variance. This remains a finite-depth renderer with an analytic daylight environment.
  Target GPU performance and RR motion quality have not been measured for this revision.
- Diffuse and specular paths run in independent dispatch layers. A separate lighting
  pass combines them, writes RR albedo guides and handles sky motion. It contains
  no ray queries. A shared two-layer RGBA32F scratch image retains full intermediate
  precision and is reused on the ordered graphics queue, with read/write barriers.
  It is never tagged for the SDK and is not duplicated across frame slots (19.5 MiB
  at the reported 1067 x 600 input). Specular distance has one traced writer per
  foreground pixel; the resolve pass writes the sky sentinel only for background.
- Direct shading, GGX visible-normal sampling and the RR specular guide use the same
  height-correlated Smith model. This improves grazing-angle behavior without
  clamping away real lighting. The GGX distribution retains its normalized peak at
  low roughness, consistent with the specular sampling PDF. The raster path adds screen-space contact occlusion
  to indirect light only; the traced path uses geometric visibility instead.
- Architecture is partitioned by opacity and sidedness into homogeneous rigid
  objects, each with one non-indexed BLAS built at startup. Opaque surfaces are
  accepted by hardware traversal; only alpha-tested surfaces invoke the candidate
  shader. Ray back-face culling handles single-sided geometry, with culling disabled
  on double-sided instances. The current scene has four BLAS ranges, including the
  two moving objects; 235,263 of 270,203 triangles (87.07%) use opaque traversal.
  Sponza's geometry is static; the moving objects update TLAS instance transforms. Each frame slot owns
  its TLAS, scratch buffer and instance upload allocation. A slot is first built,
  then updated in place after its GPU fence completes. Resize and reconstruction
  changes reuse the geometry acceleration structures.
- Ray-query candidates apply the material's alpha cutoff before committing hits.
  The instance ID and primitive index locate the corresponding
  shared vertices, UVs and materials. There is no software BVH rendering path.
- Visibility queries have a compile-time first-hit flag and return only occlusion.
  Indirect/reflection queries separately return the closest hit, including its
  distance and barycentrics. Both use the same candidate alpha tests and culling.
- When ray-query capabilities are unavailable, lighting uses a raster sun shadow
  map. The renderer compiles its raster lighting variant and disables ray
  tracing and RR while keeping the other supported rendering/features available.
  Vulkan requests its ray-query, acceleration-structure and buffer-device-address
  features before device creation, independently of the interposer's SDK needs.
- Textures retain their source dimensions and receive a full mip chain. Base color
  and emissive maps are decoded to linear color during sampling. A 1 KiB table at
  the start of the texel buffer caches the sRGB decode for all 256 byte values;
  texture offsets include this prefix. This replaces repeated per-texel powers
  without changing the texture bytes or filtering. Normals and
  metallic/roughness maps are sampled as data. Each channel uses its own texture
  dimensions for mip selection. The primary surface uses up to 8 anisotropic
  samples along the principal axis of the texel footprint, with trilinear mip
  filtering per sample. DLSS SR/DLAA/RR apply the documented mip bias
  `log2(inputWidth / outputWidth) - 1`; native rendering keeps zero bias. Secondary
  hit textures use the same bias. This preserves detail before reconstruction,
  rather than sharpening a blurred final image.
  Bilinear corners wrap with single-edge bounds checks: `frac(uv)` restricts them
  to `[-1, size]`, eliminating repeated integer remainders while retaining negative
  UV wrapping, non-power-of-two textures and one-texel mip levels.
  Degenerate authored tangents use the same orthogonal fallback as missing tangents,
  avoiding zero-vector normalization in normal mapping and indirect paths.
- Sun directions below the shading horizon skip visibility queries because their
  BRDF contribution is zero. The integrated specular-albedo guide is evaluated only
  when RR or the raster environment needs it. The bounce limit, texture resolution,
  anisotropic filtering and local-exposure behavior remain unchanged. The lower
  per-frame path count applies with RR both on and off; ray tracing remains active.
- The UI renders separately with premultiplied alpha at output resolution. HUD-less
  color and UI obey `final.rgb = ui.rgb + (1 - ui.a) * hudless.rgb`.
- Frame generation uses the interposer's swap chain and presentation hooks. Its
  plugin is loaded/unloaded with swap-chain reconstruction. The same real-frame
  token is used for constants, tags, evaluation and latency markers. Generated
  frames do not advance simulation or camera history.
- Three frame slots protect pending GPU work. Resize/mode changes wait for relevant
  work, release SDK resources according to the feature contract, recreate targets
  and reset history. Normal frame rendering waits only when reusing a frame slot.
- Leaving RR recreates the SDK session and graphics device after GPU idle. This
  isolates a Streamline 2.14.1 lifetime hazard: the RR plugin stores matrix addresses
  in shared NGX parameters and erases their viewport on release; the SR plugin does
  not replace those matrix parameters. This is consistent with the reported
  `WorldToScreenMatrix not invertible` warning after disabling RR on both backends.
  CPU scene data, animation, camera and selected settings survive the transition;
  temporal histories restart. Compiled shaders are cached per backend/entry/variant,
  and the wrapper's process-lifetime library path is configured only once. The
  switch may pause briefly. Native Windows warning elimination remains to be verified.
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
next window. Base rendering throughput can fall when FG adds GPU work even while
presentation FPS rises. The panel exposes only the presentation rate.

## Build and diagnostics

```powershell
dotnet build Streamline.NET.slnx -c Release --warnaserror
```

Slangc.NET and DXC native libraries are restored through NuGet. The application
uses the compiler's default downstream-library discovery without overriding its
DXC path. Shaders compile on first use and reuse their bytecode during session recreation.

The SDK writes its diagnostics to `Logs/` under the output directory. Startup and
feature availability are printed to the console. Managed failures also write a
`showcase-*.log` file there. For Windows acceptance, record the selected backend,
GPU and driver, SDK version, input/output sizes and feature settings, together with
logs and screenshots of any issue.

For performance diagnosis, `Logs/performance.jsonl` records one capture after
startup and each configuration change. It skips three potentially stale frame-slot
results and averages 30 completed GPU frames, using seven native timestamps. Each
record contains geometry/TLAS, lighting, reconstruction, post-processing, and
UI/composition/copy intervals, along with backend, GPU, actual input/output sizes,
settings and render FPS. `LightingMs` remains the total lighting interval for
comparison with earlier logs; `RayTracingMs` and `LightingResolveMs` now split it,
and `PrimarySamplesPerLobe` records the sampling rate. These are graphics-queue intervals, not GPU execution
of generated frames or CPU/presentation latency. Timestamp readback uses already
completed frame slots; no extra GPU-idle wait is added. The Settings panel and
per-frame console output are unchanged. Keep a configuration running for at least
10 seconds when collecting a capture on a slow GPU.

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

Resource preparation was checked on 2026-09-27. RR independence, texture/reflection
inputs, sky sampling, local exposure and rendering-work reductions were checked
on 2026-09-28; earlier checks remain below.

Development host: macOS arm64, .NET SDK 10.0.401.

- Solution Release build with warnings as errors: passed. Windows x64
  framework-dependent publishing also passed; native dependencies were inspected.
- RR-exit handling: 2,000 moving-camera cases verified finite, invertible projection,
  history and RR world-to-screen matrices. Twelve mode transitions verified the
  restart boundary and retained CPU state. An isolated harness exercised the actual
  application loop with fake devices, checking disposal-before-reinitialization,
  state transfer, normal close and initialization failure for both backend selections.
  The real shader compiler cache reused SPIR-V bytecode and kept ray/raster variants
  distinct. These checks do not execute native Streamline teardown/reinitialization.
- GPU capture checks verified unit conversion, bounded 30-frame averaging, stale-slot
  exclusion after reconfiguration, Vulkan valid-bit rollover, invalid full-width
  timestamp rejection and the actual JSON writer with explicitly synthetic ticks.
  The laptop's reported 15 FPS is not a measured post-fix result; target captures
  are needed before attributing its frame time to tracing, DLSS or post-processing.
- PowerShell asset script: executed successfully using PowerShell 7.6.0; retrieved
  Streamline v2.14.1 production files and Sponza revision
  `7d4ba189827916452eeadc82d4b712dbc6280a6f`. The new runtime filter was checked
  separately against production/development and unrelated plugin entries; the
  network download was not repeated for this change.
- Deployment inspection: interposer/plugins are at the output root; Sponza and its
  referenced data remain under `Assets/Scenes/`; all downloads remain ignored.
  A fresh Windows publish contains the 11 DLSS/Reflex dependency DLLs and excludes
  DeepDVC, NIS, DirectSR and nvperf binaries, even with older cached SDK files present.
- Scene preparation: passed, 270,203 triangles including the moving objects,
  28 material records and 69 decoded texture resources with mip chains.
- All sixteen SPIR-V shaders and HLSL translations, plus raster variants of tracing
  and lighting: compiled successfully. The hardware tracing binary contains SPIR-V
  ray-query instructions and its acceleration-structure binding in `TraceLighting`;
  `Lighting` and both raster variants contain neither.
- CPU checks for this revision: all 270,203 triangles map into four complete,
  non-overlapping BLAS ranges with correct material/object references. DXR and
  Vulkan emit identical 64-byte instance records, including 24-bit IDs, visibility
  masks, sidedness flags, BLAS addresses and translated positions. Moving/paused instances preserve
  raster history. The raster shadow projection encloses the scene bounds.
- SPIR-V member offsets and strides match the 48-byte object records and 496-byte
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
- The current shared sky-sampling shader ran in an isolated Apple M4 harness:
  a synthetic aperture integral agreed with numerical quadrature, MIS and brute-force
  sampling agreed within sampling error, and the MIS estimator had lower variance.
  GGX distribution/PDF probes remained finite and retained the low-roughness peak.
  These are mathematical checks, not scene performance measurements.
- User-supplied Windows captures on RTX 3050 Laptop / DirectX 12 contained two
  Quality/RR-on runs at 1067 x 600 input and 1600 x 900 output. Their averages were
  189.27 ms total, 164.74 ms lighting (87.04%), 11.27 ms geometry, 12.76 ms RR and
  0.36 ms post-processing, approximately 5.3 render FPS. The file did not contain
  Ultra Performance or RR-off captures and cannot verify the RR-exit warning fix.
- The user's next capture, with the same Quality/RR-on sizes and one sample per
  lobe, measured 58.13 ms total / 17.20 FPS: geometry 11.49 ms, tracing 32.03 ms,
  lighting resolve 0.76 ms, RR 13.34 ms and post-processing 0.37 ms. This confirms
  the preceding sampling revision on RTX; it precedes the depth-prepass/visibility
  changes below and is not evidence of their Windows speedup.
- After the depth/visibility revision, the user's same-mode RTX capture measured
  51.69 ms / 19.33 FPS: geometry 3.88 ms, tracing 33.07 ms, lighting resolve
  0.77 ms and RR 13.45 ms. Geometry improved substantially; tracing and RR still
  dominated, motivating the continuation-policy change described above.
- Continuation-policy validation used the shared ray-query shaders, real Sponza
  geometry/textures and the existing 512 x 288 primary-surface fixture on Apple M4.
  More aggressive policies were rejected after measuring dark-region variance.
  The selected policy reduced median lighting time from 22.21 to 12.31 ms. A
  near-equal GPU-time comparison used 128 old-policy frames versus 224 new-policy
  frames (about 2.84 versus 2.76 seconds of lighting work), against an independent
  512-frame reference. Surface, dark-region and metal HDR mean-squared errors all
  decreased in this fixture. Mean luminance was 0.0073314 before, 0.0073273 after,
  versus reference 0.0073258; the estimates agree within sampling uncertainty.
  Every sampled first-specular-hit distance remained identical, and all HDR values
  stayed finite. Primary sample count, materials, exposure, ray-tracing activation
  and the eight-event maximum remain unchanged. These are isolated Metal results;
  native Windows speedup and RR behavior during motion have not been measured.
- Depth/visibility revision: actual shared raster shaders and scene data executed
  on Apple M4 at 1067 x 600 with reconstruction mip bias, and 513 x 289 with jitter
  and nonzero motion. Depth, alpha coverage and motion matched bit-for-bit. A
  primitive-ID diagnostic also matched the original visible triangles. There were
  small interpolation/derivative differences in material outputs (401 of 8,962,800
  components at 1067 x 600; largest albedo RMS difference 3.9e-5), so this is not a
  claim of bit-identical material shading. Median geometry time changed from
  10.53 to 4.86 ms; the smaller case changed from 4.57 to 2.20 ms.
  The isolated Metal adapter maps fragment discard and early-test annotations to
  Metal equivalents and remaps register bindings; it is not a Windows backend run.
- Specialized visibility plus texture wrapping produced identical HDR pixels and
  specular hit distances across 16 shared-shader GPU lighting frames. Median time
  changed from 22.76 to 21.32 ms on Apple M4. Forty GPU texture cases covered
  negative/repeated UVs, 1 x 1 and non-power-of-two sizes, fractional/clamped mips,
  sRGB/linear data and alpha with bit-identical results. SPIR-V checks confirmed
  early tests only on the material pass and distinct closest-/first-hit ray flags.
  Sampling count, eight-event scattering limit, RR settings and scene lighting were
  unchanged in this revision; native Windows speed and appearance still need acceptance.
- The actual old/new shared lighting shaders ran through Metal ray queries on
  Apple M4 with the real 270,203-triangle scene, four BLASes and all textures, using
  a 512 x 288 CPU-generated primary-surface fixture. Median lighting time changed
  from 109.31 ms to 24.69 ms. This includes the deliberate per-frame sampling
  reduction; it is not an RTX result or an equal per-frame-noise comparison.
  Old 32-frame/four-sample and new 128-frame/one-sample averages used equal cumulative
  samples per lobe. HDR means were 0.0073251 and 0.0073314, within sampling error;
  RR specular-albedo guides matched exactly and hit distances remained finite.
  The shader resolve also passed odd 257 x 129 dimensions, exact lobe/emission
  summation, scratch reuse, sky with uninitialized sample slots and hit-distance
  preservation. Metal test bindings were remapped for its shared register namespace;
  native Windows synchronization, speed and reconstructed motion quality still need acceptance.
- Performance-revision equality checks compared all 270,203 complete triangles
  before/after partitioning, including winding, vertex attributes, world positions
  and animation. Materials and every texture/mip byte remained identical. All
  256 cached sRGB values agreed with a double-precision reference within 2e-7.
  Compiled ray queries retain hardware culling and first-hit termination for
  visibility rays, and the raster variant contains no ray-query instructions.
- Shared-shader GPU microbenchmarks on Apple M4 used alternating before/after runs,
  warm-up and the median of 20 samples. At 2560 x 1440, exposure preparation changed
  from 2.59 ms to 0.92 ms, with identical half-float tile outputs in that fixture.
  The 1024 x 1024 texture-sampling probe at 8:1 anisotropy changed from 1.84 ms to
  1.53 ms; maximum tested sRGB output difference was 3.6e-7, with exact linear-data
  and alpha results. Odd dimensions and the existing local-exposure regressions
  also passed. These isolated timings do not measure the full renderer or predict
  RTX frame rates. Native Windows DXR/Vulkan culling, alpha silhouettes and actual
  frame-time improvement require target-machine acceptance.
- All four current exposure/tone-map passes also ran on Apple M4. Checks covered
  odd image dimensions, weighted metering, invalid reset history, exact black,
  material contrast, an illumination edge crossing a tile, and adaptation at
  30/120 FPS. A 128 x 80, 128-sample CPU Sponza reference remained finite; nearest
  enlargement supplied a representative post-process image size for the shared GPU
  passes. Its arch shadows gained detail while floor brightness and sky clipping
  decreased. This offline comparison does not validate Windows Ray Query or DLSS RR.
- Current UI checks use offscreen ImGui draw data with fixture values, not measured
  GPU results: English text under English/Chinese UI cultures, a 360 x 160 window with
  scrolling and 200% DPI passed. Title dragging moved the window, collapse/expand
  worked, corner dragging could not resize it, and neither the title corner nor
  F1 closed/hid it. Changing content changed its automatic width. Capability defaults and the 12
  DLSS mode / RR combinations select one reconstruction path, including
  native-resolution RR with upscaling off. Fixture FPS and GPU labels are test
  data, not measured Windows performance.
- Frame-statistics tests passed for SDK-supplied presentation counts, generated
  frame drops, zero samples, unavailable queries, recovery, irregular intervals,
  and counter resets after resize/toggle/pause. The Windows native SDK counter and
  actual FG performance require target-machine validation; no measured Windows
  FPS increase is claimed.
- Shared shader sampling probes executed on Apple M4: 8:1 footprints in both
  axes preserved one-texel stripe detail that the previous isotropic mip collapsed
  to gray; constant-color and linear/sRGB sampling remained correct. Native,
  DLAA, Quality and Performance mip-bias values passed numeric checks. GGX probes
  confirmed that the old 0.27 material scatters reflection directions broadly
  (about 8.3 degrees at the median, versus 0.73 degrees for the polished reference).
  The RR sky-distance sentinel remained finite and equal to 65504. These checks
  validate the renderer inputs, not native RR denoising on Windows. Motion/reflection
  clarity and the performance cost of anisotropic filtering still need RTX acceptance.
- The shared AgX/tone-map shaders ran on Apple M4 in the isolated harness. Across
  32 color/exposure cases, 8-bit GPU results matched an independent evaluation of
  the published reference. The same HDR Sponza reference was compared before/after
  tone mapping with identical exposure. Windows DLSS/RR visual acceptance remains
  with the user. Validation helpers are outside the application and are not committed.

## Rendering references

- [Streamline 2.14.1 RR plugin](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/source/plugins/sl.dlss_d/dlss_dEntry.cpp)
  and [SR plugin](https://github.com/NVIDIA-RTX/Streamline/blob/v2.14.1/source/plugins/sl.dlss/dlssEntry.cpp)
  for shared NGX parameters and matrix/viewport lifetimes during feature switching.
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
