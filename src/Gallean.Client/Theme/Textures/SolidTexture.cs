using Avalonia.Media.Imaging;

namespace Gallean.Client;

internal static class SolidTexture
{
    private static readonly Lazy<WriteableBitmap> GrayTexture = new(() => Create(128));
    private static readonly Lazy<WriteableBitmap> BlackTexture = new(() => Create(0));

    public static WriteableBitmap Gray => GrayTexture.Value;
    public static WriteableBitmap Black => BlackTexture.Value;

    private static WriteableBitmap Create(float value)
    {
        TextureBuffer buffer = new(1, 1);

        buffer.Red[0] = value;
        buffer.Green[0] = value;
        buffer.Blue[0] = value;

        return buffer.ToOpaqueBitmap();
    }
}
