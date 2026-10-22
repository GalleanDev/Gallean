using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Gallean.Client;

internal sealed class TextureBuffer(int width, int height)
{
    public int Width => width;
    public int Height => height;

    public float[] Red { get; } = new float[width * height];
    public float[] Green { get; } = new float[width * height];
    public float[] Blue { get; } = new float[width * height];
    public float[] Alpha { get; } = new float[width * height];

    public void Set(int index, TexturePixel pixel)
    {
        Red[index] = pixel.Red;
        Green[index] = pixel.Green;
        Blue[index] = pixel.Blue;
        Alpha[index] = pixel.Alpha;
    }

    public unsafe WriteableBitmap ToOpaqueBitmap()
    {
        WriteableBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using ILockedFramebuffer buffer = bitmap.Lock();

        float[] red = Red;
        float[] green = Green;
        float[] blue = Blue;

        byte* data = (byte*)buffer.Address;

        for (int y = 0; y < height; y++)
        {
            byte* row = data + (y * buffer.RowBytes);

            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                row[(x * 4) + 0] = TextureHelpers.ToByte(blue[index]);
                row[(x * 4) + 1] = TextureHelpers.ToByte(green[index]);
                row[(x * 4) + 2] = TextureHelpers.ToByte(red[index]);
                row[(x * 4) + 3] = byte.MaxValue;
            }
        }

        return bitmap;
    }

    public unsafe WriteableBitmap ToPremultipliedBitmap()
    {
        WriteableBitmap bitmap = new(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using ILockedFramebuffer buffer = bitmap.Lock();

        float[] red = Red;
        float[] green = Green;
        float[] blue = Blue;
        float[] alpha = Alpha;

        byte* rows = (byte*)buffer.Address;

        for (int y = 0; y < height; y++)
        {
            byte* row = rows + (y * buffer.RowBytes);

            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + 0 + x;
                float pixelAlpha = Math.Clamp(alpha[index], 0, 1);

                row[(x * 4) + 0] = TextureHelpers.ToByte(blue[index] * pixelAlpha);
                row[(x * 4) + 1] = TextureHelpers.ToByte(green[index] * pixelAlpha);
                row[(x * 4) + 2] = TextureHelpers.ToByte(red[index] * pixelAlpha);
                row[(x * 4) + 3] = TextureHelpers.ToByte(pixelAlpha * 255);
            }
        }

        return bitmap;
    }
}
