namespace Packer;

internal sealed class CropFilter(int left, int top, int right, int bottom) : ISpriteFilter
{
    private static readonly Guid FilterType = new("601ca3e6-978d-4ee7-a7ec-4cf06b1790e1");

    public Guid Type => FilterType;

    public bool Accepts(AssetSpriteMetadata metadata)
    {
        return metadata.PixelScale == 1;
    }

    public IReadOnlyList<AssetSprite> Apply(IReadOnlyList<AssetSprite> frames, SpriteFilterContext context)
    {
        List<AssetSprite> result = [];

        foreach (AssetSprite frame in frames)
        {
            result.Add(Crop(frame));
        }

        return result;
    }

    private AssetSprite Crop(AssetSprite frame)
    {
        AssetSpriteMetadata metadata = frame.Metadata;

        int cropLeft = Math.Max(left, metadata.OffsetX);
        int cropTop = Math.Max(top, metadata.OffsetY);
        int cropRight = Math.Min(right, metadata.OffsetX + metadata.PixelWidth);
        int cropBottom = Math.Min(bottom, metadata.OffsetY + metadata.PixelHeight);

        if (cropRight <= cropLeft || cropBottom <= cropTop)
        {
            throw new InvalidOperationException("Invalid frame crop target.");
        }

        return SpriteOperations.Crop(
            frame,
            cropLeft - metadata.OffsetX,
            cropTop - metadata.OffsetY,
            cropRight - cropLeft,
            cropBottom - cropTop);
    }
}
