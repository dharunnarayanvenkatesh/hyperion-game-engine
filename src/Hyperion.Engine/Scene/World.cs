namespace Hyperion.Engine.Scene;

using Hyperion.Engine.Graphics;
using OpenTK.Mathematics;

public sealed class World
{
    private readonly List<Entity> _entities = [];
    private readonly List<PointLight> _pointLights = [];
    private readonly WorldPartition _partition = new();

    public IReadOnlyList<Entity> Entities => _entities;

    public IReadOnlyList<PointLight> PointLights => _pointLights;

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
}
