using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using OpenTK.Graphics.OpenGL4;

namespace Hyperion.Engine.Graphics;

internal static class FramebufferCapture
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void SavePng(string path, int width, int height)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);

        var pixels = new byte[checked(width * height * 4)];
        GL.PixelStore(PixelStoreParameter.PackAlignment, 1);
        GL.Finish();
        GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

        var stride = width * 4;
        var scanlines = new byte[checked(height * (stride + 1))];
        for (var row = 0; row < height; row++)
        {
            var destination = row * (stride + 1);
            scanlines[destination] = 0;
            System.Buffer.BlockCopy(pixels, (height - row - 1) * stride, scanlines, destination + 1, stride);
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            zlib.Write(scanlines);
        }

        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        using var output = File.Create(fullPath);
        output.Write(PngSignature);

        Span<byte> header = stackalloc byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header[..4], width);
        BinaryPrimitives.WriteInt32BigEndian(header.Slice(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(output, "IHDR", header);
        WriteChunk(output, "IDAT", compressed.ToArray());
        WriteChunk(output, "IEND", ReadOnlySpan<byte>.Empty);
    }

    private static void WriteChunk(Stream output, string type, ReadOnlySpan<byte> data)
    {
        Span<byte> integer = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(integer, data.Length);
        output.Write(integer);

        Span<byte> typeBytes = stackalloc byte[4];
        Encoding.ASCII.GetBytes(type, typeBytes);
        output.Write(typeBytes);
        output.Write(data);

        var crc = ComputeCrc32(typeBytes, data);
        BinaryPrimitives.WriteUInt32BigEndian(integer, crc);
        output.Write(integer);
    }

    private static uint ComputeCrc32(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var crc = uint.MaxValue;
        UpdateCrc(ref crc, type);
        UpdateCrc(ref crc, data);
        return ~crc;
    }

    private static void UpdateCrc(ref uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xedb88320u : crc >> 1;
            }
        }
    }
}
