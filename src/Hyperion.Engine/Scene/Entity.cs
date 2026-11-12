using Hyperion.Engine.Graphics;

namespace Hyperion.Engine.Scene;

public sealed class Entity(string name)
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; } = name;

    public bool IsEnabled { get; set; } = true;

    public Transform Transform { get; } = new();

    public MeshRenderer? MeshRenderer { get; set; }
}
