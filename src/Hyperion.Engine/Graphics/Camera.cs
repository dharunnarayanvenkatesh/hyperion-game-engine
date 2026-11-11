using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Hyperion.Engine.Graphics;

public sealed class Camera
{
    private const float MovementSpeed = 4.5f;
    private const float MouseSensitivity = 0.12f;
    private float _yaw = -90f;
    private float _pitch = -8f;
    private bool _skipMouseDelta = true;

    public Camera(Vector3 position, float aspectRatio)
    {
        Position = position;
        AspectRatio = aspectRatio;
        UpdateVectors();
    }

    public Vector3 Position { get; private set; }

    public float AspectRatio { get; set; }

    public Vector3 Front { get; private set; }

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + Front, Vector3.UnitY);

    public Matrix4 ProjectionMatrix => Matrix4.CreatePerspectiveFieldOfView(
        MathHelper.DegreesToRadians(60f),
        AspectRatio,
        0.05f,
        500f);

    public void Update(KeyboardState keyboard, MouseState mouse, double deltaSeconds)
    {
        var delta = (float)deltaSeconds;
        var right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        var velocity = MovementSpeed * delta;

        if (keyboard.IsKeyDown(Keys.W)) Position += Front * velocity;
        if (keyboard.IsKeyDown(Keys.S)) Position -= Front * velocity;
        if (keyboard.IsKeyDown(Keys.A)) Position -= right * velocity;
        if (keyboard.IsKeyDown(Keys.D)) Position += right * velocity;
        if (keyboard.IsKeyDown(Keys.Space)) Position += Vector3.UnitY * velocity;
        if (keyboard.IsKeyDown(Keys.LeftShift)) Position -= Vector3.UnitY * velocity;

        if (!mouse.IsButtonDown(MouseButton.Right))
        {
            _skipMouseDelta = true;
            return;
        }

        if (_skipMouseDelta)
        {
            _skipMouseDelta = false;
            return;
        }

        // Cursor grabbing and focus changes can report a synthetic jump from the
        // old pointer position to the window center. It must not rotate the camera.
        if (MathF.Abs(mouse.Delta.X) > 64f || MathF.Abs(mouse.Delta.Y) > 64f)
        {
            return;
        }

        _yaw += mouse.Delta.X * MouseSensitivity;
        _pitch -= mouse.Delta.Y * MouseSensitivity;
        _pitch = MathHelper.Clamp(_pitch, -89f, 89f);
        UpdateVectors();
    }

    public void ResetMouseDelta() => _skipMouseDelta = true;

    private void UpdateVectors()
    {
        var yaw = MathHelper.DegreesToRadians(_yaw);
        var pitch = MathHelper.DegreesToRadians(_pitch);
        Front = Vector3.Normalize(new Vector3(
            MathF.Cos(pitch) * MathF.Cos(yaw),
            MathF.Sin(pitch),
            MathF.Cos(pitch) * MathF.Sin(yaw)));
    }
}
