# Streamline.NET native runtime setup

The NuGet package contains managed bindings. Download NVIDIA's native runtime separately.

## 1. Download the Windows x64 SDK

Open the [official Streamline releases](https://github.com/NVIDIA-RTX/Streamline/releases) and select a release compatible with your Streamline.NET package.

In that release's **Assets** list, download **`streamline-sdk-<release-tag>.zip`**, where `<release-tag>` is the release's `v`-prefixed version. For Windows x64, choose the ZIP **without** an `-aarch64` or `-arm64ec` suffix. GitHub's **Source code** archives contain source code rather than the prebuilt SDK runtime.

## 2. Copy the production runtime files

Extract the SDK ZIP and open **`bin/x64/`**. This is the production runtime directory; it contains `sl.interposer.dll`, `sl.common.dll`, the feature plugins and their dependencies.

Copy **all files directly inside `bin/x64/`** into a directory of your choice. This includes the DLLs and the license files beside them. Do not copy the `development` subdirectory for normal deployment. Also preserve the SDK's accompanying `license.txt` and applicable third-party notices.

Copying the complete production set avoids having to determine each feature's DLL dependencies. Keep files from the same SDK release together, and configure your application to copy them to its build and publish output.

## 3. Set the interposer path

Before calling the SDK, set the absolute path to the `sl.interposer.dll` you copied:

```csharp
using Streamline.NET;

SL.SetLibraryPath(@"C:\Path\To\sl.interposer.dll");
```

Replace the example path with your actual file location. This selects the entry library; SDK initialization is still required.

## 4. Initialize Streamline

Follow the [official integration guide](https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md) for initialization and graphics-device setup. The [Showcase](https://github.com/qian-o/Streamline.NET/tree/master/Showcase) provides a complete C# example for DirectX 12 and Vulkan.
