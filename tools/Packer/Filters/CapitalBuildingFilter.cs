namespace Packer;

internal sealed class CapitalBuildingFilter(AssetSpriteKey.CapitalBackground background) : ISpriteFilter
{
    private const int SceneWidth = 640;
    private const int SceneHeight = 600;
    private const float MatchDistance = 6;
    private const float MismatchDistance = 22;
    private const float FadeWidth = 3;

    private static readonly float[] BlurKernel = [0.0044f, 0.054f, 0.242f, 0.3991f, 0.242f, 0.054f, 0.0044f];

    private static readonly Guid FilterType = new("e6466f70-621e-44cb-903c-71e1834c3310");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale >= 2;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        IReadOnlyList<AssetSprite> sources = context.Source.ReadFrames(context.Key)!;
        IReadOnlyList<AssetSprite> backdrops = context.Source.ReadFrames(background)!;

        float[] coverage = GetCoverage(sources, backdrops[0]);
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Process(frame, coverage));
        }

        return result;
    }

    private static float[] GetCoverage(IReadOnlyList<AssetSprite> sources, AssetSprite backdrop)
    {
        (int width, int height, _, _, int offsetX, int offsetY, _) = sources[0].Metadata;
        float[] mismatch = new float[width * height];

        foreach (AssetSprite source in sources)
        {
            float[] distances = Blur(GetDistances(source, backdrop), width, height);

            for (int index = 0; index < mismatch.Length; index++)
            {
                mismatch[index] = Math.Max(mismatch[index], distances[index]);
            }
        }

        bool fadeLeft = offsetX > 0;
        bool fadeTop = offsetY > 0;
        bool fadeRight = offsetX + width < SceneWidth;
        bool fadeBottom = offsetY + height < SceneHeight;
        float[] coverage = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int distance = int.MaxValue;

                if (fadeLeft)
                {
                    distance = Math.Min(distance, x);
                }

                if (fadeRight)
                {
                    distance = Math.Min(distance, width - 1 - x);
                }

                if (fadeTop)
                {
                    distance = Math.Min(distance, y);
                }

                if (fadeBottom)
                {
                    distance = Math.Min(distance, height - 1 - y);
                }

                int index = (y * width) + x;
                float weight = Smooth((mismatch[index] - MatchDistance) / (MismatchDistance - MatchDistance));

                coverage[index] = weight * Smooth(distance / FadeWidth);
            }
        }

        return coverage;
    }

    private static float[] GetDistances(AssetSprite source, AssetSprite backdrop)
    {
        int width = source.Metadata.Width;
        int height = source.Metadata.Height;
        int backdropWidth = backdrop.Metadata.Width;
        int left = source.Metadata.OffsetX - backdrop.Metadata.OffsetX;
        int top = source.Metadata.OffsetY - backdrop.Metadata.OffsetY;
        ReadOnlySpan<byte> pixels = source.Pixels.Span;
        ReadOnlySpan<byte> backdropPixels = backdrop.Pixels.Span;
        float[] distances = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int offset = ((y * width) + x) * 4;
                int backdropOffset = (((top + y) * backdropWidth) + left + x) * 4;
                float red = pixels[offset] - backdropPixels[backdropOffset];
                float green = pixels[offset + 1] - backdropPixels[backdropOffset + 1];
                float blue = pixels[offset + 2] - backdropPixels[backdropOffset + 2];

                distances[(y * width) + x] = MathF.Sqrt((red * red) + (green * green) + (blue * blue));
            }
        }

        return distances;
    }

    private static float[] Blur(float[] values, int width, int height)
    {
        int radius = BlurKernel.Length / 2;
        float[] rows = new float[values.Length];
        float[] result = new float[values.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;

                for (int step = -radius; step <= radius; step++)
                {
                    int sampleX = Math.Clamp(x + step, 0, width - 1);
                    sum += values[(y * width) + sampleX] * BlurKernel[step + radius];
                }

                rows[(y * width) + x] = sum;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sum = 0;

                for (int step = -radius; step <= radius; step++)
                {
                    int sampleY = Math.Clamp(y + step, 0, height - 1);
                    sum += rows[(sampleY * width) + x] * BlurKernel[step + radius];
                }

                result[(y * width) + x] = sum;
            }
        }

        return result;
    }

    private static AssetSprite Process(AssetSprite sprite, float[] coverage)
    {
        AssetSpriteMetadata metadata = sprite.Metadata;
        int width = metadata.PixelWidth;
        int height = metadata.PixelHeight;
        int scale = metadata.PixelScale;
        byte[] pixels = sprite.Pixels.ToArray();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float sourceX = ((x + 0.5f) / scale) - 0.5f;
                float sourceY = ((y + 0.5f) / scale) - 0.5f;
                float weight = Sample(coverage, metadata.Width, metadata.Height, sourceX, sourceY);
                int offset = (((y * width) + x) * 4) + 3;

                pixels[offset] = (byte)MathF.Round(pixels[offset] * weight);
            }
        }

        return sprite with { Pixels = pixels };
    }

    private static float Sample(float[] values, int width, int height, float x, float y)
    {
        int left = (int)MathF.Floor(x);
        int top = (int)MathF.Floor(y);
        float fractionX = x - left;
        float fractionY = y - top;
        float upper = Lerp(At(left, top), At(left + 1, top), fractionX);
        float lower = Lerp(At(left, top + 1), At(left + 1, top + 1), fractionX);

        return Lerp(upper, lower, fractionY);

        float At(int sampleX, int sampleY)
        {
            return values[(Math.Clamp(sampleY, 0, height - 1) * width) + Math.Clamp(sampleX, 0, width - 1)];
        }
    }

    private static float Lerp(float from, float to, float fraction)
    {
        return from + ((to - from) * fraction);
    }

    private static float Smooth(float value)
    {
        float clamped = Math.Clamp(value, 0, 1);

        return clamped * clamped * (3 - (2 * clamped));
    }
}
