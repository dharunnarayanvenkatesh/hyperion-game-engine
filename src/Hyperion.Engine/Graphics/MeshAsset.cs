namespace Hyperion.Engine.Graphics;

public sealed class MeshAsset
{
    internal MeshAsset(string name, MeshData data, Material defaultMaterial)
    {
        Name = name;
        Data = data;
        DefaultMaterial = defaultMaterial;
    }

    public string Name { get; }

    public BoundingSphere Bounds => Data.Bounds;

    public Material DefaultMaterial { get; }

    internal MeshData Data { get; }
}
