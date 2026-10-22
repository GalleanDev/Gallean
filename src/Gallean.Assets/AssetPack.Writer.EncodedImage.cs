namespace Gallean.Assets;

public sealed partial class AssetPack
{
    public sealed partial class Writer
    {
        public sealed record EncodedImage(int PixelWidth, int PixelHeight, ReadOnlyMemory<byte> Data);
    }
}
