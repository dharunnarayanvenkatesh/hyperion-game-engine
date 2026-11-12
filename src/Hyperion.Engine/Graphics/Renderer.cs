using Hyperion.Engine.Scene;
using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

internal sealed class Renderer : IDisposable
{
    private const int MaxPointLights = 4;

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

        const float PI = 3.14159265359;
        const int MAX_POINT_LIGHTS = 4;

        in vec3 vWorldPosition;
        in vec3 vNormal;

        uniform vec3 uCameraPosition;
        uniform vec3 uBaseColor;
        uniform float uMetallic;
        uniform float uRoughness;
        uniform int uPointLightCount;
        uniform vec3 uPointLightPositions[MAX_POINT_LIGHTS];
        uniform vec3 uPointLightColors[MAX_POINT_LIGHTS];

        out vec4 FragColor;

        float distributionGgx(vec3 normal, vec3 halfway, float roughness)
        {
            float alpha = roughness * roughness;
            float alphaSquared = alpha * alpha;
            float normalDotHalfway = max(dot(normal, halfway), 0.0);
            float denominator = normalDotHalfway * normalDotHalfway * (alphaSquared - 1.0) + 1.0;
            return alphaSquared / max(PI * denominator * denominator, 0.000001);
        }

        float geometrySchlickGgx(float normalDotDirection, float roughness)
        {
            float r = roughness + 1.0;
            float k = (r * r) / 8.0;
            return normalDotDirection / max(normalDotDirection * (1.0 - k) + k, 0.000001);
        }

        float geometrySmith(vec3 normal, vec3 viewDirection, vec3 lightDirection, float roughness)
        {
            float normalDotView = max(dot(normal, viewDirection), 0.0);
            float normalDotLight = max(dot(normal, lightDirection), 0.0);
            return geometrySchlickGgx(normalDotView, roughness)
                * geometrySchlickGgx(normalDotLight, roughness);
        }

        vec3 fresnelSchlick(float cosine, vec3 reflectanceAtNormal)
        {
            return reflectanceAtNormal
                + (1.0 - reflectanceAtNormal) * pow(clamp(1.0 - cosine, 0.0, 1.0), 5.0);
        }

        vec3 acesToneMap(vec3 color)
        {
            const float a = 2.51;
            const float b = 0.03;
            const float c = 2.43;
            const float d = 0.59;
            const float e = 0.14;
            return clamp((color * (a * color + b)) / (color * (c * color + d) + e), 0.0, 1.0);
        }

        void main()
        {
            vec3 normal = normalize(vNormal);
            vec3 viewDirection = normalize(uCameraPosition - vWorldPosition);
            float roughness = clamp(uRoughness, 0.045, 1.0);
            float metallic = clamp(uMetallic, 0.0, 1.0);
            vec3 baseReflectance = mix(vec3(0.04), uBaseColor, metallic);
            vec3 directLighting = vec3(0.0);

            for (int index = 0; index < uPointLightCount; index++)
            {
                vec3 toLight = uPointLightPositions[index] - vWorldPosition;
                float distanceSquared = max(dot(toLight, toLight), 0.01);
                vec3 lightDirection = normalize(toLight);
                vec3 halfway = normalize(viewDirection + lightDirection);
                vec3 radiance = uPointLightColors[index] / distanceSquared;

                float distribution = distributionGgx(normal, halfway, roughness);
                float geometry = geometrySmith(normal, viewDirection, lightDirection, roughness);
                vec3 fresnel = fresnelSchlick(max(dot(halfway, viewDirection), 0.0), baseReflectance);

                vec3 numerator = distribution * geometry * fresnel;
                float denominator = 4.0
                    * max(dot(normal, viewDirection), 0.0)
                    * max(dot(normal, lightDirection), 0.0)
                    + 0.0001;
                vec3 specular = numerator / denominator;

                vec3 specularWeight = fresnel;
                vec3 diffuseWeight = (vec3(1.0) - specularWeight) * (1.0 - metallic);
                float normalDotLight = max(dot(normal, lightDirection), 0.0);
                directLighting += (diffuseWeight * uBaseColor / PI + specular)
                    * radiance
                    * normalDotLight;
            }

            float skyAmount = normal.y * 0.5 + 0.5;
            vec3 hemisphere = mix(vec3(0.012, 0.008, 0.006), vec3(0.035, 0.055, 0.09), skyAmount);
            vec3 ambient = hemisphere * uBaseColor * mix(1.0, 0.35, metallic);
            vec3 color = acesToneMap(ambient + directLighting);
            color = pow(color, vec3(1.0 / 2.2));
            FragColor = vec4(color, 1.0);
        }
        """;

    private readonly ShaderProgram _shader = new(VertexShader, FragmentShader);
    private readonly Dictionary<PrimitiveMesh, GpuMesh> _primitiveMeshes = new()
    {
        [PrimitiveMesh.Cube] = new GpuMesh(new MeshData(PrimitiveMeshes.CreateCube())),
        [PrimitiveMesh.Plane] = new GpuMesh(new MeshData(PrimitiveMeshes.CreatePlane())),
        [PrimitiveMesh.Sphere] = new GpuMesh(new MeshData(PrimitiveMeshes.CreateSphere()))
    };
    private readonly Dictionary<MeshAsset, GpuMesh> _assetMeshes = [];

    public void Render(World world, Camera camera)
    {
        _shader.Use();
        _shader.SetMatrix4("uView", camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", camera.ProjectionMatrix);
        _shader.SetVector3("uCameraPosition", camera.Position);
        UploadLights(world);

        foreach (var entity in world.QueryRenderCandidates(camera.Position, camera.FarPlane))
        {
            if (!entity.IsEnabled)
            {
                continue;
            }

            var renderer = entity.MeshRenderer;
            if (renderer is null)
            {
                continue;
            }

            var worldBounds = renderer.LocalBounds.Transform(entity.Transform.Matrix, entity.Transform.Scale);
            if (!camera.IsVisible(worldBounds))
            {
                continue;
            }

            _shader.SetMatrix4("uModel", entity.Transform.Matrix);
            _shader.SetVector3("uBaseColor", renderer.Material.BaseColor);
            _shader.SetFloat("uMetallic", renderer.Material.Metallic);
            _shader.SetFloat("uRoughness", renderer.Material.Roughness);
            GetMesh(renderer).Draw();
        }
    }

    private GpuMesh GetMesh(MeshRenderer renderer)
    {
        if (renderer.Asset is not null)
        {
            if (!_assetMeshes.TryGetValue(renderer.Asset, out var gpuMesh))
            {
                gpuMesh = new GpuMesh(renderer.Asset.Data);
                _assetMeshes.Add(renderer.Asset, gpuMesh);
            }

            return gpuMesh;
        }

        return _primitiveMeshes[renderer.Primitive!.Value];
    }

    private void UploadLights(World world)
    {
        var lightCount = Math.Min(world.PointLights.Count, MaxPointLights);
        _shader.SetInt("uPointLightCount", lightCount);

        for (var index = 0; index < lightCount; index++)
        {
            var light = world.PointLights[index];
            _shader.SetVector3($"uPointLightPositions[{index}]", light.Position);
            _shader.SetVector3($"uPointLightColors[{index}]", light.Color * light.Intensity);
        }
    }

    public void Dispose()
    {
        foreach (var mesh in _primitiveMeshes.Values)
        {
            mesh.Dispose();
        }

        foreach (var mesh in _assetMeshes.Values)
        {
            mesh.Dispose();
        }

        _shader.Dispose();
    }
}
