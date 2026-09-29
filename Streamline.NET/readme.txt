Streamline.NET - native runtime setup
=====================================

Your application supplies NVIDIA's native runtime and plugins. This NuGet
package contains the managed bindings and does not download native files.

1. Download a compatible SDK from the official releases:
   https://github.com/NVIDIA-RTX/Streamline/releases

   Deploy the interposer, required plugins, dependencies and accompanying license
   files in any directory you choose. No particular folder layout is required.

2. Before any SDK call, pass the absolute filename of sl.interposer.dll to
   SL.SetLibraryPath. This configures the entry library, not the plugin paths.

3. Create Preferences with new(). Set PathsToPlugins to an array of absolute
   plugin directory paths (UTF-16 strings), and set NumPathsToPlugins. Select
   features with FeaturesToLoad and NumFeaturesToLoad. Keep these arrays and
   strings pinned or in unmanaged memory for the lifetime required by the SDK.
   Streamline loads the plugins; do not call SetLibraryPath for each DLL.

4. Follow NVIDIA's SDK initialization and graphics-device setup sequence:
   https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md

   Complete DirectX 12 and Vulkan example:
   https://github.com/qian-o/Streamline.NET/tree/master/Showcase

Project license: LICENSE
Third-party attributions and license text: THIRD-PARTY-NOTICES
