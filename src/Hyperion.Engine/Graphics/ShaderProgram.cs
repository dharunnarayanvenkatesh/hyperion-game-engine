using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

internal sealed class ShaderProgram : IDisposable
{
    private readonly Dictionary<string, int> _uniformLocations = [];

    public ShaderProgram(string vertexSource, string fragmentSource)
    {
        var vertexShader = Compile(ShaderType.VertexShader, vertexSource);
        var fragmentShader = Compile(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);
        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var success);

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        if (success == 0)
        {
            var log = GL.GetProgramInfoLog(Handle);
            GL.DeleteProgram(Handle);
            throw new InvalidOperationException($"Shader linking failed: {log}");
        }
    }

    public int Handle { get; }

    public void Use() => GL.UseProgram(Handle);

    public void SetMatrix4(string name, Matrix4 value)
    {
        // OpenTK stores matrices row-major while GLSL consumes column-major
        // uniforms. Transpose at upload so the shader receives the intended
        // model, view, and projection transforms.
        GL.UniformMatrix4(GetUniformLocation(name), true, ref value);
    }

    public void SetVector3(string name, Vector3 value)
    {
        GL.Uniform3(GetUniformLocation(name), value);
    }

    public void SetVector2(string name, Vector2 value)
    {
        GL.Uniform2(GetUniformLocation(name), value);
    }

    public void SetFloat(string name, float value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void SetInt(string name, int value)
    {
        GL.Uniform1(GetUniformLocation(name), value);
    }

    public void Dispose() => GL.DeleteProgram(Handle);

    private int GetUniformLocation(string name)
    {
        if (_uniformLocations.TryGetValue(name, out var location))
        {
            return location;
        }

        location = GL.GetUniformLocation(Handle, name);
        _uniformLocations[name] = location;
        return location;
    }

    private static int Compile(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var success);

        if (success != 0)
        {
            return shader;
        }

        var log = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"{type} compilation failed: {log}");
    }
}
