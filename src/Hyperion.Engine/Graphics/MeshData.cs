using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

public sealed class MeshData
{
    public MeshData(float[] vertices, uint[]? indices = null, int vertexStride = 6)
    {
        ArgumentNullException.ThrowIfNull(vertices);
        if (vertexStride < 6 || vertices.Length == 0 || vertices.Length % vertexStride != 0)
        {
            throw new ArgumentException("Vertices must contain interleaved position and normal values.", nameof(vertices));
        }

        Vertices = vertices;
        Indices = indices ?? [];
        VertexStride = vertexStride;
        Bounds = CalculateBounds(vertices, vertexStride);
    }

    public float[] Vertices { get; }

    public uint[] Indices { get; }

    public int VertexStride { get; }

    public BoundingSphere Bounds { get; }

    private static BoundingSphere CalculateBounds(float[] vertices, int vertexStride)
    {
        var minimum = new Vector3(float.MaxValue);
        var maximum = new Vector3(float.MinValue);

        for (var index = 0; index < vertices.Length; index += vertexStride)
        {
            var position = new Vector3(vertices[index], vertices[index + 1], vertices[index + 2]);
            minimum = Vector3.ComponentMin(minimum, position);
            maximum = Vector3.ComponentMax(maximum, position);
        }

        var center = (minimum + maximum) * 0.5f;
        var radiusSquared = 0f;
        for (var index = 0; index < vertices.Length; index += vertexStride)
        {
            var position = new Vector3(vertices[index], vertices[index + 1], vertices[index + 2]);
            radiusSquared = MathF.Max(radiusSquared, (position - center).LengthSquared);
        }

        return new BoundingSphere(center, MathF.Sqrt(radiusSquared));
    }
}
