// Layout of the baked GltfDirectionalAlbedo.bin asset in the scene texel buffer.
// PBRT GGX reflection at eta=1.5; 262144 deterministic Hammersley VNDF samples
// per cell. Recompute the asset if eta, the specular lobe or grid changes.
// alpha=.001*1000^(a/31), cosine=(1001^(c/63)-1)/1000.
// Float32 little-endian: 2048 row-major directional values, then 32 row averages.
// Each average integrates the same log-cosine interpolation used by the shader.
// Scene.ImportMaterials loads these 8320 bytes after the 256-entry sRGB table.
// Shared buffer reads avoid per-invocation constant arrays in SPIR-V.
#ifndef SHOWCASE_GLTF_DIRECTIONAL_ALBEDO
#define SHOWCASE_GLTF_DIRECTIONAL_ALBEDO
static const uint gltfAlbedoOffset = 256;
static const uint gltfAlbedoAlphaCount = 32;
static const uint gltfAlbedoCosineCount = 64;
static const float gltfSmoothAverageAlbedo = 0.091777959;
#endif
