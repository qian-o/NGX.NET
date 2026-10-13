using System.Numerics;
using SharpGLTF.Schema2;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal class Scene
{
    private double animationSeconds;
    private int staticObjectCount;

    public SceneVertex[] Vertices { get; private set; } = [];

    public SceneMaterial[] Materials { get; private set; } = [];

    public uint[] Texels { get; private set; } = [];

    public TextureDescription[] TextureInfo { get; private set; } = [];

    public SceneObject[] Objects { get; private set; } = [];

    public Vector3 Minimum { get; private set; }

    public Vector3 Maximum { get; private set; }

    public float GroundHeight { get; private set; }

    public float Scale => (Maximum - Minimum).Length();

    public float RayEpsilon => Scale * 1.0E-05f;

    public SceneAreaLight[] AreaLights { get; private set; } = [];

    public float AreaLightPower { get; private set; }

    public bool HasMotion
    {
        get
        {
            foreach (SceneObject item in Objects)
            {
                if (item.Offset != item.PreviousOffset)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public ulong TransformVersion { get; private set; }

    public void Update(double deltaSeconds)
    {
        animationSeconds += deltaSeconds;
        Vector3 center = (Minimum + Maximum) * 0.5f;
        bool changed = false;
        for (int i = staticObjectCount; i < Objects.Length; i++)
        {
            float phase = ((float)animationSeconds * 0.7f) + ((i - staticObjectCount) * MathF.PI);
            Vector3 p = new(center.X + (MathF.Sin(phase) * Scale * 0.09f), GroundHeight + (Scale * 0.04f), center.Z + (MathF.Cos(phase) * Scale * 0.018f));
            Vector4 offset = new(p, Objects[i].Offset.W);
            if (Objects[i].Offset != offset)
            {
                Objects[i].Offset = offset;
                changed = true;
            }
        }

        if (changed)
        {
            TransformVersion++;
        }
    }

    public void CommitHistory()
    {
        for (int i = 0; i < Objects.Length; i++)
        {
            Objects[i].PreviousOffset = Objects[i].Offset;
        }
    }

    private void BuildAreaLights()
    {
        List<SceneAreaLight> lights = [];
        double total = 0.0;
        for (int i = 0; i < Vertices.Length; i += 3)
        {
            SceneMaterial material = Materials[(int)Vertices[i].Normal.W];
            Vector3 emission = material.EmissiveMetallic.AsVector3();
            float luminance = Vector3.Dot(emission, new(0.2126f, 0.7152f, 0.0722f));
            if (luminance <= 0.0f)
            {
                continue;
            }

            Vector3 a = Vertices[i].Position.AsVector3();
            float area = Vector3.Cross(Vertices[i + 1].Position.AsVector3() - a, Vertices[i + 2].Position.AsVector3() - a).Length() * 0.5f;
            float power = MathF.PI * area * luminance * (material.Parameters.W is not 0.0f ? 2.0f : 1.0f);
            total += power;
            lights.Add(new()
            {
                Triangle = (uint)(i / 3),
                Area = area,
                Pmf = power
            });
        }

        double cumulative = 0.0;
        for (int i = 0; i < lights.Count; i++)
        {
            SceneAreaLight light = lights[i];
            light.Pmf = (float)(light.Pmf / total);
            cumulative += light.Pmf;
            light.Cdf = i == lights.Count - 1 ? 1.0f : (float)cumulative;
            lights[i] = light;
        }

        AreaLightPower = (float)total;
        AreaLights = [.. lights];
    }

    private float FindGroundHeight()
    {
        // The asset's bounding box includes its foundation below the walking surface. Locate the atrium floor with a downward ray through the scene's center.
        Vector3 center = (Minimum + Maximum) * 0.5f;
        Vector3 origin = new(center.X, Maximum.Y + RayEpsilon, center.Z);
        Vector3 direction = -Vector3.UnitY;
        float nearest = float.PositiveInfinity;
        for (int i = 0; i < Vertices.Length; i += 3)
        {
            if (Vertices[i].Position.W >= staticObjectCount)
            {
                continue;
            }

            Vector3 a = Vertices[i].Position.AsVector3();
            Vector3 edge1 = Vertices[i + 1].Position.AsVector3() - a;
            Vector3 edge2 = Vertices[i + 2].Position.AsVector3() - a;
            Vector3 p = Vector3.Cross(direction, edge2);
            float determinant = Vector3.Dot(edge1, p);
            if (determinant is 0.0f)
            {
                continue;
            }

            Vector3 relative = origin - a;
            float u = Vector3.Dot(relative, p) / determinant;
            Vector3 q = Vector3.Cross(relative, edge1);
            float v = Vector3.Dot(direction, q) / determinant;
            float distance = Vector3.Dot(edge2, q) / determinant;
            if (u >= 0.0f && v >= 0.0f && u + v <= 1.0f && distance >= 0.0f)
            {
                nearest = Math.Min(nearest, distance);
            }
        }

        if (!float.IsFinite(nearest))
        {
            throw new InvalidDataException("Could not locate the Sponza atrium floor.");
        }

        return origin.Y - nearest;
    }

    public static Scene Load(string path)
    {
        ModelRoot model = ModelRoot.Load(path);
        Scene scene = new();

        (List<SceneMaterial> materials, uint[] texels, List<TextureDescription> textureInfo) = ImportMaterials(model);

        int fallback = materials.Count;
        materials.Add(new()
        {
            BaseColor = Vector4.One,
            Parameters = new(0.8f, 1.0f, -1, 0.0f),
            Textures = new(-1)
        });
        List<SceneVertex> staticVertices = [];
        foreach (Node node in Node.Flatten(model.DefaultScene))
        {
            if (node.Mesh is null)
            {
                continue;
            }

            Matrix4x4 world = node.WorldMatrix;
            if (!Matrix4x4.Invert(world, out Matrix4x4 inverse))
            {
                throw new InvalidDataException($"Non-invertible scene transform: {node.Name}.");
            }

            Matrix4x4 normalMatrix = Matrix4x4.Transpose(inverse);
            float handedness = MathF.Sign(world.GetDeterminant());
            foreach (MeshPrimitive primitive in node.Mesh.Primitives)
            {
                IList<Vector3> positions = primitive.GetVertexAccessor("POSITION").AsVector3Array();
                IList<Vector3>? normals = primitive.GetVertexAccessor("NORMAL")?.AsVector3Array();
                IList<Vector2>? uv = primitive.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                IList<Vector4>? tangents = primitive.GetVertexAccessor("TANGENT")?.AsVector4Array();
                foreach ((int a, int b, int c) in primitive.GetTriangleIndices())
                {
                    int second = handedness < 0.0f ? c : b;
                    int third = handedness < 0.0f ? b : c;
                    Vector3 pa = Vector3.Transform(positions[a], world);
                    Vector3 pb = Vector3.Transform(positions[second], world);
                    Vector3 pc = Vector3.Transform(positions[third], world);
                    Vector3 edge1 = pb - pa;
                    Vector3 edge2 = pc - pa;
                    Vector3 faceNormal = Vector3.Cross(edge1, edge2);
                    if (faceNormal.LengthSquared() is 0.0f)
                    {
                        // Collapsed triangles have no coverage or valid geometric normal.
                        continue;
                    }

                    faceNormal = Vector3.Normalize(faceNormal);
                    Vector2 uv0 = uv?[a] ?? Vector2.Zero;
                    Vector2 uv1 = (uv?[second] ?? Vector2.Zero) - uv0;
                    Vector2 uv2 = (uv?[third] ?? Vector2.Zero) - uv0;
                    for (int k = 0; k < 3; k++)
                    {
                        int index = k switch
                        {
                            0 => a,
                            1 => second,
                            _ => third
                        };
                        Vector3 position = k switch
                        {
                            0 => pa,
                            1 => pb,
                            _ => pc
                        };
                        Vector3 n = normals is null ? faceNormal : Vector3.TransformNormal(normals[index], normalMatrix);
                        n = n.LengthSquared() > 0.0f ? Vector3.Normalize(n) : faceNormal;
                        Vector4 t = default;
                        if (tangents is not null)
                        {
                            Vector3 direction = Vector3.TransformNormal(tangents[index].AsVector3(), world);
                            if (direction.LengthSquared() > 0.0f)
                            {
                                t = new(Vector3.Normalize(direction), tangents[index].W * handedness);
                            }
                        }

                        if (Vector3.Cross(n, t.AsVector3()).LengthSquared() is 0.0f)
                        {
                            // Repair invalid authored tangents from the UV basis rather than rotating the normal map into an arbitrary frame.
                            t = GetTriangleTangent(n, edge1, edge2, uv1, uv2);
                        }

                        staticVertices.Add(new()
                        {
                            Position = new(position, 0.0f),
                            Normal = new(n, primitive.Material?.LogicalIndex ?? fallback),
                            Tangent = t,
                            UV = new(uv?[index] ?? Vector2.Zero, 0.0f, 0.0f)
                        });
                    }
                }
            }
        }

        if (staticVertices.Count is 0)
        {
            throw new InvalidDataException("The scene contains no triangles.");
        }

        scene.Minimum = staticVertices.Select(static v => v.Position.AsVector3()).Aggregate(Vector3.Min);
        scene.Maximum = staticVertices.Select(static v => v.Position.AsVector3()).Aggregate(Vector3.Max);
        List<SceneVertex> ordered = [];
        List<SceneObject> objects = [];

        // A triangle's three vertices share a material, so grouping preserves whole triangles. Homogeneous opacity/sidedness enables hardware hit handling.
        foreach (IGrouping<(bool Opaque, bool DoubleSided), SceneVertex> group in staticVertices.GroupBy(vertex =>
        {
            SceneMaterial material = materials[(int)vertex.Normal.W];

            return (Opaque: material.Parameters.Z < 0.0f, DoubleSided: material.Parameters.W is not 0.0f);
        }))
        {
            AddObject(group, group.Key.Opaque, group.Key.DoubleSided);
        }

        scene.staticObjectCount = objects.Count;

        // Gold and neutral chromium provide warm and untinted polished reflections. Base colors are linear reflectance; both use the same surface roughness.
        const float PolishedMetalRoughness = 0.08f;
        for (int i = 0; i < 2; i++)
        {
            int materialIndex = materials.Count;
            materials.Add(new()
            {
                BaseColor = i is 0 ? new(0.82f, 0.56f, 0.26f, 1.0f) : new(0.55f, 0.56f, 0.55f, 1.0f),
                EmissiveMetallic = new(0.0f, 0.0f, 0.0f, 1.0f),
                Parameters = new(PolishedMetalRoughness, 1.0f, -1, 0.0f),
                Textures = new(-1)
            });
            float radius = scene.Scale * 0.018f;
            AddObject(CreateSphere(radius, materialIndex), true, false, radius);
        }

        scene.Vertices = [.. ordered];
        scene.Objects = [.. objects];
        scene.Materials = [.. materials];

        scene.Texels = texels;
        scene.TextureInfo = [.. textureInfo];
        scene.BuildAreaLights();
        scene.GroundHeight = scene.FindGroundHeight();
        scene.Update(0.0);
        scene.CommitHistory();
        Console.WriteLine($"Scene: {ordered.Count / 3:N0} triangles, {materials.Count} materials, {textureInfo.Count} textures, {texels.Length * 4L / 1048576} MiB texels.");

        return scene;

        void AddObject(IEnumerable<SceneVertex> vertices, bool opaque, bool doubleSided, float sphereRadius = 0.0f)
        {
            uint first = (uint)ordered.Count;

            foreach (SceneVertex source in vertices)
            {
                SceneVertex vertex = source;
                vertex.Position.W = objects.Count;
                ordered.Add(vertex);
            }

            objects.Add(new()
            {
                Offset = new(0.0f, 0.0f, 0.0f, sphereRadius),
                Geometry = new(first, (uint)ordered.Count - first, opaque ? 1u : 0u, doubleSided ? 1u : 0u)
            });
        }
    }

    private static Vector4 GetTriangleTangent(Vector3 normal, Vector3 edge1, Vector3 edge2, Vector2 uv1, Vector2 uv2)
    {
        float determinant = (uv1.X * uv2.Y) - (uv1.Y * uv2.X);
        if (determinant is not 0.0f)
        {
            Vector3 tangent = ((edge1 * uv2.Y) - (edge2 * uv1.Y)) / determinant;
            tangent -= normal * Vector3.Dot(normal, tangent);

            if (tangent.LengthSquared() > 0.0f)
            {
                tangent = Vector3.Normalize(tangent);
                Vector3 bitangent = ((edge2 * uv1.X) - (edge1 * uv2.X)) / determinant;

                return new(tangent, Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0.0f ? -1 : 1);
            }
        }

        // Degenerate UVs have no defined tangent space; use a finite orthogonal frame.
        Vector3 axis = Math.Abs(normal.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;

        return new(Vector3.Normalize(Vector3.Cross(axis, normal)), 1.0f);
    }

    private static List<SceneVertex> CreateSphere(float radius, int materialIndex)
    {
        const int Segments = 64;
        const int Rings = 32;
        List<SceneVertex> result = [];

        for (int j = 0; j < Rings; j++)
        {
            for (int i = 0; i < Segments; i++)
            {
                if (j > 0)
                {
                    result.AddRange([CreateVertex(i, j), CreateVertex(i + 1, j), CreateVertex(i, j + 1)]);
                }

                if (j + 1 < Rings)
                {
                    result.AddRange([CreateVertex(i + 1, j), CreateVertex(i + 1, j + 1), CreateVertex(i, j + 1)]);
                }
            }
        }

        return result;

        SceneVertex CreateVertex(int x, int y)
        {
            float phi = x * MathF.Tau / Segments;
            float theta = y * MathF.PI / Rings;
            Vector3 n = new(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));

            return new()
            {
                Position = new(n * radius, 0.0f),
                Normal = new(n, materialIndex),
                Tangent = new(-MathF.Sin(phi), 0.0f, MathF.Cos(phi), 1.0f),
                UV = new((float)x / Segments, (float)y / Rings, 0.0f, 0.0f)
            };
        }
    }

    private static (List<SceneMaterial> Materials, uint[] Texels, List<TextureDescription> TextureInfo) ImportMaterials(ModelRoot model)
    {
        // Shared shader tables precede texture pixels. Keep the albedo layout in sync with generated GltfDirectionalAlbedo.slang (32 x 64 + 32).
        const int SrgbEntries = 256;
        const int AlbedoEntries = (32 * 64) + 32;
        byte[] albedo = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Assets", "Shaders", "GltfDirectionalAlbedo.bin"));
        if (albedo.Length != AlbedoEntries * sizeof(uint))
        {
            throw new InvalidDataException("The dielectric directional-albedo table has an unexpected size.");
        }

        int texelCount = SrgbEntries + AlbedoEntries;
        List<TextureDescription> textureInfo = [];
        Dictionary<(int, bool), int> images = [];
        List<(SharpGLTF.Memory.MemoryImage Image, bool Srgb, int Count)> pending = [];

        List<SceneMaterial> materials = [];
        foreach (Material material in model.LogicalMaterials)
        {
            MaterialChannel? color = material.FindChannel("BaseColor");
            MaterialChannel? metal = material.FindChannel("MetallicRoughness");
            MaterialChannel? normal = material.FindChannel("Normal");
            MaterialChannel? emissive = material.FindChannel("Emissive");
            materials.Add(new()
            {
                BaseColor = color?.Color ?? Vector4.One,
                EmissiveMetallic = new((emissive?.Color ?? Vector4.Zero).AsVector3() * GetScalar(emissive, "EmissiveStrength", 1.0f), GetScalar(metal, "MetallicFactor", 1.0f)),
                Parameters = new(GetScalar(metal, "RoughnessFactor", 1.0f), GetScalar(normal, "NormalScale", 1.0f), material.Alpha is AlphaMode.OPAQUE ? -1 : material.AlphaCutoff, material.DoubleSided ? 1 : 0),
                Textures = new(AddTexture(color, true), AddTexture(normal, false), AddTexture(metal, false), AddTexture(emissive, true))
            });
        }

        uint[] texels = new uint[textureInfo.Count is 0 ? texelCount + 1 : texelCount];

        // Texture RGB is 8-bit sRGB. The first 256 entries hold its exact decode values.
        for (int value = 0; value < SrgbEntries; value++)
        {
            float encoded = value / 255.0f;
            float linear = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
            texels[value] = BitConverter.SingleToUInt32Bits(linear);
        }

        // Float32 little-endian values, read with asfloat from the existing read-only GPU buffer on both backends (never per-thread arrays).
        System.Buffer.BlockCopy(albedo, 0, texels, SrgbEntries * sizeof(uint), albedo.Length);

        // Each texture owns a precomputed atlas range, preserving material indices.
        Parallel.For(0, pending.Count, i =>
        {
            using Stream stream = pending[i].Image.Open();
            TextureLoader.Load(stream, texels.AsSpan((int)textureInfo[i].Offset, pending[i].Count), pending[i].Srgb);
        });

        if (textureInfo.Count is 0)
        {
            textureInfo.Add(new((uint)texelCount, 1u, 1u, 1u));
            texels[texelCount] = uint.MaxValue;
        }

        return (materials, texels, textureInfo);

        static float GetScalar(MaterialChannel? channel, string name, float fallbackValue)
        {
            return channel?.Parameters.FirstOrDefault(parameter => string.Equals(parameter.Name, name, StringComparison.Ordinal))?.Value is float value ? value : fallbackValue;
        }

        int AddTexture(MaterialChannel? channel, bool srgb)
        {
            SharpGLTF.Schema2.Image? image = channel?.Texture?.PrimaryImage;
            if (image is null)
            {
                return -1;
            }

            if (channel!.Value.TextureCoordinate is not 0 || channel.Value.TextureTransform is not null)
            {
                throw new NotSupportedException("This scene requires transformed texture coordinates; the Sponza renderer uses TEXCOORD_0.");
            }

            if (images.TryGetValue((image.LogicalIndex, srgb), out int existing))
            {
                return existing;
            }

            int index = textureInfo.Count;
            using Stream stream = image.Content.Open();
            (TextureDescription description, int count) = TextureLoader.Describe(stream, (uint)texelCount);
            textureInfo.Add(description);
            pending.Add((image.Content, srgb, count));
            texelCount += count;
            images.Add((image.LogicalIndex, srgb), index);

            return index;
        }
    }
}
