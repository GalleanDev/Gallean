namespace Packer;

internal static class UmbraExtensions
{
    extension(AssetSprite sprite)
    {
        public UmbraSprite ToUmbra()
        {
            return new UmbraSprite(sprite.Metadata.ToUmbra(), sprite.Pixels);
        }

        public UmbraSpriteHash ToHash()
        {
            return UmbraSpriteHash.Create(sprite.Metadata.PixelWidth, sprite.Metadata.PixelHeight, sprite.Pixels.Span);
        }
    }

    extension(AssetSpriteMetadata metadata)
    {
        public UmbraSpriteMetadata ToUmbra()
        {
            return new UmbraSpriteMetadata(
                metadata.Width,
                metadata.Height,
                metadata.PixelWidth,
                metadata.PixelHeight,
                metadata.OffsetX,
                metadata.OffsetY,
                metadata.Additive);
        }
    }

    extension(UmbraSurfaceLayerType type)
    {
        public AssetSpriteLayerType FromUmbra()
        {
            return type switch
            {
                UmbraSurfaceLayerType.Normal => AssetSpriteLayerType.Normal
            };
        }
    }

    extension(UmbraSpriteMetadata metadata)
    {
        public AssetSpriteMetadata FromUmbra()
        {
            return new AssetSpriteMetadata(
                metadata.Width,
                metadata.Height,
                metadata.PixelWidth,
                metadata.PixelHeight,
                metadata.OffsetX,
                metadata.OffsetY,
                metadata.Additive);
        }
    }
}
