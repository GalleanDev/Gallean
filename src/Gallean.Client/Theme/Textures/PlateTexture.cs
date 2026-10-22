using Avalonia.Media.Imaging;

namespace Gallean.Client;

internal static class PlateTexture
{
    private const int TileWidth = 512;
    private const int TileHeight = 128;
    private const int Seed = 4711;
    private const float GrainAmplitude = 12;
    private const float DarkAlpha = 0.85f;
    private const float LightAlpha = 0.55f;

    private static readonly NoiseOctave[] Octaves =
    [
        new(4, 2, 35),
        new(8, 4, 25),
        new(16, 8, 17),
        new(32, 16, 10),
        new(64, 32, 6),
        new(128, 64, 4)
    ];

    private static readonly Lazy<WriteableBitmap> Texture = new(Create);

    public static WriteableBitmap Bitmap => Texture.Value;

    private static WriteableBitmap Create()
    {
        TextureBuffer buffer = new(TileWidth, TileHeight);

        for (int y = 0; y < TileHeight; y++)
        {
            for (int x = 0; x < TileWidth; x++)
            {
                int index = (y * TileWidth) + x;
                float cloud = TextureNoise.Fractal(x, y, Octaves, TileWidth, TileHeight, Seed);
                float grain = TextureNoise.Grain(x, y, Seed + 50, GrainAmplitude);
                float signal = Math.Clamp((cloud + grain) / 100, -1, 1);
                bool light = signal > 0;
                float strength = Math.Abs(signal);

                if (light)
                {
                    buffer.Set(index, new TexturePixel(150, 105, 55, strength * LightAlpha));
                }
                else
                {
                    buffer.Set(index, new TexturePixel(24, 12, 4, strength * DarkAlpha));
                }
            }
        }

        return buffer.ToPremultipliedBitmap();
    }
}
