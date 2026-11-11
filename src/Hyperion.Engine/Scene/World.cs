namespace Hyperion.Engine.Scene;

public sealed class World
{
    private readonly List<Entity> _entities = [];

    public IReadOnlyList<Entity> Entities => _entities;

    public Entity CreateEntity(string name)
    {
        var entity = new Entity(name);
        _entities.Add(entity);
        return entity;
    }
}
