using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct GeometryRange(uint firstVertex, uint vertexCount, uint opaque, uint doubleSided)
{
    public readonly uint FirstVertex = firstVertex;

    public readonly uint VertexCount = vertexCount;

    public readonly uint Opaque = opaque;

    public readonly uint DoubleSided = doubleSided;
}
