using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Hyperion.Engine.Diagnostics;

internal static class CrashReporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private static readonly Lock Sync = new();
    private static string? _outputDirectory;
    private static Func<DiagnosticsSnapshot?>? _snapshotProvider;
    private static bool _installed;

    public static void Install(string outputDirectory, Func<DiagnosticsSnapshot?> snapshotProvider)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputDirectory);
        ArgumentNullException.ThrowIfNull(snapshotProvider);

        lock (Sync)
        {
            _outputDirectory = outputDirectory;
            _snapshotProvider = snapshotProvider;
            if (_installed)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
            _installed = true;
        }
    }

    public static string WriteReport(Exception exception, string origin, bool isTerminating)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(origin);

        lock (Sync)
        {
            var outputDirectory = _outputDirectory
                ?? Path.Combine(AppContext.BaseDirectory, "Diagnostics");
            Directory.CreateDirectory(outputDirectory);
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss-fff", System.Globalization.CultureInfo.InvariantCulture);
            var path = Path.Combine(outputDirectory, $"crash-{timestamp}-{Environment.ProcessId}.json");
            using var process = Process.GetCurrentProcess();
            var report = new
            {
                CapturedAt = DateTimeOffset.UtcNow,
                Origin = origin,
                IsTerminating = isTerminating,
                Process = new
                {
                    process.ProcessName,
                    process.Id,
                    process.WorkingSet64,
                    process.PrivateMemorySize64,
                    Uptime = DateTimeOffset.Now - process.StartTime
                },
                Runtime = new
                {
                    Framework = RuntimeInformation.FrameworkDescription,
                    OperatingSystem = RuntimeInformation.OSDescription,
                    Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
                    Environment.ProcessorCount,
                    CommandLine = Environment.CommandLine
                },
                Exception = DescribeException(exception),
                Diagnostics = _snapshotProvider?.Invoke(),
                RecentLogs = HyperionLog.GetTail()
            };

            File.WriteAllText(path, JsonSerializer.Serialize(report, JsonOptions));
            return path;
        }
    }

    private static object DescribeException(Exception exception) => new
    {
        Type = exception.GetType().FullName,
        exception.Message,
        exception.StackTrace,
        Source = exception.Source,
        InnerException = exception.InnerException is null ? null : DescribeException(exception.InnerException)
    };

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs)
    {
        var exception = eventArgs.ExceptionObject as Exception
            ?? new InvalidOperationException($"Unhandled non-exception object: {eventArgs.ExceptionObject}");
        var path = WriteReport(exception, "AppDomain.UnhandledException", eventArgs.IsTerminating);
        HyperionLog.Error("crash_report", "An unhandled exception was captured.", new Dictionary<string, object?> { ["path"] = path });
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs)
    {
        var path = WriteReport(eventArgs.Exception, "TaskScheduler.UnobservedTaskException", isTerminating: false);
        HyperionLog.Error("task_exception", "An unobserved task exception was captured.", new Dictionary<string, object?> { ["path"] = path });
    }
}
