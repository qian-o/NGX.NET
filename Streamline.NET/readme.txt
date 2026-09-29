Streamline.NET - native runtime setup
=====================================

Streamline.NET contains the managed bindings. Your application supplies NVIDIA's
native runtime and plugins.

1. Download the native runtime

   Official SDK releases:
   https://github.com/NVIDIA-RTX/Streamline/releases

   Choose a runtime compatible with the Streamline.NET package you installed.
   For Windows x64 deployment, use the SDK's production bin/x64 files. Keep the
   interposer, the selected feature plugins, their runtime dependencies and the
   accompanying license files together. The SDK also contains separate debug
   and development builds; follow NVIDIA's integration guide when using them.

2. Place the files in an application-owned directory

   The files do not have to sit beside your executable. For example:

   MyApplication.exe
   Assets/
     Streamline/
       sl.interposer.dll
       sl.common.dll
       ...selected feature plugins and their dependencies...
       ...the accompanying license files...

   Configure your project to preserve this directory in both build and publish
   output. Installing this NuGet package does not download or copy native files.

3. Configure the entry library and plugin search paths

   using Streamline.NET;

   string runtimeDirectory = Path.GetFullPath(
       Path.Combine(AppContext.BaseDirectory, "Assets", "Streamline"));

   SL.SetLibraryPath(Path.Combine(runtimeDirectory, "sl.interposer.dll"));

   SetLibraryPath accepts the absolute path of the entry library file. It saves
   the path; the first SDK call loads that file. Configure it before any SDK call.
   Once loaded, the library remains loaded until process exit.

   Before SL.Init, configure Preferences.PathsToPlugins with your absolute
   runtime directory and set Preferences.NumPathsToPlugins accordingly. This
   field is a native UTF-16 string-pointer array; keep its storage valid for the
   SDK's required lifetime. The Showcase retains it until SL.Shutdown.

   Select the required features through Preferences.FeaturesToLoad and
   Preferences.NumFeaturesToLoad. Streamline loads the feature plugins and
   resolves their native dependencies. Do not call SetLibraryPath for each DLL.

   Then follow the SDK's initialization and graphics-device setup sequence.
   Use new() for SDK structures so their type and version headers are initialized.

4. Integration and a complete example

   NVIDIA integration guide:
   https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md

   Streamline.NET Showcase (DirectX 12 and Vulkan):
   https://github.com/qian-o/Streamline.NET/tree/master/Showcase

   The Showcase preserves the entire Assets directory in its output and loads
   Streamline from Assets/Streamline. Its asset script downloads the sample scene
   and native runtime; it is separate from this managed NuGet package.

Project license: LICENSE
Third-party attributions and license text: THIRD-PARTY-NOTICES
