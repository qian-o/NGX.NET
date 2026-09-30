using System.Numerics;

namespace Showcase.Handlers;

internal sealed class Camera
{
    public Vector3 Position;

    public Vector3 PreviousPosition { get; private set; }

    public float Yaw;

    public float Pitch;

    public const float FieldOfView = MathF.PI / 3;

    public float Near { get; private set; }

    public float Far { get; private set; }

    public Matrix4x4 View { get; private set; }

    public Matrix4x4 Projection { get; private set; }

    public Matrix4x4 ViewProjection { get; private set; }

    public Matrix4x4 PreviousViewProjection { get; private set; }

    public Matrix4x4 JitteredViewProjection { get; private set; }

    public Vector2 Jitter { get; private set; }

    public Vector3 Forward => Vector3.Normalize(new(MathF.Cos(Pitch) * MathF.Cos(Yaw), MathF.Sin(Pitch), MathF.Cos(Pitch) * MathF.Sin(Yaw)));

    private float speed;

    public void Reset(Scene scene)
    {
        Vector3 center = (scene.Minimum + scene.Maximum) * 0.5f;
        Position = new(center.X - (scene.Maximum.X - scene.Minimum.X) * 0.3f, scene.GroundHeight + scene.Scale * 0.045f, center.Z);
        Yaw = 0;
        Pitch = 0.04f;
        Near = scene.Scale * 0.0005f;
        Far = scene.Scale * 3;
        speed = scene.Scale * 0.08f;
    }

    public void Move(Window window, float delta)
    {
        if (window.Looking)
        {
            Yaw += window.MouseDelta.X * 0.003f;
            Pitch = Math.Clamp(Pitch - window.MouseDelta.Y * 0.003f, -1.5f, 1.5f);
        }

        if (window.KeyboardCaptured)
        {
            return;
        }

        Vector3 right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        float distance = speed * delta * (window.Down(0x10) ? 3 : 1);

        if (window.Down('W'))
        {
            Position += Forward * distance;
        }

        if (window.Down('S'))
        {
            Position -= Forward * distance;
        }

        if (window.Down('D'))
        {
            Position += right * distance;
        }

        if (window.Down('A'))
        {
            Position -= right * distance;
        }

        if (window.Down('E'))
        {
            Position += Vector3.UnitY * distance;
        }

        if (window.Down('Q'))
        {
            Position -= Vector3.UnitY * distance;
        }
    }

    public void Update(int width, int height, int outputWidth, int outputHeight, uint frame, bool temporal, bool reset)
    {
        View = Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, (float)outputWidth / outputHeight, Near, Far);

        // Floating-point reverse Z maps near to 1 and far to 0. Forward Z loses
        // enough precision at the far wall to reconstruct ray origins inside it.
        projection.M33 = Near / (Far - Near);
        projection.M43 = Far * projection.M33;
        Projection = projection;
        ViewProjection = View * Projection;
        Jitter = temporal ? new(Halton(frame % 32 + 1, 2) - 0.5f, Halton(frame % 32 + 1, 3) - 0.5f) : Vector2.Zero;
        Matrix4x4 jittered = Projection;

        // RH projection: clip.w = -view.z. Positive jitter is right/down in pixel space.
        jittered.M31 -= Jitter.X * 2 / width;
        jittered.M32 += Jitter.Y * 2 / height;
        JitteredViewProjection = View * jittered;

        if (reset)
        {
            PreviousViewProjection = ViewProjection;
            PreviousPosition = Position;
        }
    }

    public void CommitHistory()
    {
        PreviousViewProjection = ViewProjection;
        PreviousPosition = Position;
    }

    private static float Halton(uint index, uint radix)
    {
        float fraction = 1;
        float result = 0;

        while (index != 0)
        {
            fraction /= radix;
            result += fraction * (index % radix);
            index /= radix;
        }

        return result;
    }
}
