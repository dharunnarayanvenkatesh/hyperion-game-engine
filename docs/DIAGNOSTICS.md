# Hyperion diagnostics

Hyperion initializes its diagnostics stack with the graphics context and keeps
the stack active until the engine shuts down. Runtime artifacts are written to
`Diagnostics` beside the executable by default.

Set `HYPERION_DIAGNOSTICS_PATH` to place artifacts somewhere else:

```powershell
$env:HYPERION_DIAGNOSTICS_PATH = "C:\captures\hyperion"
dotnet run --project src/Hyperion.Sandbox
```

## Live profiler

The window title displays a low-overhead summary containing smoothed FPS, CPU
frame time, asynchronous GPU frame time, process working-set memory, and draw
calls. CPU scopes currently separate update and render work. The GPU profiler
uses OpenGL timer queries and reads completed queries later instead of stalling
the current frame.

Every five seconds the same counters are appended to the structured session
log. Memory samples include:

- managed heap and total managed allocations;
- process working set and private bytes;
- peak managed heap and working set;
- generation 0, 1, and 2 collection counts.

## Frame debugger

Press `F9` to capture the next rendered frame. The resulting
`frame-<number>-<timestamp>.json` records:

- graphics device and OpenGL versions;
- CPU, GPU, memory, visibility, draw-call, and triangle statistics;
- ordered draw submissions;
- entity and mesh identity;
- vertex, index, and triangle counts;
- PBR factors and bound material textures.

For automated captures, set `HYPERION_CAPTURE_FRAME=1`. Hyperion captures a
stabilized frame after the first 30 updates.

## Structured logging

`HyperionLog` writes human-readable console messages and newline-delimited JSON
to `hyperion-<timestamp>-<process>.jsonl`. Each event contains a UTC timestamp,
severity, event name, message, managed thread ID, and optional structured
properties.

Game code can submit events without taking a dependency on a logging package:

```csharp
HyperionLog.Information(
    "level_loaded",
    "Loaded the laboratory level.",
    new Dictionary<string, object?>
    {
        ["entities"] = 128,
        ["elapsedMs"] = 42.6
    });
```

## Crash reports

Hyperion listens for unhandled application-domain exceptions and unobserved
task exceptions. Reports are saved as `crash-<timestamp>-<process>.json` and
contain:

- the complete nested exception chain;
- process memory and uptime;
- operating system, runtime, architecture, command line, and CPU count;
- the most recent diagnostics snapshot;
- the last 128 structured log events.

Set `HYPERION_TEST_CRASH_REPORT=1` to write a harmless self-test report during
startup. This validates report creation without crashing the process.

These systems form the first diagnostics layer. Future editor tooling can read
the same JSON artifacts to provide timelines, searchable logs, draw-call
inspection, and historical memory graphs without changing the runtime format.
