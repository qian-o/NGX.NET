using System.Numerics;
using SharpGLTF.Schema2;
using Showcase.Helpers;
using Showcase.Models;

namespace Showcase;

internal sealed partial class Scene
{
    private static (List<SceneMaterial> Materials, List<uint> Texels, List<TextureDescription> TextureInfo) ImportMaterials(ModelRoot model)
    {
        // Texture RGB is 8-bit sRGB. Cache all 256 exact decode values once so
        // bilinear/trilinear samples do not repeat three pow operations per texel.
        List<uint> texels = new(256);

        for (int value = 0; value < 256; value++)
        {
            float encoded = value / 255f;
            float linear = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
            texels.Add(BitConverter.SingleToUInt32Bits(linear));
        }

        List<TextureDescription> textureInfo = [];
        Dictionary<(int, bool), int> images = [];

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
            textureInfo.Add(TextureLoader.Load(image.Content.Content.ToArray(), texels, srgb));
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

            float Scalar(MaterialChannel? channel, string name, float fallbackValue) =>
                channel?.Parameters.FirstOrDefault(parameter => parameter.Name == name)?.Value is float value ? value : fallbackValue;

            materials.Add(new()
            {
                BaseColor = color?.Color ?? Vector4.One,
                EmissiveMetallic = new((emissive?.Color ?? Vector4.Zero).AsVector3() * Scalar(emissive, "EmissiveStrength", 1), Scalar(metal, "MetallicFactor", 1)),
                Parameters = new(
                    Scalar(metal, "RoughnessFactor", 1),
                    Scalar(normal, "NormalScale", 1),
                    material.Alpha == AlphaMode.OPAQUE ? -1 : material.AlphaCutoff,
                    material.DoubleSided ? 1 : 0),
                Textures = new(Texture(color, true), Texture(normal, false), Texture(metal, false), Texture(emissive, true))
            });
        }

        return (materials, texels, textureInfo);
    }
}
