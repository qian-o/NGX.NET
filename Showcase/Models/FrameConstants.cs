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
    public Vector4 Jitter;
    public Matrix4x4 SunViewProjection;
    public Vector4 Lighting;
    public Vector4 Exposure; // automatic metering enabled, delta seconds, reset history, reserved
    public Vector4 EnvironmentMinimum;
    public Vector4 EnvironmentMaximum;
    public Vector4 PreviousCamera;
}
