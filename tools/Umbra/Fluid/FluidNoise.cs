namespace Umbra;

internal static class FluidNoise
{
    private const uint CellPrime = 73856093;
    private const uint RowPrime = 19349663;
    private const uint SeedPrime = 83492791;
    private const double Lacunarity = 2.03;
    private const double OffsetX = 1.7;
    private const double OffsetY = -3.1;
    private const int SeedStep = 13;

    public static float Fbm(double x, double y, int octaves, int seed)
    {
        double total = 0.0;
        double amplitude = 1.0;
        double normalization = 0.0;

        for (int octave = 0; octave < octaves; octave++)
        {
            total += amplitude * Value(x, y, seed);
            normalization += amplitude;
            amplitude *= 0.5;
            x = x * Lacunarity + OffsetX;
            y = y * Lacunarity + OffsetY;
            seed += SeedStep;
        }

        return (float)(total / normalization);
    }

    private static double Value(double x, double y, int seed)
    {
        double cellX = Math.Floor(x);
        double cellY = Math.Floor(y);
        int column = (int)cellX;
        int row = (int)cellY;
        double fractionX = x - cellX;
        double fractionY = y - cellY;
        double weightX = fractionX * fractionX * (3 - 2 * fractionX);
        double weightY = fractionY * fractionY * (3 - 2 * fractionY);

        double topLeft = Hash(column, row, seed);
        double topRight = Hash(column + 1, row, seed);
        double bottomLeft = Hash(column, row + 1, seed);
        double bottomRight = Hash(column + 1, row + 1, seed);

        double top = topLeft + (topRight - topLeft) * weightX;
        double bottom = bottomLeft + (bottomRight - bottomLeft) * weightX;

        return top + (bottom - top) * weightY;
    }

    private static double Hash(int column, int row, int seed)
    {
        uint value = (uint)column * CellPrime ^ (uint)row * RowPrime ^ (uint)seed * SeedPrime;

        value ^= value >> 16;
        value *= 0x7feb352d;
        value ^= value >> 15;
        value *= 0x846ca68b;
        value ^= value >> 16;

        return value / (double)uint.MaxValue;
    }
}
