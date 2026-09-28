using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal record struct GeometryRange(uint FirstVertex, uint VertexCount, uint Opaque, uint DoubleSided);
