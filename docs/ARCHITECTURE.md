# Hyperion architecture

Hyperion separates authored assets, scene state, and GPU state.

```text
Blender / glTF
      ↓
AssetManager → immutable MeshAsset + Material
      ↓
World → Entity → Transform + MeshRenderer
      ↓
WorldPartition → coarse render candidates
      ↓
Camera bounds test → visible entities
      ↓
Renderer → lazily cached GpuMesh → OpenGL
```

## Ownership rules

- `AssetManager` owns CPU-side imported asset caches.
- `World` owns entities, lights, and the spatial partition.
- `Entity` is a small identity plus explicit components.
- `Renderer` owns GPU objects and disposes them with the graphics context.
- Sandbox/game projects compose scenes but do not reach into renderer internals.

## Scaling strategy

Entity movement updates its partition cell through `Transform.Changed`. The
renderer queries occupied cells near the camera, performs per-object bounding
sphere tests, and only then uploads or draws mesh resources. Future streaming
can attach load/unload state to these same cells without changing gameplay APIs.
