using System.Numerics;
using Showcase.Models;

namespace Showcase;

internal sealed partial class Scene
{
    public SceneVertex[] Vertices { get; private set; } = [];

    public SceneMaterial[] Materials { get; private set; } = [];

    public uint[] Texels { get; private set; } = [];

    public TextureDescription[] TextureInfo { get; private set; } = [];

    public SceneObject[] Objects { get; private set; } = [];

    public Vector3 Minimum { get; private set; }

    public Vector3 Maximum { get; private set; }

    public float GroundHeight { get; private set; }

    public float Scale => (Maximum - Minimum).Length();

    public float RayEpsilon => Scale * 1e-5f;

    private double animationTime;
    private int staticObjectCount;

    public Matrix4x4 GetSunViewProjection(Vector3 direction)
    {
        Vector3 center = (Minimum + Maximum) * 0.5f;
        Matrix4x4 view = Matrix4x4.CreateLookAt(center + direction * Scale, center, Vector3.UnitY);

        return view * Matrix4x4.CreateOrthographic(Scale, Scale, RayEpsilon, Scale * 2);
    }

    public void Update(double delta)
    {
        animationTime += delta;
        Vector3 center = (Minimum + Maximum) * 0.5f;

        for (int i = staticObjectCount; i < Objects.Length; i++)
        {
            float phase = (float)animationTime * 0.7f + (i - staticObjectCount) * MathF.PI;
            Vector3 p = new(center.X + MathF.Sin(phase) * Scale * 0.09f, GroundHeight + Scale * 0.04f, center.Z + MathF.Cos(phase) * Scale * 0.018f);
            Objects[i].Offset = new(p, Objects[i].Offset.W);
        }
    }

    public void CommitHistory()
    {
        for (int i = 0; i < Objects.Length; i++)
        {
            Objects[i].PreviousOffset = Objects[i].Offset;
        }
    }
}
