using System.Numerics;
using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneObject
{
    // W stores the sphere radius, or zero for a mesh, because both share this instance layout.
    public Vector4 Offset;

    public Vector4 PreviousOffset;

    public GeometryRange Geometry;
}
