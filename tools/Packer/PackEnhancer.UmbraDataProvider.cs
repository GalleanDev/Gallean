namespace Packer;

internal sealed partial class PackEnhancer
{
    private sealed class UmbraDataProvider(EnhancementCache cache, UpscalerType? upscaler) : IUmbraDataProvider
    {
        public UmbraSprite GetUpscaledSprite(UmbraFrame frame)
        {
            AssetSpriteMetadata metadata = frame.Sprite.Metadata.FromUmbra();
            UmbraSpriteHash hash = frame.Hash;
            AssetSprite sprite = ResolveSprite(cache, [EnhancementType.Upscale], upscaler, hash, metadata, null);

            return sprite.ToUmbra();
        }
    }
}
