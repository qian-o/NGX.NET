using System.Numerics;
using System.Runtime.InteropServices;
using Showcase.Models;
using StbImageSharp;

namespace Showcase.Helpers;

internal static class TextureLoader
{
    private static readonly float[] LinearColors = CreateLinearColors();

    public static (TextureDescription Description, int TexelCount) Describe(Stream data, uint offset)
    {
        ImageInfo image = ImageInfo.FromStream(data) ?? throw new InvalidDataException("Could not read texture dimensions.");
        int count = 0;
        int mips = 0;
        for (int width = image.Width, height = image.Height; ; width = Math.Max(1, width / 2), height = Math.Max(1, height / 2))
        {
            count += width * height;
            mips++;

            if (width is 1 && height is 1)
            {
                return (new(offset, (uint)image.Width, (uint)image.Height, (uint)mips), count);
            }
        }
    }

    public static void Load(Stream data, Span<uint> texels, bool srgb)
    {
        ImageResult image = ImageResult.FromStream(data, ColorComponents.RedGreenBlueAlpha);
        int offset = 0;
        int width = image.Width;
        int height = image.Height;
        byte[] pixels = image.Data;
        while (true)
        {
            MemoryMarshal.Cast<byte, uint>(pixels).CopyTo(texels[offset..]);
            offset += width * height;

            if (width is 1 && height is 1)
            {
                break;
            }

            pixels = Downsample(pixels, width, height, srgb);
            width = Math.Max(1, width / 2);
            height = Math.Max(1, height / 2);
        }
    }

    private static byte[] Downsample(ReadOnlySpan<byte> pixels, int width, int height, bool srgb)
    {
        int nextWidth = Math.Max(1, width / 2);
        int nextHeight = Math.Max(1, height / 2);
        byte[] result = new byte[nextWidth * nextHeight * 4];
        double scaleX = (double)width / nextWidth;
        double scaleY = (double)height / nextHeight;

        // Filter each box in linear light with premultiplied alpha so transparent texels do not add color fringes. Map the complete source extent on each axis: cropping loses edge texels for odd sizes and 1 x N tails.
        for (int y = 0; y < nextHeight; y++)
        {
            double top = y * scaleY;
            double bottom = (y + 1) * scaleY;
            int firstY = (int)Math.Floor(top);
            int lastY = Math.Min(height, (int)Math.Ceiling(bottom));
            for (int x = 0; x < nextWidth; x++)
            {
                double left = x * scaleX;
                double right = (x + 1) * scaleX;
                int firstX = (int)Math.Floor(left);
                int lastX = Math.Min(width, (int)Math.Ceiling(right));
                Vector4 sum = Vector4.Zero;
                for (int sourceY = firstY; sourceY < lastY; sourceY++)
                {
                    for (int sourceX = firstX; sourceX < lastX; sourceX++)
                    {
                        float weight = (float)((Math.Min(sourceX + 1, right) - Math.Max(sourceX, left)) * (Math.Min(sourceY + 1, bottom) - Math.Max(sourceY, top)));
                        int index = ((sourceY * width) + sourceX) * 4;
                        float alpha = pixels[index + 3] / 255.0f;
                        Vector3 color = srgb ? new(LinearColors[pixels[index]], LinearColors[pixels[index + 1]], LinearColors[pixels[index + 2]]) : new(pixels[index] / 255.0f, pixels[index + 1] / 255.0f, pixels[index + 2] / 255.0f);
                        sum += new Vector4(color * alpha, alpha) * weight;
                    }
                }

                Vector3 average = sum.W > 0.0f ? new Vector3(sum.X, sum.Y, sum.Z) / sum.W : Vector3.Zero;
                int target = ((y * nextWidth) + x) * 4;
                result[target] = Encode(average.X, srgb);
                result[target + 1] = Encode(average.Y, srgb);
                result[target + 2] = Encode(average.Z, srgb);
                result[target + 3] = Encode(sum.W / (float)(scaleX * scaleY), false);
            }
        }

        return result;
    }

    private static byte Encode(float value, bool srgb)
    {
        if (srgb)
        {
            value = value <= 0.0031308f ? value * 12.92f : (1.055f * MathF.Pow(value, 1.0f / 2.4f)) - 0.055f;
        }

        return (byte)Math.Clamp((int)MathF.Round(value * 255.0f), 0, 255);
    }

    private static float[] CreateLinearColors()
    {
        float[] values = new float[256];
        for (int i = 0; i < values.Length; i++)
        {
            float encoded = i / 255.0f;
            values[i] = encoded <= 0.04045f ? encoded / 12.92f : MathF.Pow((encoded + 0.055f) / 1.055f, 2.4f);
        }

        return values;
    }
}
