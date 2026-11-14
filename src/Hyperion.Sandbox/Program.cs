using Hyperion.Engine.Core;
using Hyperion.Engine.Graphics;
using Hyperion.Engine.Scene;
using OpenTK.Mathematics;

using var game = new SandboxGame();
game.Run();

internal sealed class SandboxGame() : EngineApplication("Hyperion Engine — PBR Laboratory")
{
    private Entity? _heroSphere;
    private PointLight? _orbitingLight;

    protected override void OnStart()
    {
        CreateLighting();
        CreateMaterialShowcase();
        CreateImportedAssetFixture();
        CreateEnvironment();
    }

    protected override void OnTick(GameTime time)
    {
        if (_heroSphere is not null)
        {
            _heroSphere.Transform.Rotation = new Vector3(
                0f,
                (float)time.TotalSeconds * 12f,
                0f);
        }

        if (_orbitingLight is not null)
        {
            var timeValue = (float)time.TotalSeconds * 0.55f;
            _orbitingLight.Position = new Vector3(
                MathF.Cos(timeValue) * 4.5f,
                2.2f,
                MathF.Sin(timeValue) * 4.5f);
        }
    }

    private void CreateLighting()
    {
        World.CreateDirectionalLight(
            new Vector3(-0.5f, -1f, -0.35f),
            new Vector3(1f, 0.9f, 0.76f),
            intensity: 2.4f,
            shadowExtent: 18f);

        World.CreatePointLight(
            new Vector3(-3.8f, 4.5f, 4.5f),
            new Vector3(1.0f, 0.78f, 0.58f),
            85f);

        World.CreatePointLight(
            new Vector3(4.5f, 2.5f, 2.5f),
            new Vector3(0.35f, 0.58f, 1.0f),
            72f);

        World.CreatePointLight(
            new Vector3(0f, 5f, -3.5f),
            new Vector3(0.65f, 0.35f, 1.0f),
            65f);

        _orbitingLight = World.CreatePointLight(
            new Vector3(0f, 2.2f, 4.5f),
            new Vector3(1.0f, 0.18f, 0.08f),
            45f);
    }

    private void CreateMaterialShowcase()
    {
        _heroSphere = CreateSphere(
            "Hero — Polished Titanium",
            new Vector3(0f, 0.15f, 0f),
            1.25f,
            new Material(new Vector3(0.42f, 0.5f, 0.62f), Metallic: 0.95f, Roughness: 0.14f));

        var samples = new[]
        {
            new Material(new Vector3(0.88f, 0.09f, 0.04f), Metallic: 0.05f, Roughness: 0.28f),
            new Material(new Vector3(0.95f, 0.64f, 0.18f), Metallic: 1f, Roughness: 0.2f),
            new Material(new Vector3(0.08f, 0.36f, 0.9f), Metallic: 0.2f, Roughness: 0.42f),
            new Material(new Vector3(0.12f, 0.72f, 0.36f), Metallic: 0f, Roughness: 0.68f)
        };

        for (var index = 0; index < samples.Length; index++)
        {
            var x = -3.3f + index * 2.2f;
            CreateSphere($"Material Sample {index + 1}", new Vector3(x, -1.05f, -0.65f), 0.55f, samples[index]);
        }
    }

    private void CreateEnvironment()
    {
        var floor = World.CreateEntity("Graphite Floor");
        floor.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Plane,
            new Material(new Vector3(0.055f, 0.065f, 0.085f), Metallic: 0.25f, Roughness: 0.72f));
        floor.Transform.Position = new Vector3(0f, -1.62f, 0f);
        floor.Transform.Scale = new Vector3(14f, 1f, 14f);

        var pedestal = World.CreateEntity("Hero Pedestal");
        pedestal.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Cube,
            new Material(new Vector3(0.035f, 0.04f, 0.055f), Metallic: 0.6f, Roughness: 0.38f));
        pedestal.Transform.Position = new Vector3(0f, -1.38f, 0f);
        pedestal.Transform.Scale = new Vector3(1.75f, 0.22f, 1.75f);
    }

    private void CreateImportedAssetFixture()
    {
        var assetPath = Path.Combine(AppContext.BaseDirectory, "Assets", "sample-triangle.gltf");
        var importedMeshes = Assets.LoadGltf(assetPath);
        var importedEntity = World.CreateEntity("Blender/glTF Pipeline Fixture");
        importedEntity.MeshRenderer = new MeshRenderer(importedMeshes[0]);
        importedEntity.Transform.Position = new Vector3(-3.9f, 1.1f, -1.2f);
        importedEntity.Transform.Scale = new Vector3(0.42f);
    }

    private Entity CreateSphere(string name, Vector3 position, float scale, Material material)
    {
        var entity = World.CreateEntity(name);
        entity.MeshRenderer = new MeshRenderer(PrimitiveMesh.Sphere, material);
        entity.Transform.Position = position;
        entity.Transform.Scale = new Vector3(scale);
        return entity;
    }
}
