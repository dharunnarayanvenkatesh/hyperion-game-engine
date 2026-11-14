using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Graphics;

internal sealed class GpuTexture : IDisposable
{
    public GpuTexture(TextureAsset asset)
    {
        Handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, Handle);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);

        var internalFormat = asset.IsSrgb
            ? PixelInternalFormat.Srgb8Alpha8
            : PixelInternalFormat.Rgba8;

        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            internalFormat,
            asset.Width,
            asset.Height,
            0,
            PixelFormat.Rgba,
            PixelType.UnsignedByte,
            asset.RgbaPixels);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public int Handle { get; }

    public void Bind(int textureUnit)
    {
        GL.ActiveTexture((TextureUnit)((int)TextureUnit.Texture0 + textureUnit));
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Dispose() => GL.DeleteTexture(Handle);
}
