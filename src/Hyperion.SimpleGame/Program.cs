using Hyperion.Engine.Core;
using Hyperion.Engine.Diagnostics;
using Hyperion.Engine.Graphics;
using Hyperion.Engine.Scene;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

using var game = new CrystalCollectorGame();
game.Run();

internal sealed class CrystalCollectorGame() : EngineApplication("Hyperion — Crystal Collector")
{
    private const float ArenaLimit = 5.3f;
    private const float PlayerSpeed = 4.2f;
    private readonly List<Crystal> _crystals = [];
    private Entity? _player;
    private int _score;
    private bool _hasWon;

    protected override bool UseDefaultCameraControls => false;

    protected override void OnStart()
    {
        Camera.SetPose(new Vector3(0f, 8.5f, 9f), new Vector3(0f, 0f, 0f));
        Camera.FarPlane = 50f;

        CreateLights();
        CreateArena();
        CreatePlayer();
        CreateCrystals();
        UpdateStatus();

        HyperionLog.Information("game_started", "Crystal Collector started.");
    }

    protected override void OnTick(GameTime time)
    {
        if (_hasWon)
        {
            if (KeyboardState.IsKeyPressed(Keys.R))
            {
                Restart();
            }

            return;
        }

        MovePlayer((float)Math.Min(time.DeltaSeconds, 0.05));
        AnimateAndCollectCrystals((float)time.TotalSeconds);
    }

    private void MovePlayer(float deltaSeconds)
    {
        if (_player is null)
        {
            return;
        }

        var direction = Vector3.Zero;
        if (KeyboardState.IsKeyDown(Keys.W) || KeyboardState.IsKeyDown(Keys.Up)) direction.Z -= 1f;
        if (KeyboardState.IsKeyDown(Keys.S) || KeyboardState.IsKeyDown(Keys.Down)) direction.Z += 1f;
        if (KeyboardState.IsKeyDown(Keys.A) || KeyboardState.IsKeyDown(Keys.Left)) direction.X -= 1f;
        if (KeyboardState.IsKeyDown(Keys.D) || KeyboardState.IsKeyDown(Keys.Right)) direction.X += 1f;

        if (direction.LengthSquared <= 0f)
        {
            return;
        }

        direction = Vector3.Normalize(direction);
        var position = _player.Transform.Position + direction * PlayerSpeed * deltaSeconds;
        position.X = MathHelper.Clamp(position.X, -ArenaLimit, ArenaLimit);
        position.Z = MathHelper.Clamp(position.Z, -ArenaLimit, ArenaLimit);
        _player.Transform.Position = position;
        _player.Transform.Rotation += new Vector3(90f * deltaSeconds, 120f * deltaSeconds, 0f);
    }

    private void AnimateAndCollectCrystals(float totalSeconds)
    {
        if (_player is null)
        {
            return;
        }

        foreach (var crystal in _crystals)
        {
            if (!crystal.Entity.IsEnabled)
            {
                continue;
            }

            var position = crystal.Home;
            position.Y += MathF.Sin(totalSeconds * 2.5f + crystal.Phase) * 0.12f;
            crystal.Entity.Transform.Position = position;
            crystal.Entity.Transform.Rotation = new Vector3(0f, totalSeconds * 90f, 45f);

            var offset = crystal.Entity.Transform.Position - _player.Transform.Position;
            offset.Y = 0f;
            if (offset.LengthSquared > 0.65f * 0.65f)
            {
                continue;
            }

            crystal.Entity.IsEnabled = false;
            _score++;
            HyperionLog.Information(
                "crystal_collected",
                "The player collected a crystal.",
                new Dictionary<string, object?> { ["score"] = _score, ["total"] = _crystals.Count });

            if (_score == _crystals.Count)
            {
                _hasWon = true;
                HyperionLog.Information("game_won", "All crystals were collected.");
            }

            UpdateStatus();
        }
    }

    private void Restart()
    {
        _score = 0;
        _hasWon = false;
        _player!.Transform.Position = new Vector3(0f, 0.42f, 0f);
        foreach (var crystal in _crystals)
        {
            crystal.Entity.IsEnabled = true;
            crystal.Entity.Transform.Position = crystal.Home;
        }

        UpdateStatus();
        HyperionLog.Information("game_restarted", "Crystal Collector restarted.");
    }

    private void UpdateStatus()
    {
        StatusText = _hasWon
            ? "YOU WIN! Press R to restart"
            : $"Crystals {_score}/{_crystals.Count} — WASD to move";
    }

    private void CreatePlayer()
    {
        _player = World.CreateEntity("Player");
        _player.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Cube,
            new Material(new Vector3(0.04f, 0.55f, 1f), Metallic: 0.35f, Roughness: 0.22f));
        _player.Transform.Position = new Vector3(0f, 0.42f, 0f);
        _player.Transform.Scale = new Vector3(0.42f);
    }

    private void CreateCrystals()
    {
        var positions = new[]
        {
            new Vector3(-4f, 0.5f, -3.5f),
            new Vector3(3.8f, 0.5f, -3f),
            new Vector3(-3.5f, 0.5f, 3.2f),
            new Vector3(3.6f, 0.5f, 3.5f),
            new Vector3(0f, 0.5f, -4.5f)
        };

        for (var index = 0; index < positions.Length; index++)
        {
            var entity = World.CreateEntity($"Crystal {index + 1}");
            entity.MeshRenderer = new MeshRenderer(
                PrimitiveMesh.Sphere,
                new Material(new Vector3(1f, 0.58f, 0.04f), Metallic: 0.9f, Roughness: 0.16f));
            entity.Transform.Position = positions[index];
            entity.Transform.Scale = new Vector3(0.28f, 0.42f, 0.28f);
            _crystals.Add(new Crystal(entity, positions[index], index * 0.8f));
        }
    }

    private void CreateArena()
    {
        var floor = World.CreateEntity("Arena Floor");
        floor.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Plane,
            new Material(new Vector3(0.035f, 0.055f, 0.09f), Metallic: 0.2f, Roughness: 0.7f));
        floor.Transform.Position = new Vector3(0f, -0.02f, 0f);
        floor.Transform.Scale = new Vector3(6f, 1f, 6f);

        CreateWall("North Wall", new Vector3(0f, 0.35f, -5.8f), new Vector3(6f, 0.35f, 0.15f));
        CreateWall("South Wall", new Vector3(0f, 0.35f, 5.8f), new Vector3(6f, 0.35f, 0.15f));
        CreateWall("West Wall", new Vector3(-5.8f, 0.35f, 0f), new Vector3(0.15f, 0.35f, 6f));
        CreateWall("East Wall", new Vector3(5.8f, 0.35f, 0f), new Vector3(0.15f, 0.35f, 6f));
    }

    private void CreateWall(string name, Vector3 position, Vector3 scale)
    {
        var wall = World.CreateEntity(name);
        wall.MeshRenderer = new MeshRenderer(
            PrimitiveMesh.Cube,
            new Material(new Vector3(0.08f, 0.16f, 0.3f), Metallic: 0.65f, Roughness: 0.3f));
        wall.Transform.Position = position;
        wall.Transform.Scale = scale;
    }

    private void CreateLights()
    {
        World.CreateDirectionalLight(
            new Vector3(-0.55f, -1f, -0.4f),
            new Vector3(1f, 0.88f, 0.72f),
            intensity: 2.6f,
            shadowExtent: 8f);
        World.CreatePointLight(new Vector3(-4f, 6f, 4f), new Vector3(0.3f, 0.55f, 1f), 95f);
        World.CreatePointLight(new Vector3(4f, 5f, -4f), new Vector3(1f, 0.28f, 0.08f), 80f);
        World.CreatePointLight(new Vector3(0f, 7f, 0f), new Vector3(0.7f, 0.85f, 1f), 70f);
    }

    private sealed record Crystal(Entity Entity, Vector3 Home, float Phase);
}
