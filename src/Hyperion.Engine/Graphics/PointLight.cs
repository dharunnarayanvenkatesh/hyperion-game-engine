using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

public sealed class PointLight
{
    public PointLight(Vector3 position, Vector3 color, float intensity)
    {
        Position = position;
        Color = color;
        Intensity = intensity;
    }

    public Vector3 Position { get; set; }

    public Vector3 Color { get; set; }

    public float Intensity { get; set; }
}
