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

Default output is a resizable 1600 x 900 window with DLSS Quality and Reflex Low
Latency when supported. Frame Generation is initially off. On the RTX 4070 Ti SUPER,
select the frame-generation multiplier offered by the SDK; the application does
not assume Multi Frame Generation support.

Hold the right mouse button and use WASD to move, Q/E to move vertically and Shift
to accelerate. The panel provides camera reset, a fixed camera, animation pause,
exposure, reconstruction quality, ray tracing, frame generation, Reflex and DeepDVC.
Changing window size changes output resolution. Animation pause keeps rendering.

## Rendering and integration

- Native resolution uses FXAA. DLSS SR and DLAA receive untonemapped HDR color,
  depth and camera/object motion. RR receives the same scene with real noisy
  illumination, diffuse/specular albedo, world normals, linear roughness and
  specular hit distance. RR performs the reconstruction directly.
- The scene uses metallic/roughness materials, normal maps, alpha masking, a sky,
  a shadowed sun, local lights and two moving objects with different materials.
  glTF hierarchy transforms are evaluated on load; downloaded assets are unchanged.
- The shared ray tracer traverses a software BVH on shader cores. It traces shadows
  and, in ray-tracing mode, one stochastic diffuse and one GGX specular secondary
  ray per pixel. The secondary surface receives direct lighting. This is a limited
  bounce renderer; RT-core acceleration and a separate conventional denoiser are
  outside this implementation. Ray tracing with RR disabled exposes the noisy
  input. Performance needs measurement on the target GPU.
- Textures retain their source dimensions and receive a full mip chain. Base color
  and emissive maps are decoded to linear color during sampling. Normals and
  metallic/roughness maps are sampled as data.
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
dotnet run --project Showcase -c Release -- --check-shaders
dotnet run --project Showcase -c Release -- --check-scene
```

`--check-shaders` compiles all nine shader entry points to DXIL and SPIR-V on
Windows. Slangc.NET and DXC native libraries are restored through NuGet; a separate
shader SDK installation is not required. On other supported compiler platforms,
the check compiles SPIR-V and explicitly reports that Windows DXIL remains unchecked;
build/run with `-p:PlatformTarget=AnyCPU` when the host is not x64.
`--check-scene` loads and prepares the complete scene without initializing graphics.

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

## Verification record — 2026-09-27

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
- All nine SPIR-V shaders and all nine HLSL translations: compiled successfully.
- CPU checks: buffer layouts, complete BVH coverage and traversal depth, texture
  mip bounds, camera jitter/reprojection, animation pause and history commit passed.
  SPIR-V member offsets and array strides match the CPU buffer layouts.
- Windows DXIL compilation, device creation, actual DLSS/RR/FG behavior, GPU timings,
  image quality, backend parity and resize/exit stability: pending Windows acceptance.
  No GPU performance results or rendered screenshots have been claimed.
