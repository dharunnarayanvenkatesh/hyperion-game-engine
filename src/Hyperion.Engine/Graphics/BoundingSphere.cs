using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

public readonly record struct BoundingSphere(Vector3 Center, float Radius)
{
    public BoundingSphere Transform(Matrix4 matrix, Vector3 scale)
    {
        var worldCenter = new Vector4(Center, 1f) * matrix;
        var maximumScale = MathF.Max(MathF.Abs(scale.X), MathF.Max(MathF.Abs(scale.Y), MathF.Abs(scale.Z)));
        return new BoundingSphere(worldCenter.Xyz, Radius * maximumScale);
    }
}
