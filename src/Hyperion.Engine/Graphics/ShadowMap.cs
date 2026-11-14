using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Graphics;

internal sealed class ShadowMap : IDisposable
{
    public ShadowMap(int resolution)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(resolution);
        Resolution = resolution;

        Texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Texture);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            level: 0,
            PixelInternalFormat.DepthComponent24,
            resolution,
            resolution,
            border: 0,
            PixelFormat.DepthComponent,
            PixelType.Float,
            IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBorderColor, [1f, 1f, 1f, 1f]);

        Framebuffer = GL.GenFramebuffer();
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        GL.FramebufferTexture2D(
            FramebufferTarget.Framebuffer,
            FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D,
            Texture,
            level: 0);
        GL.DrawBuffer(DrawBufferMode.None);
        GL.ReadBuffer(ReadBufferMode.None);

        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        if (status != FramebufferErrorCode.FramebufferComplete)
        {
            Dispose();
            throw new InvalidOperationException($"Shadow framebuffer is incomplete: {status}.");
        }
    }

    public int Resolution { get; }

    public int Framebuffer { get; }

    public int Texture { get; }

    public void BeginWrite()
    {
        GL.Viewport(0, 0, Resolution, Resolution);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, Framebuffer);
        GL.Clear(ClearBufferMask.DepthBufferBit);
    }

    public void Bind(int textureUnit)
    {
        GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
        GL.BindTexture(TextureTarget.Texture2D, Texture);
    }

    public void Dispose()
    {
        GL.DeleteFramebuffer(Framebuffer);
        GL.DeleteTexture(Texture);
    }
}
