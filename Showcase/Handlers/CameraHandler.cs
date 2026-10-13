using System.Numerics;
using Silk.NET.Input;

namespace Showcase.Handlers;

internal class CameraHandler
{
    public const float FieldOfView = MathF.PI / 3.0f;

    public Vector3 Position;

    public float Yaw;

    public float Pitch;

    private float speed;

    public float Near { get; private set; }

    public float Far { get; private set; }

    public Matrix4x4 View { get; private set; }

    public Matrix4x4 Projection { get; private set; }

    public Matrix4x4 ViewProjection { get; private set; }

    public Matrix4x4 PreviousViewProjection { get; private set; }

    public Matrix4x4 JitteredViewProjection { get; private set; }

    public Vector2 Jitter { get; private set; }

    public Vector3 Forward => Vector3.Normalize(new(MathF.Cos(Pitch) * MathF.Cos(Yaw), MathF.Sin(Pitch), MathF.Cos(Pitch) * MathF.Sin(Yaw)));

    public void Reset(Scene scene)
    {
        Vector3 center = (scene.Minimum + scene.Maximum) * 0.5f;
        Position = new(center.X - ((scene.Maximum.X - scene.Minimum.X) * 0.3f), scene.GroundHeight + (scene.Scale * 0.045f), center.Z);
        Yaw = 0.0f;
        Pitch = 0.04f;
        Near = scene.Scale * 0.0005f;
        Far = scene.Scale * 3.0f;
        speed = scene.Scale * 0.08f;
    }

    public void Move(InputHandler input, float deltaSeconds)
    {
        if (input.IsLooking)
        {
            Yaw += input.MouseDelta.X * 0.003f;
            Pitch = Math.Clamp(Pitch - (input.MouseDelta.Y * 0.003f), -1.5f, 1.5f);
        }

        if (InputHandler.IsKeyboardCaptured)
        {
            return;
        }

        Vector3 right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
        float distance = speed * deltaSeconds * ((input.IsKeyDown(Key.ShiftLeft) || input.IsKeyDown(Key.ShiftRight)) ? 3 : 1);
        if (input.IsKeyDown(Key.W))
        {
            Position += Forward * distance;
        }

        if (input.IsKeyDown(Key.S))
        {
            Position -= Forward * distance;
        }

        if (input.IsKeyDown(Key.D))
        {
            Position += right * distance;
        }

        if (input.IsKeyDown(Key.A))
        {
            Position -= right * distance;
        }

        if (input.IsKeyDown(Key.E))
        {
            Position += Vector3.UnitY * distance;
        }

        if (input.IsKeyDown(Key.Q))
        {
            Position -= Vector3.UnitY * distance;
        }
    }

    public void Update(int width, int height, int outputWidth, int outputHeight, uint frame, bool temporal, bool reset)
    {
        View = Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitY);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, (float)outputWidth / outputHeight, Near, Far);

        // Floating-point reverse Z maps near to 1 and far to 0. Forward Z loses enough precision at the far wall to reconstruct ray origins inside it.
        projection.M33 = Near / (Far - Near);
        projection.M43 = Far * projection.M33;
        Projection = projection;
        ViewProjection = View * Projection;
        Jitter = temporal ? new(Halton((frame % 32u) + 1u, 2u) - 0.5f, Halton((frame % 32u) + 1u, 3u) - 0.5f) : Vector2.Zero;
        Matrix4x4 jittered = Projection;

        // RH projection: clip.w = -view.z. Positive jitter is right/down in pixel space.
        jittered.M31 -= Jitter.X * 2.0f / width;
        jittered.M32 += Jitter.Y * 2.0f / height;
        JitteredViewProjection = View * jittered;

        if (reset)
        {
            PreviousViewProjection = ViewProjection;
        }
    }

    public void CommitHistory()
    {
        PreviousViewProjection = ViewProjection;
    }

    private static float Halton(uint index, uint radix)
    {
        float fraction = 1.0f;
        float result = 0.0f;
        while (index is not 0u)
        {
            fraction /= radix;
            result += fraction * (index % radix);
            index /= radix;
        }

        return result;
    }
}
