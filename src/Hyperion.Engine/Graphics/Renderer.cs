using Hyperion.Engine.Scene;
using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

internal sealed class Renderer : IDisposable
{
    private const string VertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;

        out vec3 vWorldPosition;
        out vec3 vNormal;

        void main()
        {
            vec4 worldPosition = vec4(aPosition, 1.0) * uModel;
            vWorldPosition = worldPosition.xyz;
            vNormal = normalize(aNormal * mat3(transpose(inverse(uModel))));
            gl_Position = worldPosition * uView * uProjection;
        }
        """;

    private const string FragmentShader = """
        #version 330 core

        in vec3 vWorldPosition;
        in vec3 vNormal;

        uniform vec3 uCameraPosition;
        uniform vec3 uBaseColor;
        uniform float uMetallic;
        uniform float uRoughness;

        out vec4 FragColor;

        void main()
        {
            vec3 normal = normalize(vNormal);
            vec3 viewDirection = normalize(uCameraPosition - vWorldPosition);
            vec3 lightDirection = normalize(vec3(-0.45, 0.85, 0.35));
            vec3 lightColor = vec3(1.0, 0.93, 0.82) * 2.2;

            float diffuseFactor = max(dot(normal, lightDirection), 0.0);
            vec3 halfway = normalize(lightDirection + viewDirection);
            float shininess = mix(128.0, 8.0, clamp(uRoughness, 0.0, 1.0));
            float specularFactor = pow(max(dot(normal, halfway), 0.0), shininess);

            vec3 dielectricSpecular = vec3(0.04);
            vec3 specularColor = mix(dielectricSpecular, uBaseColor, uMetallic);
            vec3 diffuseColor = uBaseColor * (1.0 - uMetallic);
            vec3 ambient = uBaseColor * vec3(0.025, 0.035, 0.06);
            vec3 color = ambient
                + diffuseColor * lightColor * diffuseFactor
                + specularColor * lightColor * specularFactor;

            color = color / (color + vec3(1.0));
            color = pow(color, vec3(1.0 / 2.2));
            FragColor = vec4(color, 1.0);
        }
        """;

    private readonly ShaderProgram _shader = new(VertexShader, FragmentShader);
    private readonly Dictionary<PrimitiveMesh, GpuMesh> _meshes = new()
    {
        [PrimitiveMesh.Cube] = new GpuMesh(PrimitiveMeshes.CreateCube()),
        [PrimitiveMesh.Plane] = new GpuMesh(PrimitiveMeshes.CreatePlane())
    };

    public void Render(World world, Camera camera)
    {
        _shader.Use();
        _shader.SetMatrix4("uView", camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", camera.ProjectionMatrix);
        _shader.SetVector3("uCameraPosition", camera.Position);

        foreach (var entity in world.Entities)
        {
            var renderer = entity.MeshRenderer;
            if (renderer is null)
            {
                continue;
            }

            _shader.SetMatrix4("uModel", entity.Transform.Matrix);
            _shader.SetVector3("uBaseColor", renderer.Material.BaseColor);
            _shader.SetFloat("uMetallic", renderer.Material.Metallic);
            _shader.SetFloat("uRoughness", renderer.Material.Roughness);
            _meshes[renderer.Mesh].Draw();
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _meshes.Values)
        {
            mesh.Dispose();
        }

        _shader.Dispose();
    }
}
