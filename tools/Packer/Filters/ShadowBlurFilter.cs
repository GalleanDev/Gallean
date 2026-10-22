namespace Packer;

internal sealed class ShadowBlurFilter(double blurStrength) : ISpriteFilter
{
    private static readonly Guid FilterType = new("b7c9f1a4-3d52-4e18-9f0c-2a6b5d8e4713");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale >= 2;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Process(frame));
        }

        return result;
    }

    private AssetSprite Process(AssetSprite sprite)
    {
        int scale = sprite.Metadata.PixelScale;
        int logicalRadius = (int)Math.Ceiling(3 * blurStrength);
        int radius = logicalRadius * scale;
        float[] kernel = BuildGaussianKernel(blurStrength * scale, radius);

        int sourceWidth = sprite.Metadata.PixelWidth;
        int sourceHeight = sprite.Metadata.PixelHeight;
        int width = sourceWidth + (2 * radius);
        int height = sourceHeight + (2 * radius);

        float[] rows = BlurHorizontally(sprite.Pixels.Span, sourceWidth, sourceHeight, width, kernel, radius);
        byte[] pixels = BlurVertically(rows, width, sourceHeight, height, kernel, radius);

        AssetSpriteMetadata metadata = new(
            sprite.Metadata.Width + (2 * logicalRadius),
            sprite.Metadata.Height + (2 * logicalRadius),
            width,
            height,
            sprite.Metadata.OffsetX - logicalRadius,
            sprite.Metadata.OffsetY - logicalRadius,
            Additive: sprite.Metadata.Additive);

        return Trim(new AssetSprite(metadata, pixels), scale);
    }

    private static float[] BuildGaussianKernel(double sigma, int radius)
    {
        float[] kernel = new float[(2 * radius) + 1];
        double total = 0;

        for (int step = -radius; step <= radius; step++)
        {
            double weight = Math.Exp(-(step * step) / (2 * sigma * sigma));
            kernel[step + radius] = (float)weight;
            total += weight;
        }

        for (int index = 0; index < kernel.Length; index++)
        {
            kernel[index] = (float)(kernel[index] / total);
        }

        return kernel;
    }

    private static float[] BlurHorizontally(ReadOnlySpan<byte> pixels, int sourceWidth, int sourceHeight, int width, float[] kernel, int radius)
    {
        float[] result = new float[width * sourceHeight * 4];

        for (int y = 0; y < sourceHeight; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float red = 0;
                float green = 0;
                float blue = 0;
                float alpha = 0;

                int first = Math.Max(0, x - (2 * radius));
                int last = Math.Min(sourceWidth - 1, x);

                for (int sourceX = first; sourceX <= last; sourceX++)
                {
                    int sample = ((y * sourceWidth) + sourceX) * 4;
                    float weight = kernel[sourceX - x + (2 * radius)];
                    float weightedAlpha = pixels[sample + 3] * weight;

                    red += pixels[sample] * weightedAlpha / byte.MaxValue;
                    green += pixels[sample + 1] * weightedAlpha / byte.MaxValue;
                    blue += pixels[sample + 2] * weightedAlpha / byte.MaxValue;
                    alpha += weightedAlpha;
                }

                int offset = ((y * width) + x) * 4;

                result[offset] = red;
                result[offset + 1] = green;
                result[offset + 2] = blue;
                result[offset + 3] = alpha;
            }
        }

        return result;
    }

    private static byte[] BlurVertically(float[] rows, int width, int sourceHeight, int height, float[] kernel, int radius)
    {
        byte[] result = new byte[width * height * 4];

        for (int y = 0; y < height; y++)
        {
            int first = Math.Max(0, y - (2 * radius));
            int last = Math.Min(sourceHeight - 1, y);

            for (int x = 0; x < width; x++)
            {
                float red = 0;
                float green = 0;
                float blue = 0;
                float alpha = 0;

                for (int sourceY = first; sourceY <= last; sourceY++)
                {
                    int sample = ((sourceY * width) + x) * 4;
                    float weight = kernel[sourceY - y + (2 * radius)];

                    red += rows[sample] * weight;
                    green += rows[sample + 1] * weight;
                    blue += rows[sample + 2] * weight;
                    alpha += rows[sample + 3] * weight;
                }

                byte roundedAlpha = ToByte(alpha);
                if (roundedAlpha > 0)
                {
                    int offset = ((y * width) + x) * 4;

                    result[offset] = ToByte(red * byte.MaxValue / alpha);
                    result[offset + 1] = ToByte(green * byte.MaxValue / alpha);
                    result[offset + 2] = ToByte(blue * byte.MaxValue / alpha);
                    result[offset + 3] = roundedAlpha;
                }
            }
        }

        return result;

        static byte ToByte(float value)
        {
            return (byte)Math.Clamp(MathF.Round(value), 0, byte.MaxValue);
        }
    }

    private static AssetSprite Trim(AssetSprite sprite, int scale)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;
        ReadOnlySpan<byte> pixels = sprite.Pixels.Span;

        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[(((y * width) + x) * 4) + 3] > 0)
                {
                    minX = Math.Min(minX, x);
                    maxX = Math.Max(maxX, x);
                    minY = Math.Min(minY, y);
                    maxY = Math.Max(maxY, y);
                }
            }
        }

        if (maxX < minX)
        {
            AssetSpriteMetadata empty = new(
                1, 1, 1, 1, sprite.Metadata.OffsetX, sprite.Metadata.OffsetY, sprite.Metadata.Additive);

            return new AssetSprite(empty, new byte[4]);
        }

        int left = AlignDown(minX, scale);
        int top = AlignDown(minY, scale);
        int right = AlignUp(maxX + 1, scale);
        int bottom = AlignUp(maxY + 1, scale);

        return SpriteOperations.Crop(sprite, left, top, right - left, bottom - top);
    }

    private static int AlignDown(int value, int scale)
    {
        return value - (value % scale);
    }

    private static int AlignUp(int value, int scale)
    {
        return AlignDown(value + scale - 1, scale);
    }
}
