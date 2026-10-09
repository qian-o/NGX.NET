using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct TextureDescription(uint offset, uint width, uint height, uint mips)
{
    public readonly uint Offset = offset;

    public readonly uint Width = width;

    public readonly uint Height = height;

    public readonly uint Mips = mips;
}
