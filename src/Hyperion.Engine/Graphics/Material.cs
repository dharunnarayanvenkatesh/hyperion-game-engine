using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

public readonly record struct Material(
    Vector3 BaseColor,
    float Metallic = 0f,
    float Roughness = 0.5f,
    TextureAsset? BaseColorTexture = null,
    TextureAsset? MetallicRoughnessTexture = null)
{
    public static Material Default => new(new Vector3(0.8f), 0f, 0.5f);
}
