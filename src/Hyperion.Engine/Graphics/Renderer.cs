using Hyperion.Engine.Diagnostics;
using Hyperion.Engine.Scene;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Hyperion.Engine.Graphics;

internal sealed class Renderer : IDisposable
{
    private const int MaxPointLights = 4;

    private const string VertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;
        layout (location = 1) in vec3 aNormal;
        layout (location = 2) in vec2 aTexCoord;

        uniform mat4 uModel;
        uniform mat4 uView;
        uniform mat4 uProjection;
        uniform mat4 uLightViewProjection;

        out vec3 vWorldPosition;
        out vec3 vNormal;
        out vec2 vTexCoord;
        out vec4 vLightPosition;

        void main()
        {
            vec4 worldPosition = vec4(aPosition, 1.0) * uModel;
            vWorldPosition = worldPosition.xyz;
            vNormal = normalize(aNormal * mat3(transpose(inverse(uModel))));
            vTexCoord = aTexCoord;
            vLightPosition = worldPosition * uLightViewProjection;
            gl_Position = worldPosition * uView * uProjection;
        }
        """;

    private const string FragmentShader = """
        #version 330 core

        const float PI = 3.14159265359;
        const int MAX_POINT_LIGHTS = 4;

        in vec3 vWorldPosition;
        in vec3 vNormal;
        in vec2 vTexCoord;
        in vec4 vLightPosition;

        uniform vec3 uCameraPosition;
        uniform vec3 uBaseColor;
        uniform float uMetallic;
        uniform float uRoughness;
        uniform sampler2D uBaseColorTexture;
        uniform sampler2D uMetallicRoughnessTexture;
        uniform int uHasBaseColorTexture;
        uniform int uHasMetallicRoughnessTexture;
        uniform int uPointLightCount;
        uniform vec3 uPointLightPositions[MAX_POINT_LIGHTS];
        uniform vec3 uPointLightColors[MAX_POINT_LIGHTS];
        uniform int uHasDirectionalLight;
        uniform vec3 uDirectionalLightDirection;
        uniform vec3 uDirectionalLightColor;
        uniform sampler2D uShadowMap;
        uniform vec2 uShadowTexelSize;

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

        vec3 evaluatePbrLight(
            vec3 normal,
            vec3 viewDirection,
            vec3 lightDirection,
            vec3 radiance,
            vec3 baseColor,
            vec3 baseReflectance,
            float metallic,
            float roughness)
        {
            vec3 halfway = normalize(viewDirection + lightDirection);
            float distribution = distributionGgx(normal, halfway, roughness);
            float geometry = geometrySmith(normal, viewDirection, lightDirection, roughness);
            vec3 fresnel = fresnelSchlick(max(dot(halfway, viewDirection), 0.0), baseReflectance);
            vec3 numerator = distribution * geometry * fresnel;
            float denominator = 4.0
                * max(dot(normal, viewDirection), 0.0)
                * max(dot(normal, lightDirection), 0.0)
                + 0.0001;
            vec3 specular = numerator / denominator;
            vec3 diffuseWeight = (vec3(1.0) - fresnel) * (1.0 - metallic);
            float normalDotLight = max(dot(normal, lightDirection), 0.0);
            return (diffuseWeight * baseColor / PI + specular) * radiance * normalDotLight;
        }

        float calculateShadow(vec3 normal, vec3 lightDirection)
        {
            vec3 projected = vLightPosition.xyz / max(vLightPosition.w, 0.0001);
            projected = projected * 0.5 + 0.5;
            if (projected.z <= 0.0 || projected.z >= 1.0
                || projected.x <= 0.0 || projected.x >= 1.0
                || projected.y <= 0.0 || projected.y >= 1.0)
            {
                return 0.0;
            }

            float bias = max(0.0025 * (1.0 - dot(normal, lightDirection)), 0.00035);
            float shadow = 0.0;
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    float closestDepth = texture(
                        uShadowMap,
                        projected.xy + vec2(x, y) * uShadowTexelSize).r;
                    shadow += projected.z - bias > closestDepth ? 1.0 : 0.0;
                }
            }

            return shadow / 9.0;
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
            vec3 baseColor = uBaseColor;
            float roughness = clamp(uRoughness, 0.045, 1.0);
            float metallic = clamp(uMetallic, 0.0, 1.0);

            if (uHasBaseColorTexture != 0)
            {
                baseColor *= texture(uBaseColorTexture, vTexCoord).rgb;
            }

            if (uHasMetallicRoughnessTexture != 0)
            {
                vec4 packedMaterial = texture(uMetallicRoughnessTexture, vTexCoord);
                roughness = clamp(roughness * packedMaterial.g, 0.045, 1.0);
                metallic = clamp(metallic * packedMaterial.b, 0.0, 1.0);
            }

            vec3 baseReflectance = mix(vec3(0.04), baseColor, metallic);
            vec3 directLighting = vec3(0.0);

            for (int index = 0; index < uPointLightCount; index++)
            {
                vec3 toLight = uPointLightPositions[index] - vWorldPosition;
                float distanceSquared = max(dot(toLight, toLight), 0.01);
                vec3 lightDirection = normalize(toLight);
                vec3 radiance = uPointLightColors[index] / distanceSquared;
                directLighting += evaluatePbrLight(
                    normal,
                    viewDirection,
                    lightDirection,
                    radiance,
                    baseColor,
                    baseReflectance,
                    metallic,
                    roughness);
            }

            if (uHasDirectionalLight != 0)
            {
                vec3 lightDirection = normalize(-uDirectionalLightDirection);
                float visibility = 1.0 - calculateShadow(normal, lightDirection);
                directLighting += evaluatePbrLight(
                    normal,
                    viewDirection,
                    lightDirection,
                    uDirectionalLightColor,
                    baseColor,
                    baseReflectance,
                    metallic,
                    roughness) * visibility;
            }

            float skyAmount = normal.y * 0.5 + 0.5;
            vec3 hemisphere = mix(vec3(0.012, 0.008, 0.006), vec3(0.035, 0.055, 0.09), skyAmount);
            vec3 ambient = hemisphere * baseColor * mix(1.0, 0.35, metallic);
            vec3 color = acesToneMap(ambient + directLighting);
            color = pow(color, vec3(1.0 / 2.2));
            FragColor = vec4(color, 1.0);
        }
        """;

    private const string ShadowVertexShader = """
        #version 330 core

        layout (location = 0) in vec3 aPosition;

        uniform mat4 uModel;
        uniform mat4 uLightViewProjection;

        void main()
        {
            gl_Position = vec4(aPosition, 1.0) * uModel * uLightViewProjection;
        }
        """;

    private const string ShadowFragmentShader = """
        #version 330 core

        void main()
        {
        }
        """;

    private readonly ShaderProgram _shader = new(VertexShader, FragmentShader);
    private readonly ShaderProgram _shadowShader = new(ShadowVertexShader, ShadowFragmentShader);
    private readonly ShadowMap _shadowMap = new(resolution: 2048);
    private readonly Dictionary<PrimitiveMesh, GpuMesh> _primitiveMeshes = new()
    {
        [PrimitiveMesh.Cube] = new GpuMesh(new MeshData(PrimitiveMeshes.CreateCube())),
        [PrimitiveMesh.Plane] = new GpuMesh(new MeshData(PrimitiveMeshes.CreatePlane())),
        [PrimitiveMesh.Sphere] = new GpuMesh(new MeshData(PrimitiveMeshes.CreateSphere()))
    };
    private readonly Dictionary<MeshAsset, GpuMesh> _assetMeshes = [];
    private readonly Dictionary<TextureAsset, GpuTexture> _textures = [];

    public RenderFrameStatistics Render(
        World world,
        Camera camera,
        FrameDebugger frameDebugger,
        int viewportWidth,
        int viewportHeight)
    {
        var candidates = 0;
        var visibleObjects = 0;
        var culledObjects = 0;
        var drawCalls = 0;
        var shadowDrawCalls = 0;
        long triangles = 0;

        var lightViewProjection = Matrix4.Identity;
        if (world.DirectionalLights.Count > 0)
        {
            (lightViewProjection, shadowDrawCalls) = RenderShadowMap(
                world,
                world.DirectionalLights[0],
                viewportWidth,
                viewportHeight);
        }

        _shader.Use();
        _shader.SetMatrix4("uView", camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", camera.ProjectionMatrix);
        _shader.SetVector3("uCameraPosition", camera.Position);
        UploadLights(world, lightViewProjection);

        foreach (var entity in world.QueryRenderCandidates(camera.Position, camera.FarPlane))
        {
            candidates++;
            if (!entity.IsEnabled)
            {
                culledObjects++;
                continue;
            }

            var renderer = entity.MeshRenderer;
            if (renderer is null)
            {
                culledObjects++;
                continue;
            }

            var worldBounds = renderer.LocalBounds.Transform(entity.Transform.Matrix, entity.Transform.Scale);
            if (!camera.IsVisible(worldBounds))
            {
                culledObjects++;
                continue;
            }

            _shader.SetMatrix4("uModel", entity.Transform.Matrix);
            ApplyMaterial(renderer.Material);
            var mesh = GetMesh(renderer);
            frameDebugger.RecordDraw(entity, renderer, mesh);
            mesh.Draw();
            visibleObjects++;
            drawCalls++;
            triangles += mesh.TriangleCount;
        }

        return new RenderFrameStatistics(
            candidates,
            visibleObjects,
            culledObjects,
            drawCalls,
            shadowDrawCalls,
            triangles);
    }

    private (Matrix4 LightViewProjection, int DrawCalls) RenderShadowMap(
        World world,
        DirectionalLight light,
        int viewportWidth,
        int viewportHeight)
    {
        var direction = light.Direction.LengthSquared > 0.000001f
            ? Vector3.Normalize(light.Direction)
            : Vector3.Normalize(new Vector3(-0.5f, -1f, -0.35f));
        var extent = MathF.Max(light.ShadowExtent, 1f);
        var up = MathF.Abs(Vector3.Dot(direction, Vector3.UnitY)) > 0.98f
            ? Vector3.UnitZ
            : Vector3.UnitY;
        var lightPosition = light.ShadowCenter - direction * extent * 1.5f;
        var lightView = Matrix4.LookAt(lightPosition, light.ShadowCenter, up);
        var lightProjection = Matrix4.CreateOrthographicOffCenter(
            -extent,
            extent,
            -extent,
            extent,
            0.1f,
            extent * 4f);
        var lightViewProjection = lightView * lightProjection;
        var drawCalls = 0;

        _shadowMap.BeginWrite();
        GL.Enable(EnableCap.PolygonOffsetFill);
        GL.PolygonOffset(2f, 4f);
        GL.CullFace(TriangleFace.Front);
        try
        {
            _shadowShader.Use();
            _shadowShader.SetMatrix4("uLightViewProjection", lightViewProjection);
            foreach (var entity in world.Entities)
            {
                if (!entity.IsEnabled || entity.MeshRenderer is null)
                {
                    continue;
                }

                _shadowShader.SetMatrix4("uModel", entity.Transform.Matrix);
                GetMesh(entity.MeshRenderer).Draw();
                drawCalls++;
            }
        }
        finally
        {
            GL.CullFace(TriangleFace.Back);
            GL.Disable(EnableCap.PolygonOffsetFill);
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, viewportWidth, viewportHeight);
        }

        return (lightViewProjection, drawCalls);
    }

    private void ApplyMaterial(Material material)
    {
        _shader.SetVector3("uBaseColor", material.BaseColor);
        _shader.SetFloat("uMetallic", material.Metallic);
        _shader.SetFloat("uRoughness", material.Roughness);
        _shader.SetInt("uBaseColorTexture", 0);
        _shader.SetInt("uMetallicRoughnessTexture", 1);

        var hasBaseColorTexture = material.BaseColorTexture is not null;
        var hasMetallicRoughnessTexture = material.MetallicRoughnessTexture is not null;
        _shader.SetInt("uHasBaseColorTexture", hasBaseColorTexture ? 1 : 0);
        _shader.SetInt("uHasMetallicRoughnessTexture", hasMetallicRoughnessTexture ? 1 : 0);

        if (material.BaseColorTexture is not null)
        {
            GetTexture(material.BaseColorTexture).Bind(0);
        }

        if (material.MetallicRoughnessTexture is not null)
        {
            GetTexture(material.MetallicRoughnessTexture).Bind(1);
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

    private GpuTexture GetTexture(TextureAsset asset)
    {
        if (!_textures.TryGetValue(asset, out var gpuTexture))
        {
            gpuTexture = new GpuTexture(asset);
            _textures.Add(asset, gpuTexture);
        }

        return gpuTexture;
    }

    private void UploadLights(World world, Matrix4 lightViewProjection)
    {
        var lightCount = Math.Min(world.PointLights.Count, MaxPointLights);
        _shader.SetInt("uPointLightCount", lightCount);

        for (var index = 0; index < lightCount; index++)
        {
            var light = world.PointLights[index];
            _shader.SetVector3($"uPointLightPositions[{index}]", light.Position);
            _shader.SetVector3($"uPointLightColors[{index}]", light.Color * light.Intensity);
        }

        var hasDirectionalLight = world.DirectionalLights.Count > 0;
        _shader.SetInt("uHasDirectionalLight", hasDirectionalLight ? 1 : 0);
        _shader.SetMatrix4("uLightViewProjection", lightViewProjection);
        _shader.SetInt("uShadowMap", 2);
        _shader.SetVector2(
            "uShadowTexelSize",
            new Vector2(1f / _shadowMap.Resolution, 1f / _shadowMap.Resolution));

        if (hasDirectionalLight)
        {
            var light = world.DirectionalLights[0];
            var direction = light.Direction.LengthSquared > 0.000001f
                ? Vector3.Normalize(light.Direction)
                : Vector3.Normalize(new Vector3(-0.5f, -1f, -0.35f));
            _shader.SetVector3("uDirectionalLightDirection", direction);
            _shader.SetVector3("uDirectionalLightColor", light.Color * light.Intensity);
            _shadowMap.Bind(2);
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

        foreach (var texture in _textures.Values)
        {
            texture.Dispose();
        }

        _shader.Dispose();
        _shadowShader.Dispose();
        _shadowMap.Dispose();
    }
}
