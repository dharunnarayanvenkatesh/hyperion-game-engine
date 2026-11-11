namespace Hyperion.Engine.Graphics;

public enum PrimitiveMesh
{
    Cube,
    Plane
}

public sealed record MeshRenderer(PrimitiveMesh Mesh, Material Material);
