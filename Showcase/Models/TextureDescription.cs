using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal record struct TextureDescription(uint Offset, uint Width, uint Height, uint Mips);
