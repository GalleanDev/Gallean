namespace Packer;

internal sealed partial class EnhancementCache
{
    public sealed record Image(int PixelWidth, int PixelHeight, byte[] Pixels);
}
