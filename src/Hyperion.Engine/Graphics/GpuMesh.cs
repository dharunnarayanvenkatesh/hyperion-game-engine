using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Graphics;

internal sealed class GpuMesh : IDisposable
{
    private readonly bool _usesIndices;

    public GpuMesh(MeshData data)
    {
        VertexCount = data.Vertices.Length / 6;
        IndexCount = data.Indices.Length;
        _usesIndices = IndexCount > 0;
        VertexArray = GL.GenVertexArray();
        VertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(VertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        GL.BufferData(
            BufferTarget.ArrayBuffer,
            data.Vertices.Length * sizeof(float),
            data.Vertices,
            BufferUsageHint.StaticDraw);

        if (_usesIndices)
        {
            IndexBuffer = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, IndexBuffer);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                data.Indices.Length * sizeof(uint),
                data.Indices,
                BufferUsageHint.StaticDraw);
        }

        var stride = 6 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

        GL.BindVertexArray(0);
    }

    public int VertexArray { get; }

    public int VertexBuffer { get; }

    public int IndexBuffer { get; }

    public int VertexCount { get; }

    public int IndexCount { get; }

    public void Draw()
    {
        GL.BindVertexArray(VertexArray);
        if (_usesIndices)
        {
            GL.DrawElements(PrimitiveType.Triangles, IndexCount, DrawElementsType.UnsignedInt, 0);
        }
        else
        {
            GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);
        }
    }

    public void Dispose()
    {
        GL.DeleteBuffer(VertexBuffer);
        if (IndexBuffer != 0)
        {
            GL.DeleteBuffer(IndexBuffer);
        }

        GL.DeleteVertexArray(VertexArray);
    }
}
