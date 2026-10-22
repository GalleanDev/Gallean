using TorchSharp;

namespace Umbra;

internal sealed record ChromaKey(double RedLevel, double GreenLevel, double BlueLevel)
{
    private const double Threshold = 140.0 / 255.0;

    public static ChromaKey Blue { get; } = new(0.0, 71.0 / 255.0, 187.0 / 255.0);
    public static ChromaKey Green { get; } = new(0.0, 177.0 / 255.0, 64.0 / 255.0);

    public static ChromaKey Select(UmbraSprite frame)
    {
        double blueMetric = Blue.GetMetric(frame);
        double greenMetric = Green.GetMetric(frame);

        if (blueMetric - greenMetric > 0.1)
        {
            return Green;
        }
        else
        {
            return Blue;
        }
    }

    public torch.Tensor ToPlanes(torch.Device device)
    {
        using IDisposable scope = torch.NewDisposeScope();

        using torch.Tensor channels = torch.tensor([(float)RedLevel, (float)GreenLevel, (float)BlueLevel], torch.ScalarType.Float32, device);

        return channels.view(3, 1, 1).MoveToOuterDisposeScope();
    }

    private double GetMetric(UmbraSprite frame)
    {
        ReadOnlySpan<byte> pixels = frame.Pixels.Span;
        double keyRed = RedLevel * byte.MaxValue;
        double keyGreen = GreenLevel * byte.MaxValue;
        double keyBlue = BlueLevel * byte.MaxValue;
        double limit = Threshold * byte.MaxValue;
        int opaque = 0;
        int conflicts = 0;

        for (int offset = 0; offset + 3 < pixels.Length; offset += 4)
        {
            if (pixels[offset + 3] == byte.MaxValue)
            {
                opaque++;

                double deltaRed = pixels[offset] - keyRed;
                double deltaGreen = pixels[offset + 1] - keyGreen;
                double deltaBlue = pixels[offset + 2] - keyBlue;

                if ((deltaRed * deltaRed) + (deltaGreen * deltaGreen) + (deltaBlue * deltaBlue) < limit * limit)
                {
                    conflicts++;
                }
            }
        }

        if (opaque == 0)
        {
            return 0.0;
        }
        else
        {
            return (double)conflicts / opaque;
        }
    }
}
