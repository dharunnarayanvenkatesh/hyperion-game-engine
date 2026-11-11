using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Graphics;

internal sealed class GpuMesh : IDisposable
{
    public GpuMesh(float[] vertices)
    {
        VertexCount = vertices.Length / 6;
        VertexArray = GL.GenVertexArray();
        VertexBuffer = GL.GenBuffer();

        GL.BindVertexArray(VertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, VertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);

        var stride = 6 * sizeof(float);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));

        GL.BindVertexArray(0);
    }

    public int VertexArray { get; }

    public int VertexBuffer { get; }

    public int VertexCount { get; }

    public void Draw()
    {
        GL.BindVertexArray(VertexArray);
        GL.DrawArrays(PrimitiveType.Triangles, 0, VertexCount);
    }

    public void Dispose()
    {
        GL.DeleteBuffer(VertexBuffer);
        GL.DeleteVertexArray(VertexArray);
    }
}
