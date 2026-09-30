# NGX.NET Showcase

A .NET 10 Sponza renderer using Silk.NET. Compare native rendering with DLSS Super Resolution, Ray Reconstruction and 2× Frame Generation where supported by the GPU and driver.

## Run

From the repository root, run with Vulkan (default):

```shell
dotnet run --project Showcase -c Release
```

Or select DirectX 12:

```shell
dotnet run --project Showcase -c Release -- --backend directx12
```

## Controls

Hold the right mouse button to look around. Use WASD to move, Q/E to move down/up, and Shift to move faster.

Use the settings panel to select DLSS quality, toggle Ray Reconstruction and Frame Generation, or pause animation.
