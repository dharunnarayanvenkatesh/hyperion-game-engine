using System.Globalization;

namespace Hyperion.Engine.Diagnostics;

public readonly record struct RenderFrameStatistics(
    int Candidates,
    int VisibleObjects,
    int CulledObjects,
    int DrawCalls,
    int ShadowDrawCalls,
    long Triangles)
{
    public int TotalDrawCalls => DrawCalls + ShadowDrawCalls;
}

public sealed record GraphicsDeviceInfo(
    string Vendor,
    string Renderer,
    string OpenGlVersion,
    string ShadingLanguageVersion);

public sealed record DiagnosticsSnapshot(
    long FrameNumber,
    double FramesPerSecond,
    CpuProfileSnapshot Cpu,
    double GpuMilliseconds,
    MemorySnapshot Memory,
    RenderFrameStatistics Render);

internal sealed class EngineDiagnostics : IDisposable
{
    private readonly CpuProfiler _cpuProfiler = new();
    private readonly GpuProfiler _gpuProfiler = new();
    private readonly MemoryTracker _memoryTracker = new();
    private readonly FrameDebugger _frameDebugger;
    private readonly GraphicsDeviceInfo _graphicsDevice;
    private MemorySnapshot _memory;
    private double _framesPerSecond;
    private double _memorySampleAccumulator;
    private double _titleRefreshAccumulator;
    private double _logAccumulator;

    public EngineDiagnostics(string outputDirectory, GraphicsDeviceInfo graphicsDevice)
    {
        OutputDirectory = outputDirectory;
        _graphicsDevice = graphicsDevice;
        _frameDebugger = new FrameDebugger(outputDirectory);
        _memory = _memoryTracker.Sample();
    }

    public string OutputDirectory { get; }

    public DiagnosticsSnapshot? CurrentSnapshot { get; private set; }

    public string? UpdatedWindowTitle { get; private set; }

    public FrameDebugger FrameDebugger => _frameDebugger;

    public void BeginFrame(long frameNumber)
    {
        _cpuProfiler.BeginFrame();
        _frameDebugger.BeginFrame(frameNumber);
    }

    public CpuProfiler.Scope MeasureCpu(string name) => _cpuProfiler.Measure(name);

    public void BeginGpuFrame() => _gpuProfiler.BeginFrame();

    public void EndGpuFrame() => _gpuProfiler.EndFrame();

    public void RequestFrameCapture()
    {
        _frameDebugger.RequestCapture();
        HyperionLog.Information("frame_capture_requested", "A frame-debugger capture was requested.");
    }

    public void EndFrame(long frameNumber, double deltaSeconds, RenderFrameStatistics renderStatistics)
    {
        var cpu = _cpuProfiler.EndFrame();
        var instantaneousFps = deltaSeconds > 0 ? 1.0 / deltaSeconds : 0;
        _framesPerSecond = _framesPerSecond == 0
            ? instantaneousFps
            : (_framesPerSecond * 0.9) + (instantaneousFps * 0.1);

        _memorySampleAccumulator += deltaSeconds;
        if (_memorySampleAccumulator >= 0.5)
        {
            _memory = _memoryTracker.Sample();
            _memorySampleAccumulator = 0;
        }

        CurrentSnapshot = new DiagnosticsSnapshot(
            frameNumber,
            _framesPerSecond,
            cpu,
            _gpuProfiler.LastFrameMilliseconds,
            _memory,
            renderStatistics);

        _frameDebugger.EndFrame(CurrentSnapshot, _graphicsDevice);

        _titleRefreshAccumulator += deltaSeconds;
        if (_titleRefreshAccumulator >= 0.25)
        {
            UpdatedWindowTitle = string.Create(
                CultureInfo.InvariantCulture,
                $"{_framesPerSecond:F0} FPS | CPU {cpu.FrameMilliseconds:F2} ms | GPU {_gpuProfiler.LastFrameMilliseconds:F2} ms | {_memory.WorkingSetBytes / 1048576.0:F0} MB | {renderStatistics.TotalDrawCalls} draws");
            _titleRefreshAccumulator = 0;
        }

        _logAccumulator += deltaSeconds;
        if (_logAccumulator >= 5)
        {
            HyperionLog.Information(
                "performance_sample",
                "Periodic engine performance sample.",
                new Dictionary<string, object?>
                {
                    ["frame"] = frameNumber,
                    ["fps"] = _framesPerSecond,
                    ["cpuMs"] = cpu.FrameMilliseconds,
                    ["gpuMs"] = _gpuProfiler.LastFrameMilliseconds,
                    ["workingSetBytes"] = _memory.WorkingSetBytes,
                    ["managedHeapBytes"] = _memory.ManagedHeapBytes,
                    ["drawCalls"] = renderStatistics.DrawCalls,
                    ["shadowDrawCalls"] = renderStatistics.ShadowDrawCalls,
                    ["triangles"] = renderStatistics.Triangles
                });
            _logAccumulator = 0;
        }
    }

    public void Dispose()
    {
        _gpuProfiler.Dispose();
        _memoryTracker.Dispose();
    }
}
