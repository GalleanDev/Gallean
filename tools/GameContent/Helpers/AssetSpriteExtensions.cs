namespace GameContent;

internal static class AssetSpriteExtensions
{
    extension(AssetSprite sprite)
    {
        public AssetSprite WithOffset(int offsetX, int offsetY)
        {
            return sprite with { Metadata = sprite.Metadata with { OffsetX = offsetX, OffsetY = offsetY } };
        }
    }
}
