namespace Hyperion.Engine.Scene;

using Hyperion.Engine.Graphics;
using OpenTK.Mathematics;

public sealed class World
{
    private readonly List<Entity> _entities = [];
    private readonly List<PointLight> _pointLights = [];
    private readonly List<DirectionalLight> _directionalLights = [];
    private readonly WorldPartition _partition = new();

    public IReadOnlyList<Entity> Entities => _entities;

    public IReadOnlyList<PointLight> PointLights => _pointLights;

    public IReadOnlyList<DirectionalLight> DirectionalLights => _directionalLights;

    public Entity CreateEntity(string name)
    {
        var entity = new Entity(name);
        _entities.Add(entity);
        entity.Transform.Changed += () => _partition.Track(entity);
        _partition.Track(entity);
        return entity;
    }

    internal IEnumerable<Entity> QueryRenderCandidates(Vector3 center, float radius) =>
        _partition.QuerySphere(center, radius);

    public PointLight CreatePointLight(Vector3 position, Vector3 color, float intensity)
    {
        var light = new PointLight(position, color, intensity);
        _pointLights.Add(light);
        return light;
    }

    public DirectionalLight CreateDirectionalLight(
        Vector3 direction,
        Vector3 color,
        float intensity,
        float shadowExtent = 18f)
    {
        var light = new DirectionalLight(direction, color, intensity, shadowExtent);
        _directionalLights.Add(light);
        return light;
    }
}
