# NGX.NET Showcase

A Windows x64 Sponza renderer using .NET 10, DirectX 12 or Vulkan. Scene assets are included in the repository.

```shell
dotnet run --project Showcase -c Release
```

Choose a backend at startup. The settings panel provides DLSS quality, ray reconstruction and 2× frame generation when supported by the selected GPU and driver. Ray tracing, camera controls, lighting and animation remain available independently of reconstruction.

The sample calls NGX on the real device and command buffer. It retains each rendered frame while a separate presenter submits the generated frame, then the real frame at the intermediate interval. GPU fences and per-slot presentation completion protect resources through reuse and resizing.

Reflex and latency markers use NVAPI for DirectX 12 and `VK_NV_low_latency2` for Vulkan. Frame generation requires that low-latency integration to be available. There is no Streamline interposer or separate PCL ETW channel.
