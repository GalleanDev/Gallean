using System.Numerics;

namespace Packer;

internal sealed class SpellIconFilter : ISpriteFilter
{
    private const float OpaqueLevel = 0.5f;
    private const float SpillTolerance = 40.0f;
    private const float SpillTransparency = 0.4f;
    private const int ForegroundRadius = 2;

    private static readonly Guid FilterType = new("8f3b2c61-d47e-4a19-b5c8-1e6a9d07f2b4");

    private static readonly Vector3 KeyColor = new(196, 174, 140);

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return true;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Filter(frame));
        }

        return result;
    }

    private static AssetSprite Filter(AssetSprite sprite)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;
        int scale = sprite.Metadata.PixelScale;
        byte[] pixels = sprite.Pixels.ToArray();
        bool[] opaque = new bool[width * height];

        for (int index = 0; index < opaque.Length; index++)
        {
            opaque[index] = pixels[(index * 4) + 3] > OpaqueLevel * byte.MaxValue;
        }

        bool[] boundary = GetBoundary(opaque, width, height, scale);
        List<Vector3> interiorColors = [];

        for (int index = 0; index < opaque.Length; index++)
        {
            if (opaque[index] && !boundary[index])
            {
                interiorColors.Add(ReadColor(pixels, index));
            }
        }

        if (interiorColors.Count <= 0)
        {
            return sprite;
        }

        Vector3 fallback = GetMedian(interiorColors);

        byte[] result = [.. pixels];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                if (boundary[index] && pixels[(index * 4) + 3] > 0)
                {
                    Vector3 foreground = GetForeground(pixels, opaque, boundary, width, height, x, y, ForegroundRadius * scale, fallback);

                    Despill(result, index, foreground);
                }
            }
        }

        return sprite with { Pixels = result };
    }

    private static void Despill(byte[] pixels, int index, Vector3 foreground)
    {
        Vector3 color = ReadColor(pixels, index);
        Vector3 spillAxis = KeyColor - foreground;
        float spill = Math.Clamp(Vector3.Dot(color - foreground, spillAxis) / Math.Max(spillAxis.LengthSquared(), 1), 0, 1);
        float deviation = Vector3.Distance(color, foreground + (spill * spillAxis));

        if (deviation < SpillTolerance)
        {
            Vector3 despilled = color - (spill * spillAxis);

            pixels[index * 4] = ToByte(despilled.X);
            pixels[(index * 4) + 1] = ToByte(despilled.Y);
            pixels[(index * 4) + 2] = ToByte(despilled.Z);
            pixels[(index * 4) + 3] = ToByte(pixels[(index * 4) + 3] * (1 - (SpillTransparency * spill)));
        }
    }

    private static bool[] GetBoundary(bool[] opaque, int width, int height, int radius)
    {
        bool[] boundary = new bool[opaque.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                boundary[(y * width) + x] = IsNearTransparency(opaque, width, height, x, y, radius);
            }
        }

        return boundary;
    }

    private static bool IsNearTransparency(bool[] opaque, int width, int height, int x, int y, int radius)
    {
        for (int sampleY = y - radius; sampleY <= y + radius; sampleY++)
        {
            for (int sampleX = x - radius; sampleX <= x + radius; sampleX++)
            {
                if (sampleX < 0 || sampleY < 0 || sampleX >= width || sampleY >= height)
                {
                    return true;
                }

                if (!opaque[(sampleY * width) + sampleX])
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static Vector3 GetForeground(
        byte[] pixels,
        bool[] opaque,
        bool[] boundary,
        int width,
        int height,
        int x,
        int y,
        int radius,
        Vector3 fallback)
    {
        Vector3 total = Vector3.Zero;
        int count = 0;

        for (int sampleY = Math.Max(0, y - radius); sampleY <= Math.Min(height - 1, y + radius); sampleY++)
        {
            for (int sampleX = Math.Max(0, x - radius); sampleX <= Math.Min(width - 1, x + radius); sampleX++)
            {
                int sample = (sampleY * width) + sampleX;

                if (opaque[sample] && !boundary[sample])
                {
                    total += ReadColor(pixels, sample);
                    count++;
                }
            }
        }

        if (count > 0)
        {
            return total / count;
        }
        else
        {
            return fallback;
        }
    }

    private static Vector3 GetMedian(List<Vector3> colors)
    {
        List<float> red = [.. colors.Select(color => color.X).Order()];
        List<float> green = [.. colors.Select(color => color.Y).Order()];
        List<float> blue = [.. colors.Select(color => color.Z).Order()];
        int middle = colors.Count / 2;

        return new Vector3(red[middle], green[middle], blue[middle]);
    }

    private static Vector3 ReadColor(byte[] pixels, int index)
    {
        return new Vector3(pixels[index * 4], pixels[(index * 4) + 1], pixels[(index * 4) + 2]);
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(MathF.Round(value), byte.MinValue, byte.MaxValue);
    }
}
