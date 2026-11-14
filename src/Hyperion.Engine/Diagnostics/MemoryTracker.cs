using System.Diagnostics;

namespace Hyperion.Engine.Diagnostics;

public readonly record struct MemorySnapshot(
    long ManagedHeapBytes,
    long WorkingSetBytes,
    long PrivateBytes,
    long TotalAllocatedBytes,
    long PeakManagedHeapBytes,
    long PeakWorkingSetBytes,
    int Gen0Collections,
    int Gen1Collections,
    int Gen2Collections);

internal sealed class MemoryTracker : IDisposable
{
    private readonly Process _process = Process.GetCurrentProcess();
    private long _peakManagedHeapBytes;
    private long _peakWorkingSetBytes;

    public MemorySnapshot Sample()
    {
        _process.Refresh();
        var managedHeapBytes = GC.GetTotalMemory(forceFullCollection: false);
        var workingSetBytes = _process.WorkingSet64;
        _peakManagedHeapBytes = Math.Max(_peakManagedHeapBytes, managedHeapBytes);
        _peakWorkingSetBytes = Math.Max(_peakWorkingSetBytes, workingSetBytes);

        return new MemorySnapshot(
            managedHeapBytes,
            workingSetBytes,
            _process.PrivateMemorySize64,
            GC.GetTotalAllocatedBytes(precise: false),
            _peakManagedHeapBytes,
            _peakWorkingSetBytes,
            GC.CollectionCount(0),
            GC.CollectionCount(1),
            GC.CollectionCount(2));
    }

    public void Dispose() => _process.Dispose();
}
