namespace Gallean.Client;

internal static class TextureNoise
{
    public static float Grain(int x, int y, int seed, float amplitude)
    {
        return (Random(x, y, seed) - 0.5f) * 2 * amplitude;
    }

    public static float Fractal(int x, int y, NoiseOctave[] octaves, int width, int height, int seed)
    {
        float value = 0;

        for (int octave = 0; octave < octaves.Length; octave++)
        {
            value += octaves[octave].Amplitude * Value(x, y, octaves[octave].CellsX, octaves[octave].CellsY, width, height, seed + octave);
        }

        return value;
    }

    private static float Value(int x, int y, int cellsX, int cellsY, int width, int height, int seed)
    {
        float cellX = (float)x * cellsX / width;
        float cellY = (float)y * cellsY / height;

        int leftCell = (int)cellX;
        int topCell = (int)cellY;
        int rightCell = (leftCell + 1) % cellsX;
        int bottomCell = (topCell + 1) % cellsY;

        float fractionX = TextureHelpers.Smooth(cellX - leftCell);
        float fractionY = TextureHelpers.Smooth(cellY - topCell);

        float topLeft = Random(leftCell, topCell, seed);
        float topRight = Random(rightCell, topCell, seed);
        float bottomLeft = Random(leftCell, bottomCell, seed);
        float bottomRight = Random(rightCell, bottomCell, seed);

        float top = TextureHelpers.Lerp(topLeft, topRight, fractionX);
        float bottom = TextureHelpers.Lerp(bottomLeft, bottomRight, fractionX);

        return (TextureHelpers.Lerp(top, bottom, fractionY) - 0.5f) * 2;
    }

    private static float Random(int x, int y, int seed)
    {
        int hash = StableHash.Combine(StableHash.Combine(x, y), seed);

        return hash / (float)int.MaxValue;
    }
}
