using Hyperion.Engine.Core;
using Hyperion.Engine.Graphics;
using Hyperion.Engine.Scene;
using OpenTK.Mathematics;

using var game = new SandboxGame();
game.Run();

internal sealed class SandboxGame() : EngineApplication("Hyperion Engine — First Light")
{
    private Entity? _heroCube;

    protected override void OnStart()
    {
        _heroCube = World.CreateEntity("Hero Cube");
        _heroCube.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Cube,
            new Material(new Vector3(0.12f, 0.42f, 0.95f), Metallic: 0.55f, Roughness: 0.22f));
        _heroCube.Transform.Position = new Vector3(0f, 0f, 0f);

        var accentCube = World.CreateEntity("Accent Cube");
        accentCube.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Cube,
            new Material(new Vector3(0.95f, 0.22f, 0.08f), Metallic: 0.15f, Roughness: 0.45f));
        accentCube.Transform.Position = new Vector3(2.5f, -0.5f, -1.5f);
        accentCube.Transform.Scale = new Vector3(0.5f);

        var floor = World.CreateEntity("Floor");
        floor.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Plane,
            new Material(new Vector3(0.16f, 0.18f, 0.22f), Metallic: 0.05f, Roughness: 0.82f));
        floor.Transform.Position = new Vector3(0f, -1.5f, 0f);
        floor.Transform.Scale = new Vector3(12f, 1f, 12f);
    }

    protected override void OnTick(GameTime time)
    {
        if (_heroCube is null)
        {
            return;
        }

        _heroCube.Transform.Rotation = new Vector3(
            18f + (float)time.TotalSeconds * 17f,
            (float)time.TotalSeconds * 28f,
            0f);
    }
}
