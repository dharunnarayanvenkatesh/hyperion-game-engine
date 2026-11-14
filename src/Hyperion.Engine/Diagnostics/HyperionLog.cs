using System.Text;
using System.Text.Json;

namespace Hyperion.Engine.Diagnostics;

public enum LogSeverity
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

public static class HyperionLog
{
    private const int TailCapacity = 128;
    private static readonly Lock Sync = new();
    private static readonly Queue<StructuredLogEvent> Tail = new(TailCapacity);
    private static StreamWriter? _writer;

    public static string? SessionLogPath { get; private set; }

    public static void Initialize(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);

        lock (Sync)
        {
            if (_writer is not null)
            {
                return;
            }

            Directory.CreateDirectory(directory);
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
            SessionLogPath = Path.Combine(directory, $"hyperion-{timestamp}-{Environment.ProcessId}.jsonl");
            _writer = new StreamWriter(
                new FileStream(SessionLogPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read),
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: false))
            {
                AutoFlush = true
            };
        }
    }

    public static void Write(
        LogSeverity severity,
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(message);

        var logEvent = new StructuredLogEvent(
            DateTimeOffset.UtcNow,
            severity.ToString(),
            eventName,
            message,
            Environment.CurrentManagedThreadId,
            properties);

        lock (Sync)
        {
            if (Tail.Count == TailCapacity)
            {
                Tail.Dequeue();
            }

            Tail.Enqueue(logEvent);
            _writer?.WriteLine(JsonSerializer.Serialize(logEvent));
        }

        Console.WriteLine($"[{logEvent.Timestamp:HH:mm:ss.fff}] [{severity}] {eventName}: {message}");
    }

    public static void Information(
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(LogSeverity.Information, eventName, message, properties);

    public static void Warning(
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(LogSeverity.Warning, eventName, message, properties);

    public static void Error(
        string eventName,
        string message,
        IReadOnlyDictionary<string, object?>? properties = null) =>
        Write(LogSeverity.Error, eventName, message, properties);

    internal static StructuredLogEvent[] GetTail()
    {
        lock (Sync)
        {
            return [.. Tail];
        }
    }

    internal static void Shutdown()
    {
        lock (Sync)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    internal sealed record StructuredLogEvent(
        DateTimeOffset Timestamp,
        string Severity,
        string EventName,
        string Message,
        int ManagedThreadId,
        IReadOnlyDictionary<string, object?>? Properties);
}
