using System.Numerics;
using System.Runtime.InteropServices;

namespace Showcase.Models;

[StructLayout(LayoutKind.Sequential)]
internal struct FrameConstants
{
    public Matrix4x4 ViewProjection;

    public Matrix4x4 CurrentViewProjection;

    public Matrix4x4 PreviousViewProjection;

    public Matrix4x4 InverseViewProjection;

    public Vector4 Camera;

    public Vector4 Size;

    public Vector4 Sun;

    public Vector4 Scene;

    public Vector4 Parameters;

    public Vector4 Lighting;

    public Vector4 LightSelection;
    // Matches uint4 integrator in the shader.
    public uint SamplesPerPixel;

    public uint MaxDepth;

    public uint SampleOffset;

    public uint FilmSamples;

    public Vector4 Exposure;
    // Ground elevation, scene-units to km, observer altitude, aerial range (km).
    public Vector4 Atmosphere;

    public Matrix4x4 InverseAtmosphereViewProjection;
}
