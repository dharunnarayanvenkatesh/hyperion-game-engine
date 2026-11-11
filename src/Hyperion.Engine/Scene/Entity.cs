using Hyperion.Engine.Graphics;

namespace Hyperion.Engine.Scene;

public sealed class Entity(string name)
{
    public string Name { get; } = name;

    public Transform Transform { get; } = new();

    public MeshRenderer? MeshRenderer { get; set; }
}
