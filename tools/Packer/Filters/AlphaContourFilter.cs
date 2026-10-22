namespace Packer;

internal sealed class AlphaContourFilter : ISpriteFilter
{
    private const double Sigma = 0.75;
    private const double DisplacementLimit = 0.5;
    private const double RampWidth = 0.375;
    private const double BandRadius = 0.75;
    private const double OpaqueLevel = 0.9;
    private const double ClearLevel = 0.1;
    private const double GuardStart = 1.5;
    private const double GuardWidth = 1.0;
    private const double ThicknessReach = 2.0;
    private const float OrthogonalStep = 3.0f;
    private const float DiagonalStep = 4.0f;
    private const float Unreachable = 1.0e6f;

    private static readonly Guid FilterType = new("5d1e8a37-92c4-4b6f-a0e3-7c4f18b2d965");

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
            result.Add(Filter(frame));
        }

        return result;
    }

    private static AssetSprite Filter(AssetSprite sprite)
    {
        int width = sprite.Metadata.PixelWidth;
        int height = sprite.Metadata.PixelHeight;
        int scale = sprite.Metadata.PixelScale;
        double sigma = Sigma * scale;
        double limit = DisplacementLimit * scale;
        double ramp = RampWidth * scale;
        int kernelRadius = (int)Math.Ceiling(3 * sigma);
        int reach = kernelRadius + 2;

        double[] alpha = ReadAlpha(sprite.Pixels.Span);
        bool[] solid = new bool[alpha.Length];

        for (int index = 0; index < alpha.Length; index++)
        {
            solid[index] = alpha[index] > 0.5;
        }

        float[] outside = GetChamferDistance(solid, width, height, seed: true);
        float[] inside = GetChamferDistance(solid, width, height, seed: false);
        double[] distance = new double[alpha.Length];
        double[] depth = new double[alpha.Length];

        for (int index = 0; index < alpha.Length; index++)
        {
            depth[index] = Math.Min(inside[index], reach);

            if (solid[index])
            {
                distance[index] = 0.5 - depth[index];
            }
            else
            {
                distance[index] = Math.Min(outside[index], reach) - 0.5;
            }
        }

        double[] smoothed = Blur(distance, width, height, sigma, kernelRadius);
        double[] thickness = Pool(depth, width, height, (int)Math.Ceiling(ThicknessReach * sigma), Math.Max, 0.0);
        int bandRadius = (int)Math.Round(BandRadius * scale);
        double[] highest = Pool(alpha, width, height, bandRadius, Math.Max, 0.0);
        double[] lowest = Pool(alpha, width, height, bandRadius, Math.Min, 1.0);
        byte[] pixels = DilateColors(sprite.Pixels.Span, width, height, (int)Math.Ceiling(limit + ramp) + 1);

        for (int index = 0; index < alpha.Length; index++)
        {
            if (highest[index] > OpaqueLevel && lowest[index] < ClearLevel)
            {
                double shift = Math.Clamp(smoothed[index] - distance[index], -limit, limit);
                double straight = Math.Clamp(0.5 - ((distance[index] + shift) / ramp), 0, 1);
                double weight = Math.Clamp((thickness[index] - (GuardStart * sigma)) / (GuardWidth * sigma), 0, 1);

                pixels[(index * 4) + 3] = ToByte(alpha[index] + (weight * (straight - alpha[index])));
            }
        }

        return sprite with { Pixels = pixels };
    }

    private static double[] ReadAlpha(ReadOnlySpan<byte> pixels)
    {
        double[] alpha = new double[pixels.Length / 4];

        for (int index = 0; index < alpha.Length; index++)
        {
            alpha[index] = pixels[(index * 4) + 3] / (double)byte.MaxValue;
        }

        return alpha;
    }

    private static byte[] DilateColors(ReadOnlySpan<byte> source, int width, int height, int rounds)
    {
        byte[] pixels = [.. source];
        bool[] filled = new bool[width * height];

        for (int index = 0; index < filled.Length; index++)
        {
            filled[index] = pixels[(index * 4) + 3] > 0;
        }

        for (int round = 0; round < rounds; round++)
        {
            byte[] dilated = [.. pixels];
            bool[] grown = [.. filled];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (!filled[(y * width) + x])
                    {
                        DilateColor(pixels, filled, dilated, grown, width, height, x, y);
                    }
                }
            }

            pixels = dilated;
            filled = grown;
        }

        return pixels;
    }

    private static void DilateColor(
        byte[] pixels,
        bool[] filled,
        byte[] dilated,
        bool[] grown,
        int width,
        int height,
        int x,
        int y)
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int count = 0;

        for (int offsetY = -1; offsetY <= 1; offsetY++)
        {
            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                int sampleX = x + offsetX;
                int sampleY = y + offsetY;

                if (sampleX >= 0 && sampleY >= 0 && sampleX < width && sampleY < height)
                {
                    int sample = (sampleY * width) + sampleX;

                    if (filled[sample])
                    {
                        red += pixels[sample * 4];
                        green += pixels[(sample * 4) + 1];
                        blue += pixels[(sample * 4) + 2];
                        count++;
                    }
                }
            }
        }

        if (count > 0)
        {
            int index = (y * width) + x;

            dilated[index * 4] = (byte)(red / count);
            dilated[(index * 4) + 1] = (byte)(green / count);
            dilated[(index * 4) + 2] = (byte)(blue / count);
            grown[index] = true;
        }
    }

    private static float[] GetChamferDistance(bool[] solid, int width, int height, bool seed)
    {
        float[] distance = new float[solid.Length];

        for (int index = 0; index < solid.Length; index++)
        {
            if (solid[index] == seed)
            {
                distance[index] = 0;
            }
            else
            {
                distance[index] = Unreachable;
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;
                float best = distance[index];

                if (x > 0)
                {
                    best = MathF.Min(best, distance[index - 1] + OrthogonalStep);
                }

                if (y > 0)
                {
                    best = MathF.Min(best, distance[index - width] + OrthogonalStep);

                    if (x > 0)
                    {
                        best = MathF.Min(best, distance[index - width - 1] + DiagonalStep);
                    }

                    if (x < width - 1)
                    {
                        best = MathF.Min(best, distance[index - width + 1] + DiagonalStep);
                    }
                }

                distance[index] = best;
            }
        }

        for (int y = height - 1; y >= 0; y--)
        {
            for (int x = width - 1; x >= 0; x--)
            {
                int index = (y * width) + x;
                float best = distance[index];

                if (x < width - 1)
                {
                    best = MathF.Min(best, distance[index + 1] + OrthogonalStep);
                }

                if (y < height - 1)
                {
                    best = MathF.Min(best, distance[index + width] + OrthogonalStep);

                    if (x < width - 1)
                    {
                        best = MathF.Min(best, distance[index + width + 1] + DiagonalStep);
                    }

                    if (x > 0)
                    {
                        best = MathF.Min(best, distance[index + width - 1] + DiagonalStep);
                    }
                }

                distance[index] = best;
            }
        }

        for (int index = 0; index < distance.Length; index++)
        {
            distance[index] /= OrthogonalStep;
        }

        return distance;
    }

    private static double[] Blur(double[] channel, int width, int height, double sigma, int radius)
    {
        double[] kernel = new double[(2 * radius) + 1];
        double total = 0;

        for (int step = -radius; step <= radius; step++)
        {
            kernel[step + radius] = Math.Exp(-(step * step) / (2 * sigma * sigma));
            total += kernel[step + radius];
        }

        for (int index = 0; index < kernel.Length; index++)
        {
            kernel[index] /= total;
        }

        double[] rows = new double[channel.Length];

        for (int y = 0; y < height; y++)
        {
            int row = y * width;

            for (int x = 0; x < width; x++)
            {
                double sum = 0;

                for (int step = 0; step < kernel.Length; step++)
                {
                    sum += kernel[step] * channel[row + Math.Clamp(x + step - radius, 0, width - 1)];
                }

                rows[row + x] = sum;
            }
        }

        double[] blurred = new double[channel.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double sum = 0;

                for (int step = 0; step < kernel.Length; step++)
                {
                    sum += kernel[step] * rows[(Math.Clamp(y + step - radius, 0, height - 1) * width) + x];
                }

                blurred[(y * width) + x] = sum;
            }
        }

        return blurred;
    }

    private static double[] Pool(
        double[] channel,
        int width,
        int height,
        int radius,
        Func<double, double, double> select,
        double empty)
    {
        double[] rows = new double[channel.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double best = empty;

                for (int sampleX = Math.Max(0, x - radius); sampleX <= Math.Min(width - 1, x + radius); sampleX++)
                {
                    best = select(best, channel[(y * width) + sampleX]);
                }

                rows[(y * width) + x] = best;
            }
        }

        double[] pooled = new double[channel.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                double best = empty;

                for (int sampleY = Math.Max(0, y - radius); sampleY <= Math.Min(height - 1, y + radius); sampleY++)
                {
                    best = select(best, rows[(sampleY * width) + x]);
                }

                pooled[(y * width) + x] = best;
            }
        }

        return pooled;
    }

    private static byte ToByte(double value)
    {
        return (byte)Math.Clamp(Math.Round(value * byte.MaxValue), 0, byte.MaxValue);
    }
}
