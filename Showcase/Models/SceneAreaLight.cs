using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct SceneAreaLight
{
    public uint Triangle;

    public float Area;

    public float Pmf;

    public float Cdf;
}
