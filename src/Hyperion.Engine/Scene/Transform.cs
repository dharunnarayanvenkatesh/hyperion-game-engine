using OpenTK.Mathematics;

namespace Hyperion.Engine.Scene;

public sealed class Transform
{
    private Vector3 _position = Vector3.Zero;
    private Vector3 _rotation = Vector3.Zero;
    private Vector3 _scale = Vector3.One;

    internal event Action? Changed;

    public Vector3 Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            Changed?.Invoke();
        }
    }

    public Vector3 Rotation
    {
        get => _rotation;
        set
        {
            if (_rotation == value) return;
            _rotation = value;
            Changed?.Invoke();
        }
    }

    public Vector3 Scale
    {
        get => _scale;
        set
        {
            if (_scale == value) return;
            _scale = value;
            Changed?.Invoke();
        }
    }

    public Matrix4 Matrix =>
        Matrix4.CreateScale(_scale)
        * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(_rotation.X))
        * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(_rotation.Y))
        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(_rotation.Z))
        * Matrix4.CreateTranslation(_position);
}
