using System.Numerics;
using System.Runtime.InteropServices;
using Showcase.Models;
using StbImageSharp;

namespace Showcase.Helpers;

internal static class TextureLoader
{
    private static readonly float[] linearColors = CreateLinearColors();

    public static TextureDescription Load(byte[] data, List<uint> texels, bool srgb)
    {
        ImageResult image = ImageResult.FromMemory(data, ColorComponents.RedGreenBlueAlpha);
        int offset = texels.Count;
        int width = image.Width;
        int height = image.Height;
        int mipCount = 0;
        byte[] pixels = image.Data;

        while (true)
        {
            texels.AddRange(MemoryMarshal.Cast<byte, uint>(pixels));
            mipCount++;

            if (width == 1 && height == 1)
            {
                break;
            }

            pixels = Downsample(pixels, width, height, srgb);
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }

        return new((uint)offset, (uint)image.Width, (uint)image.Height, (uint)mipCount);
    }

    private static byte[] Downsample(byte[] pixels, int width, int height, bool srgb)
    {
        int nextWidth = Math.Max(1, width / 2);
        int nextHeight = Math.Max(1, height / 2);
        byte[] result = new byte[nextWidth * nextHeight * 4];
        float scale = Math.Max((float)nextWidth / width, (float)nextHeight / height);
        int scaledWidth = (int)MathF.Ceiling(width * scale);
        int scaledHeight = (int)MathF.Ceiling(height * scale);
        int cropX = -(int)MathF.Round((nextWidth - width * scale) / 2);
        int cropY = -(int)MathF.Round((nextHeight - height * scale) / 2);

        // Filter each box in linear light with premultiplied alpha so transparent
        // texels do not add color fringes. Retain the centered crop when rounding
        // mip dimensions changes the source aspect ratio.
        for (int y = 0; y < nextHeight; y++)
        {
            int firstY = (int)Math.Floor((double)(y + cropY) * height / scaledHeight + 0.5);
            int lastY = Math.Min(height, (int)Math.Floor((double)(y + cropY + 1) * height / scaledHeight + 0.5));

            for (int x = 0; x < nextWidth; x++)
            {
                int firstX = (int)Math.Floor((double)(x + cropX) * width / scaledWidth + 0.5);
                int lastX = Math.Min(width, (int)Math.Floor((double)(x + cropX + 1) * width / scaledWidth + 0.5));
                Vector4 sum = Vector4.Zero;

                for (int sourceY = firstY; sourceY < lastY; sourceY++)
                {
                    for (int sourceX = firstX; sourceX < lastX; sourceX++)
                    {
                        int index = (sourceY * width + sourceX) * 4;
                        float alpha = pixels[index + 3] / 255f;
                        Vector3 color = srgb
                            ? new(linearColors[pixels[index]], linearColors[pixels[index + 1]], linearColors[pixels[index + 2]])
                            : new(pixels[index] / 255f, pixels[index + 1] / 255f, pixels[index + 2] / 255f);
                        sum += new Vector4(color * alpha, alpha);
                    }
                }

                Vector3 average = sum.W > 0 ? new Vector3(sum.X, sum.Y, sum.Z) / sum.W : Vector3.Zero;
                int target = (y * nextWidth + x) * 4;
                result[target] = Encode(average.X, srgb);
                result[target + 1] = Encode(average.Y, srgb);
                result[target + 2] = Encode(average.Z, srgb);
                result[target + 3] = Encode(sum.W / ((lastX - firstX) * (lastY - firstY)), false);
            }
        }

        return result;
    }

    private static byte Encode(float value, bool srgb)
    {
        if (srgb)
        {
            value = value <= 0.0031308f ? value * 12.92f : 1.055f * MathF.Pow(value, 1 / 2.4f) - 0.055f;
        }

        return (byte)Math.Clamp((int)MathF.Round(value * 255), 0, 255);
    }

    private static float[] CreateLinearColors()
    {
        float[] values = new float[256];

        for (int i = 0; i < values.Length; i++)
        {
            float encoded = i / 255f;
            values[i] = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
        }

        return values;
    }
}
