using Hyperion.Engine.Assets;
using Hyperion.Engine.Diagnostics;
using Hyperion.Engine.Graphics;
using Hyperion.Engine.Scene;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Hyperion.Engine.Core;

public abstract class EngineApplication : GameWindow
{
    private Renderer? _renderer;
    private EngineDiagnostics? _diagnostics;
    private readonly string _baseTitle;
    private double _elapsedSeconds;
    private long _frameNumber;
    private int _renderedFrames;
    private bool _screenshotCaptured;

    protected EngineApplication(string title, int width = 1280, int height = 720)
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                ClientSize = new Vector2i(width, height),
                Title = title,
                API = ContextAPI.OpenGL,
                APIVersion = new Version(3, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible,
                NumberOfSamples = 4
            })
    {
        _baseTitle = title;
        Camera = new Camera(new Vector3(0f, 1.25f, 6f), width / (float)height);
    }

    protected World World { get; } = new();

    protected AssetManager Assets { get; } = new();

    protected Camera Camera { get; }

    protected string StatusText { get; set; } = string.Empty;

    protected virtual bool UseDefaultCameraControls => true;

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.ClearColor(0.006f, 0.009f, 0.018f, 1f);

        var diagnosticsDirectory = Environment.GetEnvironmentVariable("HYPERION_DIAGNOSTICS_PATH");
        if (string.IsNullOrWhiteSpace(diagnosticsDirectory))
        {
            diagnosticsDirectory = Path.Combine(AppContext.BaseDirectory, "Diagnostics");
        }

        HyperionLog.Initialize(diagnosticsDirectory);
        var graphicsDevice = new GraphicsDeviceInfo(
            GL.GetString(StringName.Vendor) ?? "Unknown",
            GL.GetString(StringName.Renderer) ?? "Unknown",
            GL.GetString(StringName.Version) ?? "Unknown",
            GL.GetString(StringName.ShadingLanguageVersion) ?? "Unknown");
        _diagnostics = new EngineDiagnostics(diagnosticsDirectory, graphicsDevice);
        CrashReporter.Install(diagnosticsDirectory, () => _diagnostics?.CurrentSnapshot);

        HyperionLog.Information(
            "engine_start",
            "Hyperion initialized its diagnostics services.",
            new Dictionary<string, object?>
            {
                ["graphicsVendor"] = graphicsDevice.Vendor,
                ["graphicsRenderer"] = graphicsDevice.Renderer,
                ["openGlVersion"] = graphicsDevice.OpenGlVersion,
                ["diagnosticsDirectory"] = diagnosticsDirectory
            });

        if (string.Equals(
                Environment.GetEnvironmentVariable("HYPERION_TEST_CRASH_REPORT"),
                "1",
                StringComparison.Ordinal))
        {
            var reportPath = CrashReporter.WriteReport(
                new InvalidOperationException("Hyperion diagnostics self-test."),
                "DiagnosticsSelfTest",
                isTerminating: false);
            HyperionLog.Information(
                "crash_report_self_test",
                "Wrote a non-fatal crash-report self-test.",
                new Dictionary<string, object?> { ["path"] = reportPath });
        }

        _renderer = new Renderer();
        CursorState = CursorState.Normal;
        Camera.ResetMouseDelta();
        OnStart();
        HyperionLog.Information("scene_ready", "The initial scene finished loading.");
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _frameNumber++;
        _diagnostics?.BeginFrame(_frameNumber);

        if (_diagnostics is not null && KeyboardState.IsKeyPressed(Keys.F9))
        {
            _diagnostics.RequestFrameCapture();
        }

        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        if (_diagnostics is not null)
        {
            using var updateScope = _diagnostics.MeasureCpu("Update");
            UpdateGame(args.Time);
        }
        else
        {
            UpdateGame(args.Time);
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        var renderStatistics = default(RenderFrameStatistics);

        if (_diagnostics is not null
            && _frameNumber >= 30
            && string.Equals(
                Environment.GetEnvironmentVariable("HYPERION_CAPTURE_FRAME"),
                "1",
                StringComparison.Ordinal)
            && !_diagnostics.FrameDebugger.IsCapturing
            && _diagnostics.FrameDebugger.LastCapturePath is null)
        {
            _diagnostics.RequestFrameCapture();
        }

        if (_diagnostics is not null && _renderer is not null)
        {
            using (var renderScope = _diagnostics.MeasureCpu("Render"))
            {
                _diagnostics.BeginGpuFrame();
                try
                {
                    GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                    renderStatistics = _renderer.Render(
                        World,
                        Camera,
                        _diagnostics.FrameDebugger,
                        ClientSize.X,
                        ClientSize.Y);
                }
                finally
                {
                    _diagnostics.EndGpuFrame();
                }
            }
        }
        else
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        CaptureRequestedScreenshot();
        SwapBuffers();

        if (_diagnostics is not null)
        {
            _diagnostics.EndFrame(_frameNumber, args.Time, renderStatistics);
            if (_diagnostics.UpdatedWindowTitle is not null)
            {
                var gameStatus = string.IsNullOrWhiteSpace(StatusText) ? string.Empty : $" | {StatusText}";
                Title = $"{_baseTitle}{gameStatus} | {_diagnostics.UpdatedWindowTitle}";
            }
        }
    }

    private void UpdateGame(double deltaSeconds)
    {
        _elapsedSeconds += deltaSeconds;
        var gameTime = new GameTime(_elapsedSeconds, deltaSeconds);
        if (UseDefaultCameraControls)
        {
            Camera.Update(KeyboardState, MouseState, deltaSeconds);
        }

        OnTick(gameTime);
    }

    private void CaptureRequestedScreenshot()
    {
        if (_screenshotCaptured)
        {
            return;
        }

        var screenshotPath = Environment.GetEnvironmentVariable("HYPERION_SCREENSHOT_PATH");
        if (string.IsNullOrWhiteSpace(screenshotPath) || ++_renderedFrames < 30)
        {
            return;
        }

        FramebufferCapture.SavePng(screenshotPath, ClientSize.X, ClientSize.Y);
        _screenshotCaptured = true;

        if (string.Equals(
                Environment.GetEnvironmentVariable("HYPERION_SCREENSHOT_EXIT"),
                "1",
                StringComparison.Ordinal))
        {
            Close();
        }
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        if (e.Height > 0)
        {
            Camera.AspectRatio = e.Width / (float)e.Height;
        }
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        base.OnFocusedChanged(e);
        if (e.IsFocused)
        {
            Camera.ResetMouseDelta();
        }
    }

    protected override void OnUnload()
    {
        _renderer?.Dispose();
        _renderer = null;
        HyperionLog.Information("engine_stop", "Hyperion is shutting down cleanly.");
        _diagnostics?.Dispose();
        _diagnostics = null;
        Assets.Clear();
        HyperionLog.Shutdown();
        base.OnUnload();
    }

    protected abstract void OnStart();

    protected virtual void OnTick(GameTime time)
    {
    }
}
