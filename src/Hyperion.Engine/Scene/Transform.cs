using OpenTK.Mathematics;

namespace Hyperion.Engine.Scene;

public sealed class Transform
{
    public Vector3 Position { get; set; } = Vector3.Zero;

    public Vector3 Rotation { get; set; } = Vector3.Zero;

    public Vector3 Scale { get; set; } = Vector3.One;

    public Matrix4 Matrix =>
        Matrix4.CreateScale(Scale)
        * Matrix4.CreateRotationX(MathHelper.DegreesToRadians(Rotation.X))
        * Matrix4.CreateRotationY(MathHelper.DegreesToRadians(Rotation.Y))
        * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation.Z))
        * Matrix4.CreateTranslation(Position);
}
