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
        FieldOfViewRadians,
        AspectRatio,
        NearPlane,
        FarPlane);

    public float NearPlane { get; set; } = 0.05f;

    public float FarPlane { get; set; } = 500f;

    public float FieldOfViewRadians { get; set; } = MathHelper.DegreesToRadians(60f);

    public bool IsVisible(BoundingSphere bounds)
    {
        var relative = bounds.Center - Position;
        var forwardDistance = Vector3.Dot(relative, Front);
        if (forwardDistance + bounds.Radius < NearPlane || forwardDistance - bounds.Radius > FarPlane)
        {
            return false;
        }

        var right = Vector3.Normalize(Vector3.Cross(Front, Vector3.UnitY));
        var up = Vector3.Normalize(Vector3.Cross(right, Front));
        var halfVertical = MathF.Max(forwardDistance, 0f) * MathF.Tan(FieldOfViewRadians * 0.5f);
        var halfHorizontal = halfVertical * AspectRatio;
        var horizontalDistance = MathF.Abs(Vector3.Dot(relative, right));
        var verticalDistance = MathF.Abs(Vector3.Dot(relative, up));

        return horizontalDistance <= halfHorizontal + bounds.Radius
            && verticalDistance <= halfVertical + bounds.Radius;
    }

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

    public void SetPose(Vector3 position, Vector3 target)
    {
        var direction = target - position;
        if (direction.LengthSquared < 0.000001f)
        {
            throw new ArgumentException("The camera target must differ from its position.", nameof(target));
        }

        Position = position;
        direction = Vector3.Normalize(direction);
        _pitch = MathHelper.RadiansToDegrees(MathF.Asin(direction.Y));
        _yaw = MathHelper.RadiansToDegrees(MathF.Atan2(direction.Z, direction.X));
        UpdateVectors();
        ResetMouseDelta();
    }

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
