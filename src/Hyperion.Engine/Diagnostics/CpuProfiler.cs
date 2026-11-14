using System.Diagnostics;

namespace Hyperion.Engine.Diagnostics;

public sealed record CpuProfileSnapshot(
    double FrameMilliseconds,
    IReadOnlyDictionary<string, double> ScopeMilliseconds);

internal sealed class CpuProfiler
{
    private readonly Dictionary<string, long> _scopeTicks = new(StringComparer.Ordinal);
    private long _frameStart;

    public void BeginFrame()
    {
        _scopeTicks.Clear();
        _frameStart = Stopwatch.GetTimestamp();
    }

    public Scope Measure(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Scope(this, name, Stopwatch.GetTimestamp());
    }

    public CpuProfileSnapshot EndFrame()
    {
        var elapsed = Stopwatch.GetElapsedTime(_frameStart).TotalMilliseconds;
        var timings = _scopeTicks.ToDictionary(
            pair => pair.Key,
            pair => pair.Value * 1000.0 / Stopwatch.Frequency,
            StringComparer.Ordinal);
        return new CpuProfileSnapshot(elapsed, timings);
    }

    private void EndScope(string name, long start)
    {
        var elapsed = Stopwatch.GetTimestamp() - start;
        _scopeTicks[name] = _scopeTicks.GetValueOrDefault(name) + elapsed;
    }

    internal readonly struct Scope(CpuProfiler owner, string name, long start) : IDisposable
    {
        public void Dispose() => owner.EndScope(name, start);
    }
}
