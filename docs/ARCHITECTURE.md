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
Directional light → depth-only ShadowMap pass
      ↓
Renderer → lazily cached GpuMesh → OpenGL
      ↓
Diagnostics → CPU/GPU timings + frame capture + memory/log/crash artifacts
```

## Ownership rules

- `AssetManager` owns CPU-side imported asset caches.
- `World` owns entities, lights, and the spatial partition.
- `Entity` is a small identity plus explicit components.
- `Renderer` owns GPU objects and disposes them with the graphics context.
- `ShadowMap` owns the directional-light depth texture and framebuffer.
- `EngineDiagnostics` owns profiler queries and runtime diagnostic snapshots.
- `HyperionLog` and `CrashReporter` own process-wide diagnostic artifacts.
- Sandbox/game projects compose scenes but do not reach into renderer internals.

## Scaling strategy

Entity movement updates its partition cell through `Transform.Changed`. The
renderer queries occupied cells near the camera, performs per-object bounding
sphere tests, and only then uploads or draws mesh resources. Future streaming
can attach load/unload state to these same cells without changing gameplay APIs.
