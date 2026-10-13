# NGX.NET Showcase

A Sponza path-tracing demo for [NGX.NET](..), using DirectX 12 or Vulkan.

- DLSS Super Resolution, Ray Reconstruction and Frame Generation.
- Daylight sky, atmospheric scattering and automatic exposure.
- Progressive rendering when Super Resolution and Ray Reconstruction are off.

## Run

Requires the .NET 10 SDK and a GPU with DXR 1.1 or Vulkan 1.3 ray queries. DLSS features are enabled according to GPU and driver support.

From the repository root:

```sh
dotnet run --project Showcase -c Release
```

Select DirectX 12 or Vulkan at startup. DirectX 12 requires Windows; Vulkan is available on Windows and Linux.

## Controls

| Input | Action |
| --- | --- |
| Hold right mouse button | Look around |
| WASD | Move |
| Q / E | Move down / up |
| Shift | Move faster |

The settings panel controls DLSS, daylight and animation. Animation starts paused. To inspect progressive convergence, turn off Super Resolution and Ray Reconstruction and keep the camera and scene still.

## Sources

- Path tracing adapts [pbrt-v4](https://github.com/mmp/pbrt-v4/tree/b4ce9687e6c695f5582997c61b0c66cf064bdb4a) in Slang — [Apache-2.0](Assets/Shaders/Pbrt.LICENSE.txt).
- Sky and atmosphere adapt [Sébastien Hillaire's implementation](https://github.com/sebh/UnrealEngineSkyAtmosphere/tree/183ead5bdacc701b3b626347a680a2f3cd3d4fbd) — [MIT](Assets/Shaders/Atmosphere.LICENSE.txt).
