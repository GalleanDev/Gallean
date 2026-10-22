using System.Numerics;

namespace Umbra;

internal sealed record FluidControl(
    int Width,
    int Height,
    float[] Source,
    float[] RestoreX,
    float[] RestoreY,
    float[] CurrentX,
    float[] CurrentY)
{
    private const int SourceOctaves = 2;
    private const int SourceSeedOffset = 77;
    private const float CirculationStrength = 0.15f;
    private const float CirculationReach = 3.0f;

    public static FluidControl Create(UmbraFluidConfiguration configuration, UmbraFluidMap map, int width, int height)
    {
        float[] settled = Resample(map, width, height);
        bool[] mask = Array.ConvertAll(settled, value => value > 0);
        bool[] everywhere = Array.ConvertAll(settled, _ => true);
        bool[] outside = Array.ConvertAll(mask, value => !value);
        bool[] outlets = FindOutlets(mask, width, height);
        FluidDistanceField maskDistance = new(mask, everywhere, width, height);
        FluidDistanceField outletDistance = new(outlets, mask, width, height);
        FluidDistanceField outsideDistance = new(outside, everywhere, width, height);
        float[] source = new float[width * height];
        float[] restoreX = new float[width * height];
        float[] restoreY = new float[width * height];
        float[] currentX = new float[width * height];
        float[] currentY = new float[width * height];
        double frequency = configuration.SourceFrequency / width;
        double reach = configuration.RestoreReach * height;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                float seeded = FluidNoise.Fbm(x * frequency, y * frequency, SourceOctaves, configuration.Seed + SourceSeedOffset);
                float patch = Math.Clamp((seeded - (float)configuration.SourceThreshold) * (float)configuration.SourceContrast, 0, 1);
                float restore = (float)Math.Clamp(maskDistance.GetDistance(x, y) / reach, 0, 1);
                Vector2 restoreDirection = maskDistance.GetDirection(x, y);
                Vector2 currentDirection = GetCurrentDirection(outletDistance, outsideDistance, x, y);

                source[index] = patch * settled[index];
                restoreX[index] = restoreDirection.X * restore;
                restoreY[index] = restoreDirection.Y * restore;
                currentX[index] = currentDirection.X;
                currentY[index] = currentDirection.Y;
            }
        }

        return new FluidControl(width, height, source, restoreX, restoreY, currentX, currentY);
    }

    private static Vector2 GetCurrentDirection(FluidDistanceField outletDistance, FluidDistanceField outsideDistance, int x, int y)
    {
        if (float.IsFinite(outletDistance.GetDistance(x, y)))
        {
            return outletDistance.GetDirection(x, y);
        }
        else
        {
            Vector2 outsideDirection = outsideDistance.GetDirection(x, y);
            float circulation = Math.Clamp((outsideDistance.GetDistance(x, y) - 1) / CirculationReach, 0, 1) * CirculationStrength;

            return new Vector2(-outsideDirection.Y, outsideDirection.X) * circulation;
        }
    }

    private static float[] Resample(UmbraFluidMap map, int width, int height)
    {
        ReadOnlySpan<byte> values = map.Values.Span;

        float[] result = new float[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int mapX = x * map.Width / width;
                int mapY = y * map.Height / height;

                result[y * width + x] = values[mapY * map.Width + mapX] / 255.0f;
            }
        }

        return result;
    }

    private static bool[] FindOutlets(bool[] mask, int width, int height)
    {
        bool[] result = new bool[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isEdge = x == 0 || x == width - 1 || y == height - 1;

                result[y * width + x] = mask[y * width + x] && isEdge;
            }
        }

        return result;
    }
}
