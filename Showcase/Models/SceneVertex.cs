using System.Numerics;
using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneVertex
{
    public Vector4 Position;

    public Vector4 Normal;

    public Vector4 Tangent;

    public Vector4 UV;
}
