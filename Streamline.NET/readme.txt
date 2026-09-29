Streamline.NET - native runtime setup
=====================================

This NuGet package contains managed bindings only. The native Streamline
libraries must be supplied separately.

1. Download the native libraries from NVIDIA's official Streamline SDK releases:
   https://github.com/NVIDIA-RTX/Streamline/releases

   Keep the interposer, required plugins and their dependencies together in a
   directory of your choice, along with the accompanying license files.

2. Set the interposer's absolute file path before calling the SDK:

   Streamline.NET.SL.SetLibraryPath(@"C:\Path\To\sl.interposer.dll");

   Replace the example path with the actual location on your machine.
   This selects the library; SDK initialization is still required.

3. Follow NVIDIA's integration guide to initialize Streamline and connect it
   to your graphics device:
   https://github.com/NVIDIA-RTX/Streamline/blob/main/docs/ProgrammingGuide.md

   Complete DirectX 12 and Vulkan example:
   https://github.com/qian-o/Streamline.NET/tree/master/Showcase

Project license: LICENSE
Third-party attributions and license text: THIRD-PARTY-NOTICES
