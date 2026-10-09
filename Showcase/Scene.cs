using System.Numerics;
using SharpGLTF.Schema2;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal class Scene
{
    private double animationTime;
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

    public float RayEpsilon => Scale * 1e-5f;

    public Matrix4x4 GetSunViewProjection(Vector3 direction)
    {
        Vector3 center = (Minimum + Maximum) * 0.5f;
        Matrix4x4 view = Matrix4x4.CreateLookAt(center + (direction * Scale), center, Vector3.UnitY);

        return view * Matrix4x4.CreateOrthographic(Scale, Scale, RayEpsilon, Scale * 2);
    }

    public void Update(double delta)
    {
        animationTime += delta;
        Vector3 center = (Minimum + Maximum) * 0.5f;
        for (int i = staticObjectCount; i < Objects.Length; i++)
        {
            float phase = ((float)animationTime * 0.7f) + ((i - staticObjectCount) * MathF.PI);
            Vector3 p = new(center.X + (MathF.Sin(phase) * Scale * 0.09f), GroundHeight + (Scale * 0.04f), center.Z + (MathF.Cos(phase) * Scale * 0.018f));
            Objects[i].Offset = new(p, Objects[i].Offset.W);
        }
    }

    public void CommitHistory()
    {
        for (int i = 0; i < Objects.Length; i++)
        {
            Objects[i].PreviousOffset = Objects[i].Offset;
        }
    }

    private float FindGroundHeight()
    {
        // The asset's bounding box includes its foundation below the walking surface.
        // Locate the atrium floor with a downward ray through the scene's center.
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
            if (determinant == 0)
            {
                continue;
            }

            Vector3 relative = origin - a;
            float u = Vector3.Dot(relative, p) / determinant;
            Vector3 q = Vector3.Cross(relative, edge1);
            float v = Vector3.Dot(direction, q) / determinant;
            float distance = Vector3.Dot(edge2, q) / determinant;
            if (u >= 0 && v >= 0 && u + v <= 1 && distance >= 0)
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
            Parameters = new(0.8f, 1, -1, 0),
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
                throw new InvalidDataException($"Non-invertible scene transform: {node.Name}");
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
                    int second = handedness < 0 ? c : b;
                    int third = handedness < 0 ? b : c;
                    Vector3 pa = Vector3.Transform(positions[a], world);
                    Vector3 pb = Vector3.Transform(positions[second], world);
                    Vector3 pc = Vector3.Transform(positions[third], world);
                    Vector3 edge1 = pb - pa;
                    Vector3 edge2 = pc - pa;
                    Vector3 faceNormal = Vector3.Cross(edge1, edge2);
                    if (faceNormal.LengthSquared() == 0)
                    {
                        // Collapsed triangles have no coverage or valid geometric normal.
                        continue;
                    }

                    faceNormal = Vector3.Normalize(faceNormal);
                    Vector2 uv0 = uv?[a] ?? Vector2.Zero;
                    Vector2 uv1 = (uv?[second] ?? Vector2.Zero) - uv0;
                    Vector2 uv2 = (uv?[third] ?? Vector2.Zero) - uv0;
                    for (int corner = 0; corner < 3; corner++)
                    {
                        int index = corner switch { 0 => a, 1 => second, _ => third };
                        Vector3 n = normals is null ? faceNormal : Vector3.TransformNormal(normals[index], normalMatrix);
                        n = n.LengthSquared() > 0 ? Vector3.Normalize(n) : faceNormal;
                        Vector4 t = default;
                        if (tangents is not null)
                        {
                            Vector3 direction = Vector3.TransformNormal(tangents[index].AsVector3(), world);
                            if (direction.LengthSquared() > 0)
                            {
                                t = new(Vector3.Normalize(direction), tangents[index].W * handedness);
                            }
                        }

                        if (Vector3.Cross(n, t.AsVector3()).LengthSquared() == 0)
                        {
                            // Repair invalid authored tangents from the UV basis rather
                            // than rotating the normal map into an arbitrary frame.
                            t = TriangleTangent(n, edge1, edge2, uv1, uv2);
                        }

                        staticVertices.Add(new()
                        {
                            Position = new(corner switch { 0 => pa, 1 => pb, _ => pc }, 0),
                            Normal = new(n, primitive.Material?.LogicalIndex ?? fallback),
                            Tangent = t,
                            UV = new(uv?[index] ?? Vector2.Zero, 0, 0)
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

        void AddObject(IEnumerable<SceneVertex> vertices, bool opaque, bool doubleSided, float sphereRadius = 0)
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
                Offset = new(0, 0, 0, sphereRadius),
                Geometry = new(first, (uint)ordered.Count - first, opaque ? 1u : 0u, doubleSided ? 1u : 0u)
            });
        }

        // A triangle's three vertices share a material, so grouping preserves
        // whole triangles. Homogeneous opacity/sidedness enables hardware hit handling.
        foreach (IGrouping<(bool Opaque, bool DoubleSided), SceneVertex> group in staticVertices.GroupBy(vertex =>
        {
            SceneMaterial material = materials[(int)vertex.Normal.W];

            return (Opaque: material.Parameters.Z < 0, DoubleSided: material.Parameters.W != 0);
        }))
        {
            AddObject(group, group.Key.Opaque, group.Key.DoubleSided);
        }

        scene.staticObjectCount = objects.Count;

        // Gold and neutral chromium provide warm and untinted polished reflections.
        // Base colors are linear reflectance; both use the same surface roughness.
        const float PolishedMetalRoughness = 0.08f;
        for (int i = 0; i < 2; i++)
        {
            int materialIndex = materials.Count;
            materials.Add(new()
            {
                BaseColor = i is 0 ? new(0.82f, 0.56f, 0.26f, 1) : new(0.55f, 0.56f, 0.55f, 1),
                EmissiveMetallic = new(0, 0, 0, 1),
                Parameters = new(PolishedMetalRoughness, 1, -1, 0),
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
        scene.GroundHeight = scene.FindGroundHeight();
        scene.Update(0);
        scene.CommitHistory();
        Console.WriteLine($"Scene: {ordered.Count / 3:N0} triangles, {materials.Count} materials, {textureInfo.Count} textures, {texels.Length * 4L / 1048576} MiB texels.");

        return scene;
    }

    private static Vector4 TriangleTangent(Vector3 normal, Vector3 edge1, Vector3 edge2, Vector2 uv1, Vector2 uv2)
    {
        float determinant = (uv1.X * uv2.Y) - (uv1.Y * uv2.X);
        if (determinant != 0)
        {
            Vector3 tangent = ((edge1 * uv2.Y) - (edge2 * uv1.Y)) / determinant;
            tangent -= normal * Vector3.Dot(normal, tangent);

            if (tangent.LengthSquared() > 0)
            {
                tangent = Vector3.Normalize(tangent);
                Vector3 bitangent = ((edge2 * uv1.X) - (edge1 * uv2.X)) / determinant;

                return new(tangent, Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0 ? -1 : 1);
            }
        }

        // Degenerate UVs have no defined tangent space; use a finite orthogonal frame.
        Vector3 axis = Math.Abs(normal.Y) < 0.99f ? Vector3.UnitY : Vector3.UnitX;

        return new(Vector3.Normalize(Vector3.Cross(axis, normal)), 1);
    }

    private static List<SceneVertex> CreateSphere(float radius, int materialIndex)
    {
        const int Segments = 64;
        const int Rings = 32;
        List<SceneVertex> result = [];

        SceneVertex Vertex(int x, int y)
        {
            float phi = x * MathF.Tau / Segments;
            float theta = y * MathF.PI / Rings;
            Vector3 n = new(MathF.Sin(theta) * MathF.Cos(phi), MathF.Cos(theta), MathF.Sin(theta) * MathF.Sin(phi));

            return new()
            {
                Position = new(n * radius, 0),
                Normal = new(n, materialIndex),
                Tangent = new(-MathF.Sin(phi), 0, MathF.Cos(phi), 1),
                UV = new((float)x / Segments, (float)y / Rings, 0, 0)
            };
        }

        for (int y = 0; y < Rings; y++)
        {
            for (int x = 0; x < Segments; x++)
            {
                if (y > 0)
                {
                    result.AddRange([Vertex(x, y), Vertex(x + 1, y), Vertex(x, y + 1)]);
                }

                if (y + 1 < Rings)
                {
                    result.AddRange([Vertex(x + 1, y), Vertex(x + 1, y + 1), Vertex(x, y + 1)]);
                }
            }
        }

        return result;
    }

    private static (List<SceneMaterial> Materials, uint[] Texels, List<TextureDescription> TextureInfo) ImportMaterials(ModelRoot model)
    {
        int texelCount = 256;
        List<TextureDescription> textureInfo = [];
        Dictionary<(int, bool), int> images = [];
        List<(SharpGLTF.Memory.MemoryImage Image, bool Srgb, int Count)> pending = [];

        int Texture(MaterialChannel? channel, bool srgb)
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

        List<SceneMaterial> materials = [];
        foreach (Material material in model.LogicalMaterials)
        {
            MaterialChannel? color = material.FindChannel("BaseColor");
            MaterialChannel? metal = material.FindChannel("MetallicRoughness");
            MaterialChannel? normal = material.FindChannel("Normal");
            MaterialChannel? emissive = material.FindChannel("Emissive");
            float Scalar(MaterialChannel? channel, string name, float fallbackValue)
            {
                return channel?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Value is float value ? value : fallbackValue;
            }

            materials.Add(new()
            {
                BaseColor = color?.Color ?? Vector4.One,
                EmissiveMetallic = new((emissive?.Color ?? Vector4.Zero).AsVector3() * Scalar(emissive, "EmissiveStrength", 1), Scalar(metal, "MetallicFactor", 1)),
                Parameters = new(Scalar(metal, "RoughnessFactor", 1), Scalar(normal, "NormalScale", 1), material.Alpha is AlphaMode.OPAQUE ? -1 : material.AlphaCutoff, material.DoubleSided ? 1 : 0),
                Textures = new(Texture(color, true), Texture(normal, false), Texture(metal, false), Texture(emissive, true))
            });
        }

        uint[] texels = new uint[textureInfo.Count is 0 ? texelCount + 1 : texelCount];

        // Texture RGB is 8-bit sRGB. The first 256 entries hold its exact decode values.
        for (int value = 0; value < 256; value++)
        {
            float encoded = value / 255.0f;
            float linear = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
            texels[value] = BitConverter.SingleToUInt32Bits(linear);
        }

        // Each texture owns a precomputed atlas range, preserving material indices.
        Parallel.For(0, pending.Count, i =>
        {
            using Stream stream = pending[i].Image.Open();
            TextureLoader.Load(stream, texels.AsSpan((int)textureInfo[i].Offset, pending[i].Count), pending[i].Srgb);
        });

        if (textureInfo.Count is 0)
        {
            textureInfo.Add(new((uint)texelCount, 1, 1, 1));
            texels[texelCount] = uint.MaxValue;
        }

        return (materials, texels, textureInfo);
    }
}
