using System.Numerics;
using Hyperion.Engine.Graphics;
using SharpGLTF.Schema2;
using EngineMaterial = Hyperion.Engine.Graphics.Material;

namespace Hyperion.Engine.Assets;

internal static class GltfImporter
{
    public static IReadOnlyList<MeshAsset> Import(string path)
    {
        var model = ModelRoot.Load(path);
        var importedMeshes = new List<MeshAsset>();

        foreach (var mesh in model.LogicalMeshes)
        {
            for (var primitiveIndex = 0; primitiveIndex < mesh.Primitives.Count; primitiveIndex++)
            {
                var primitive = mesh.Primitives[primitiveIndex];
                var positionAccessor = primitive.GetVertexAccessor("POSITION")
                    ?? throw new InvalidDataException($"Mesh '{mesh.Name}' has a primitive without POSITION data.");

                var positions = positionAccessor.AsVector3Array();
                var triangles = primitive.GetTriangleIndices().ToArray();
                if (triangles.Length == 0)
                {
                    continue;
                }

                var normalAccessor = primitive.GetVertexAccessor("NORMAL");
                var normals = normalAccessor?.AsVector3Array().ToArray()
                    ?? CalculateNormals(positions, triangles);

                var vertices = new float[positions.Count * 6];
                for (var index = 0; index < positions.Count; index++)
                {
                    var position = positions[index];
                    var normal = normals[index];
                    var offset = index * 6;
                    vertices[offset] = position.X;
                    vertices[offset + 1] = position.Y;
                    vertices[offset + 2] = position.Z;
                    vertices[offset + 3] = normal.X;
                    vertices[offset + 4] = normal.Y;
                    vertices[offset + 5] = normal.Z;
                }

                var indices = new uint[triangles.Length * 3];
                for (var index = 0; index < triangles.Length; index++)
                {
                    var triangle = triangles[index];
                    indices[index * 3] = checked((uint)triangle.A);
                    indices[index * 3 + 1] = checked((uint)triangle.B);
                    indices[index * 3 + 2] = checked((uint)triangle.C);
                }

                var meshName = string.IsNullOrWhiteSpace(mesh.Name) ? $"Mesh {mesh.LogicalIndex}" : mesh.Name;
                var assetName = mesh.Primitives.Count == 1 ? meshName : $"{meshName} / Primitive {primitiveIndex}";
                importedMeshes.Add(new MeshAsset(
                    assetName,
                    new MeshData(vertices, indices),
                    ReadMaterial(primitive.Material)));
            }
        }

        if (importedMeshes.Count == 0)
        {
            throw new InvalidDataException($"'{path}' did not contain any triangle meshes.");
        }

        return importedMeshes;
    }

    private static EngineMaterial ReadMaterial(SharpGLTF.Schema2.Material? source)
    {
        if (source is null)
        {
            return EngineMaterial.Default;
        }

        var baseColorChannel = source.FindChannel("BaseColor");
        var metallicRoughnessChannel = source.FindChannel("MetallicRoughness");
        var baseColor = ReadParameter(baseColorChannel, "RGBA", Vector4.One);
        var metallic = ReadParameter(metallicRoughnessChannel, "MetallicFactor", 1f);
        var roughness = ReadParameter(metallicRoughnessChannel, "RoughnessFactor", 1f);

        return new EngineMaterial(
            new OpenTK.Mathematics.Vector3(baseColor.X, baseColor.Y, baseColor.Z),
            Metallic: metallic,
            Roughness: roughness);
    }

    private static T ReadParameter<T>(MaterialChannel? channel, string name, T fallback)
    {
        var parameter = channel?.Parameters.FirstOrDefault(candidate => candidate.Name == name);
        return parameter?.Value is T value ? value : fallback;
    }

    private static Vector3[] CalculateNormals(
        IReadOnlyList<Vector3> positions,
        IReadOnlyList<(int A, int B, int C)> triangles)
    {
        var normals = new Vector3[positions.Count];

        foreach (var (a, b, c) in triangles)
        {
            var edgeA = positions[b] - positions[a];
            var edgeB = positions[c] - positions[a];
            var faceNormal = Vector3.Cross(edgeA, edgeB);
            normals[a] += faceNormal;
            normals[b] += faceNormal;
            normals[c] += faceNormal;
        }

        for (var index = 0; index < normals.Length; index++)
        {
            normals[index] = normals[index].LengthSquared() > 0f
                ? Vector3.Normalize(normals[index])
                : Vector3.UnitY;
        }

        return normals;
    }
}
