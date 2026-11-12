# Blender to Hyperion

Hyperion treats glTF 2.0 as its stable interchange format. Blender remains the
authoring tool; Hyperion imports runtime-ready data rather than trying to read
Blender's internal `.blend` format.

## Blender export settings

1. Apply object scale and rotation with `Ctrl+A` before export.
2. Use **File → Export → glTF 2.0**.
3. Prefer **glTF Binary (`.glb`)** for self-contained production assets.
4. Enable **Selected Objects** when exporting a focused asset.
5. Enable **Normals** and use **+Y Up**, Blender's glTF default.
6. Use Principled BSDF materials with metallic/roughness values.
7. Triangulation is accepted by the importer; applying a Triangulate modifier
   in Blender makes the final topology explicit and reproducible.

## Runtime loading

```csharp
var meshes = Assets.LoadGltf("Assets/robot.glb");
var robot = World.CreateEntity("Robot");
robot.MeshRenderer = new MeshRenderer(meshes[0]);
```

`AssetManager` canonicalizes paths and caches imported models. Each glTF
primitive becomes an immutable `MeshAsset`; repeated calls return the same
resources. Geometry is uploaded lazily when first visible.

## Supported in this milestone

- `.gltf` and `.glb`
- Indexed triangle meshes
- Positions and normals
- Generated smooth normals when normals are absent
- Multiple meshes and primitives
- Base-color, metallic, and roughness factors
- Automatic bounds for culling

## Next asset-pipeline layers

- Texture coordinates and base-color/normal/ORM textures
- Node hierarchy transforms
- Tangents and normal mapping
- Skeletal animation and morph targets
- Background import and derived-data caching

The checked-in `assets/sample-triangle.gltf` file is copied into the sandbox
build and loaded at startup. This makes importer regressions fail during the
normal runtime smoke test.
