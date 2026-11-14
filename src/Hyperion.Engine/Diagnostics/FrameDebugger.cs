using System.Text.Json;
using Hyperion.Engine.Graphics;
using Hyperion.Engine.Scene;

namespace Hyperion.Engine.Diagnostics;

internal sealed class FrameDebugger(string outputDirectory)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly List<FrameDrawCall> _drawCalls = [];
    private bool _captureRequested;
    private long _frameNumber;

    public bool IsCapturing => _captureRequested;

    public string? LastCapturePath { get; private set; }

    public void RequestCapture()
    {
        _captureRequested = true;
    }

    public void BeginFrame(long frameNumber)
    {
        _frameNumber = frameNumber;
        _drawCalls.Clear();
    }

    public void RecordDraw(Entity entity, MeshRenderer renderer, GpuMesh mesh)
    {
        if (!_captureRequested)
        {
            return;
        }

        var material = renderer.Material;
        _drawCalls.Add(new FrameDrawCall(
            _drawCalls.Count + 1,
            entity.Id,
            entity.Name,
            renderer.Asset?.Name ?? renderer.Primitive?.ToString() ?? "Unknown",
            mesh.VertexCount,
            mesh.IndexCount,
            mesh.TriangleCount,
            material.BaseColor.X,
            material.BaseColor.Y,
            material.BaseColor.Z,
            material.Metallic,
            material.Roughness,
            material.BaseColorTexture?.Name,
            material.MetallicRoughnessTexture?.Name));
    }

    public void EndFrame(DiagnosticsSnapshot snapshot, GraphicsDeviceInfo graphicsDevice)
    {
        if (!_captureRequested)
        {
            return;
        }

        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
        LastCapturePath = Path.Combine(outputDirectory, $"frame-{_frameNumber}-{timestamp}.json");
        var capture = new FrameCaptureReport(
            DateTimeOffset.UtcNow,
            _frameNumber,
            graphicsDevice,
            snapshot,
            _drawCalls.ToArray());
        File.WriteAllText(LastCapturePath, JsonSerializer.Serialize(capture, JsonOptions));
        _captureRequested = false;

        HyperionLog.Information(
            "frame_capture",
            "Captured a frame-debugger report.",
            new Dictionary<string, object?>
            {
                ["path"] = LastCapturePath,
                ["drawCalls"] = _drawCalls.Count,
                ["triangles"] = snapshot.Render.Triangles
            });
    }

    private sealed record FrameCaptureReport(
        DateTimeOffset CapturedAt,
        long FrameNumber,
        GraphicsDeviceInfo GraphicsDevice,
        DiagnosticsSnapshot Performance,
        IReadOnlyList<FrameDrawCall> DrawCalls);

    private sealed record FrameDrawCall(
        int Order,
        Guid EntityId,
        string EntityName,
        string Mesh,
        int Vertices,
        int Indices,
        int Triangles,
        float BaseColorRed,
        float BaseColorGreen,
        float BaseColorBlue,
        float Metallic,
        float Roughness,
        string? BaseColorTexture,
        string? MetallicRoughnessTexture);
}
