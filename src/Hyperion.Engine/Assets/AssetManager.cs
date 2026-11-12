using Hyperion.Engine.Graphics;

namespace Hyperion.Engine.Assets;

public sealed class AssetManager
{
    private readonly Dictionary<string, IReadOnlyList<MeshAsset>> _gltfCache =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<MeshAsset> LoadGltf(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var fullPath = Path.GetFullPath(path);

        if (_gltfCache.TryGetValue(fullPath, out var cached))
        {
            return cached;
        }

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The glTF asset could not be found.", fullPath);
        }

        var meshes = GltfImporter.Import(fullPath);
        _gltfCache.Add(fullPath, meshes);
        return meshes;
    }

    public void Clear() => _gltfCache.Clear();
}
