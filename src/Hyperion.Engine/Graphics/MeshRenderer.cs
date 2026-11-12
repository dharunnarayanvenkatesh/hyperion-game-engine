namespace Hyperion.Engine.Graphics;

public enum PrimitiveMesh
{
    Cube,
    Plane,
    Sphere
}

public sealed class MeshRenderer
{
    public MeshRenderer(PrimitiveMesh mesh, Material material)
    {
        Primitive = mesh;
        Material = material;
    }

    public MeshRenderer(MeshAsset mesh, Material material)
    {
        Asset = mesh ?? throw new ArgumentNullException(nameof(mesh));
        Material = material;
    }

    public MeshRenderer(MeshAsset mesh)
        : this(mesh, mesh?.DefaultMaterial ?? throw new ArgumentNullException(nameof(mesh)))
    {
    }

    public PrimitiveMesh? Primitive { get; }

    public MeshAsset? Asset { get; }

    public Material Material { get; set; }

    public BoundingSphere LocalBounds => Asset?.Bounds ?? Primitive switch
    {
        PrimitiveMesh.Cube => new BoundingSphere(OpenTK.Mathematics.Vector3.Zero, MathF.Sqrt(3f)),
        PrimitiveMesh.Plane => new BoundingSphere(OpenTK.Mathematics.Vector3.Zero, MathF.Sqrt(2f)),
        PrimitiveMesh.Sphere => new BoundingSphere(OpenTK.Mathematics.Vector3.Zero, 1f),
        _ => throw new InvalidOperationException("The mesh renderer has no mesh source.")
    };
}
