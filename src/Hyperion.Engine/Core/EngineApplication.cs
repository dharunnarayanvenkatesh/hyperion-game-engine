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
    private double _elapsedSeconds;

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
        Camera = new Camera(new Vector3(0f, 1.25f, 6f), width / (float)height);
    }

    protected World World { get; } = new();

    protected Camera Camera { get; }

    protected override void OnLoad()
    {
        base.OnLoad();

        GL.Enable(EnableCap.DepthTest);
        GL.Enable(EnableCap.Multisample);
        GL.Enable(EnableCap.CullFace);
        GL.CullFace(TriangleFace.Back);
        GL.ClearColor(0.015f, 0.02f, 0.035f, 1f);

        _renderer = new Renderer();
        CursorState = CursorState.Normal;
        Camera.ResetMouseDelta();
        OnStart();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            Close();
            return;
        }

        _elapsedSeconds += args.Time;
        var gameTime = new GameTime(_elapsedSeconds, args.Time);
        Camera.Update(KeyboardState, MouseState, args.Time);
        OnTick(gameTime);
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _renderer?.Render(World, Camera);
        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs args)
    {
        base.OnResize(args);
        GL.Viewport(0, 0, args.Width, args.Height);
        if (args.Height > 0)
        {
            Camera.AspectRatio = args.Width / (float)args.Height;
        }
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs args)
    {
        base.OnFocusedChanged(args);
        if (args.IsFocused)
        {
            Camera.ResetMouseDelta();
        }
    }

    protected override void OnUnload()
    {
        _renderer?.Dispose();
        _renderer = null;
        base.OnUnload();
    }

    protected abstract void OnStart();

    protected virtual void OnTick(GameTime time)
    {
    }
}
