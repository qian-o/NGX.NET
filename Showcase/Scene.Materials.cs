using System.Numerics;
using SharpGLTF.Schema2;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal sealed partial class Scene
{
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

            if (channel!.Value.TextureCoordinate != 0 || channel.Value.TextureTransform is not null)
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

            float Scalar(MaterialChannel? channel, string name, float fallbackValue) => channel?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Value is float value ? value : fallbackValue;

            materials.Add(new()
            {
                BaseColor = color?.Color ?? Vector4.One,
                EmissiveMetallic = new((emissive?.Color ?? Vector4.Zero).AsVector3() * Scalar(emissive, "EmissiveStrength", 1), Scalar(metal, "MetallicFactor", 1)),
                Parameters = new(Scalar(metal, "RoughnessFactor", 1), Scalar(normal, "NormalScale", 1), material.Alpha == AlphaMode.OPAQUE ? -1 : material.AlphaCutoff, material.DoubleSided ? 1 : 0),
                Textures = new(Texture(color, true), Texture(normal, false), Texture(metal, false), Texture(emissive, true))
            });
        }

        uint[] texels = new uint[textureInfo.Count == 0 ? texelCount + 1 : texelCount];

        // Texture RGB is 8-bit sRGB. The first 256 entries hold its exact decode values.
        for (int value = 0; value < 256; value++)
        {
            float encoded = value / 255f;
            float linear = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
            texels[value] = BitConverter.SingleToUInt32Bits(linear);
        }

        // Each texture owns a precomputed atlas range, preserving material indices.
        Parallel.For(0, pending.Count, i =>
        {
            using Stream stream = pending[i].Image.Open();
            TextureLoader.Load(stream, texels.AsSpan((int)textureInfo[i].Offset, pending[i].Count), pending[i].Srgb);
        });

        if (textureInfo.Count == 0)
        {
            textureInfo.Add(new((uint)texelCount, 1, 1, 1));
            texels[texelCount] = uint.MaxValue;
        }

        return (materials, texels, textureInfo);
    }
}
