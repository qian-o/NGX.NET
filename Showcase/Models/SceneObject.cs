using System.Numerics;
using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneObject
{
    public Vector4 Offset; // xyz: translation; w: analytic sphere radius, or 0 for a mesh

    public Vector4 PreviousOffset;

    public GeometryRange Geometry;
}
