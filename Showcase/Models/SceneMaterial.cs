using System.Numerics;
using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneMaterial
{
    public Vector4 BaseColor;
    public Vector4 EmissiveMetallic;
    public Vector4 Parameters;
    public Vector4 Textures;
}
