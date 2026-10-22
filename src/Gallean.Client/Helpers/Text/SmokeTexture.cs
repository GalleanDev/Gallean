using SkiaSharp;

namespace Gallean.Client;

internal static class SmokeTexture
{
    public const int Size = 256;

    private const int BaseCells = 8;
    private const int Octaves = 4;
    private const int Channels = 3;
    private const int PixelStride = 4;

    private static readonly SKSamplingOptions Sampling = new(SKFilterMode.Linear, SKMipmapMode.Linear);
    private static readonly Lazy<SKImage> Image = new(Build);

    public static SKShader CreateShader()
    {
        return Image.Value.ToShader(SKShaderTileMode.Repeat, SKShaderTileMode.Repeat, Sampling);
    }

    private static SKImage Build()
    {
        byte[] pixels = new byte[Size * Size * PixelStride];

        for (int channel = 0; channel < Channels; channel++)
        {
            double[] values = BuildChannel(channel);
            double minimum = values.Min();
            double maximum = values.Max();

            for (int index = 0; index < values.Length; index++)
            {
                double normalized = (values[index] - minimum) / (maximum - minimum);

                pixels[(index * PixelStride) + channel] = (byte)Math.Round(normalized * byte.MaxValue);
            }
        }

        for (int index = 0; index < Size * Size; index++)
        {
            pixels[(index * PixelStride) + Channels] = byte.MaxValue;
        }

        SKImageInfo info = new(Size, Size, SKColorType.Rgba8888, SKAlphaType.Opaque);

        SKImage image = SKImage.FromPixelCopy(info, pixels);
        SkiaMemoryPressure.TrackImage(image);

        return image;
    }

    private static double[] BuildChannel(int channel)
    {
        double[] values = new double[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                values[(y * Size) + x] = GetFbm(x, y, channel);
            }
        }

        return values;
    }

    private static double GetFbm(int x, int y, int channel)
    {
        double value = 0;
        double amplitude = 0.5;
        int cells = BaseCells;

        for (int octave = 0; octave < Octaves; octave++)
        {
            double scale = cells / (double)Size;

            value += amplitude * GetNoise(x * scale, y * scale, cells, (channel * Octaves) + octave);
            amplitude *= 0.5;
            cells *= 2;
        }

        return value;
    }

    private static double GetNoise(double x, double y, int cells, int layer)
    {
        int left = (int)Math.Floor(x);
        int top = (int)Math.Floor(y);
        int right = (left + 1) % cells;
        int bottom = (top + 1) % cells;
        double weightX = Fade(x - left);
        double weightY = Fade(y - top);

        double upper = double.Lerp(GetLattice(left % cells, top % cells, layer), GetLattice(right, top % cells, layer), weightX);
        double lower = double.Lerp(GetLattice(left % cells, bottom, layer), GetLattice(right, bottom, layer), weightX);

        return double.Lerp(upper, lower, weightY);
    }

    private static double Fade(double offset)
    {
        return offset * offset * offset * ((offset * ((offset * 6) - 15)) + 10);
    }

    private static double GetLattice(int x, int y, int layer)
    {
        return StableHash.Combine(StableHash.Combine(x, y), layer) / (double)int.MaxValue;
    }
}
