namespace Hyperion.Engine.Graphics;

public sealed class TextureAsset
{
    internal TextureAsset(string name, int width, int height, byte[] rgbaPixels, bool isSrgb)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(rgbaPixels);
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Texture dimensions must be positive.");
        }

        if (rgbaPixels.Length != width * height * 4)
        {
            throw new ArgumentException("Texture data must contain exactly four bytes per pixel.", nameof(rgbaPixels));
        }

        Name = name;
        Width = width;
        Height = height;
        RgbaPixels = rgbaPixels;
        IsSrgb = isSrgb;
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    public bool IsSrgb { get; }

    internal byte[] RgbaPixels { get; }
}
