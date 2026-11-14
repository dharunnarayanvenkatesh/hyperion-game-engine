using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

public sealed class DirectionalLight
{
    public DirectionalLight(Vector3 direction, Vector3 color, float intensity, float shadowExtent)
    {
        if (direction.LengthSquared < 0.000001f)
        {
            throw new ArgumentException("A directional light requires a non-zero direction.", nameof(direction));
        }

        Direction = Vector3.Normalize(direction);
        Color = color;
        Intensity = intensity;
        ShadowExtent = shadowExtent;
    }

    public Vector3 Direction { get; set; }

    public Vector3 Color { get; set; }

    public float Intensity { get; set; }

    public Vector3 ShadowCenter { get; set; } = Vector3.Zero;

    public float ShadowExtent { get; set; }
}
